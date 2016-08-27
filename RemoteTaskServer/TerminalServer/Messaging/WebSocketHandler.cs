#region

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using UlteriusServer.TerminalServer.Infrastructure;
using UlteriusServer.TerminalServer.Messaging.Connection;
using UlteriusServer.TerminalServer.Messaging.Serialization;
using UlteriusServer.TerminalServer.Session;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TerminalServer.Messaging
{
    public class WebSocketHandler
    {
        private readonly ILogger _log;
        private readonly IServiceBus _queue;
        private readonly IEventSerializator _serializer;
        private readonly WebSocket _ws;

        private CancellationToken _cancellation;

        public WebSocketHandler(IServiceBus bus, WebSocket ws, IEventSerializator serializer, ILogger log)
        {
            _ws = ws;
            _queue = bus;
            _log = log;
            _serializer = serializer;
        }


        public async Task HandleConnectionAsync(CancellationToken cancellation)
        {
            _cancellation = cancellation;
            var unsubs = new List<UnsubscribeAction>();
            var connectionId = GetConnectionId(_ws);
            var sessionId = GetSessionId(_ws);
            try
            {
                _log.Info("Starting session '{0}' at connection '{1}'", sessionId, connectionId);

                unsubs.Add(_queue.SubscribeHandler<IConnectionEvent>(msg =>
                {
                    lock (_ws)
                    {
                        UserConnection user;
                        ConnectionManager._connections.TryGetValue(connectionId, out user);
                        if (user != null)
                        {
                            using (
                                var wsmsg =
                                    _ws.CreateMessageWriter(user.AesShook
                                        ? WebSocketMessageType.Binary
                                        : WebSocketMessageType.Text))
                                _serializer.Serialize(connectionId, msg, wsmsg);
                        }
                    }
                }, con => _ws.IsConnected && con.ConnectionId == connectionId));

                _queue.Publish(new UserConnectionEvent(connectionId, sessionId));

                while (_ws.IsConnected && !_cancellation.IsCancellationRequested)
                {
                    var msg = await _ws.ReadMessageAsync(_cancellation).ConfigureAwait(false);
                    if (msg == null) continue;
                    Type type;
                    var queueRequest = _serializer.Deserialize(connectionId, msg, out type);
                    if (queueRequest == null)
                    {
                        continue;
                    }
                    Console.WriteLine(queueRequest);
                    queueRequest.ConnectionId = connectionId;

                    _queue.Publish(queueRequest, type);
                }
            }
            catch (Exception aex)
            {
                _log.Error("Error Handling connection", aex.GetBaseException());
                try
                {
                    _ws.Close();
                }
                catch
                {
                    // ignored
                }
            }

            finally
            {
                Console.WriteLine("Session '{0}' with connection '{1}' disconnected", sessionId, connectionId);

                foreach (var unsub in unsubs)
                    unsub();
                _ws.Dispose();
                _queue.Publish(new ConnectionDisconnectedRequest(connectionId, sessionId));
            }
        }

        private static Guid GetConnectionId(WebSocket ws)
        {
            return (Guid) ws.HttpRequest.Items[WebSocketQueueServer.ConnectionIdKey];
        }

        private static Guid GetSessionId(WebSocket ws)
        {
            var sessionId = Guid.Empty;
            var cookie = ws.HttpRequest.Cookies[ConnectionManager.UserSessionCookieName] ??
                         ws.HttpResponse.Cookies[ConnectionManager.UserSessionCookieName];
            if (cookie != null && Guid.TryParse(cookie.Value, out sessionId))
                return sessionId;
            throw new Exception("No session ID generated for this connection");
        }
    }
}