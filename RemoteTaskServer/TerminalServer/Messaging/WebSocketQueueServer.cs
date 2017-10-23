#region

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using UlteriusServer.TerminalServer.Infrastructure;
using UlteriusServer.TerminalServer.Messaging.Connection;
using UlteriusServer.TerminalServer.Messaging.Serialization;
using UlteriusServer.TerminalServer.Session;
using vtortola.WebSockets;
using vtortola.WebSockets.Rfc6455;
using ILogger = UlteriusServer.TerminalServer.Infrastructure.ILogger;

#endregion

namespace UlteriusServer.TerminalServer.Messaging
{
    public class WebSocketQueueServer : IMessageBus, IDisposable
    {
        public static readonly string ConnectionIdKey = "CID";
        private readonly CancellationTokenSource _cancellation;

        private readonly ILogger _log;
        private readonly IEventSerializator _serializator;
        private readonly ISystemInfo _sysInfo;
        private readonly WebSocketListener _wsServer;

        public WebSocketQueueServer(IPEndPoint endpoint, ISystemInfo sysinfo, ILogger log)
        {
            _log = log;
            _sysInfo = sysinfo;
            _cancellation = new CancellationTokenSource();
            _serializator = new DefaultEventSerializator();

            Queue = ServiceBusFactory.New(sbc =>
            {
                sbc.UseBinarySerializer();
                sbc.ReceiveFrom("loopback://localhost/queue");
            });
            var options = new WebSocketListenerOptions
            {
                PingTimeout = Timeout.InfiniteTimeSpan,
                HttpAuthenticationHandler = this.HttpNegotiation
            };
            options.Standards.RegisterRfc6455();
            _wsServer = new WebSocketListener(endpoint, options);
        }

        public void Dispose()
        {
            _cancellation.Cancel();
            _wsServer.Dispose();
        }

        public IServiceBus Queue { get; }

        public Task StartAsync()
        {
            _wsServer.StartAsync().GetAwaiter().GetResult();
            return AcceptWebSocketClientsAsync(_wsServer);
        }

        private async Task<bool> HttpNegotiation(WebSocketHttpRequest request, WebSocketHttpResponse response)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1));
            var connectionId = Guid.Empty;
            if (request.RequestUri == null || request.RequestUri.OriginalString.Length < 1 ||
                !Guid.TryParse(request.RequestUri.OriginalString.Substring(1), out connectionId))
            {
                connectionId = _sysInfo.Guid();
                _log.Info("Connection Id created: {0}", connectionId);
            }
            else
                _log.Info("Connection Id from url: {0}", connectionId);

            request.Items.Add(ConnectionIdKey, connectionId);

            Guid userId;
            if (request.Cookies[ConnectionManager.UserSessionCookieName] == null)
            {
                userId = _sysInfo.Guid();
                _log.Info("User ID created: {0}", userId);
            }
            else
            {
                userId = Guid.Parse(request.Cookies[ConnectionManager.UserSessionCookieName].Value);
                _log.Info("User ID found in cookie: {0}", userId);
            }

            Queue.PublishRequest(new ConnectionConnectRequest(connectionId, userId), ctx =>
            {
                ctx.HandleFault(f => { response.Status = HttpStatusCode.InternalServerError; });
                ctx.HandleTimeout(TimeSpan.FromSeconds(5), () => { response.Status = HttpStatusCode.RequestTimeout; });
                ctx.Handle<ConnectionConnectResponse>(
                    res =>
                    {
                        response.Cookies.Add(new Cookie(ConnectionManager.UserSessionCookieName, res.UserId.ToString()));
                       
                    });
            });
            return true;
        }

        private async Task AcceptWebSocketClientsAsync(WebSocketListener server)
        {
            while (!_cancellation.IsCancellationRequested)
            {
                try
                {
                    var ws = await server.AcceptWebSocketAsync(_cancellation.Token).ConfigureAwait(false);
                    if (ws == null) continue;
                    var handler = new WebSocketHandler(Queue, ws, _serializator, _log);
                    Task.Run(() => handler.HandleConnectionAsync(_cancellation.Token));
                }
                catch (TaskCanceledException)
                {
                }
                catch (InvalidOperationException)
                {
                }
                catch (Exception aex)
                {
                   // _log.Error("Error Accepting clients", aex.GetBaseException());
                }
            }
           
            //_log.Info("Server Stop accepting clients");
        }
    }
}