#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using UlteriusServer.TerminalServer.Infrastructure;
using UlteriusServer.TerminalServer.Messaging;
using UlteriusServer.TerminalServer.Messaging.Connection;
using UlteriusServer.TerminalServer.Session;

#endregion

namespace UlteriusServer.TerminalServer.Session
{
    public class ConnectionManager : IDisposable
    {
        public static readonly string UserSessionCookieName = "SID";
        private readonly CancellationTokenSource _cancel;

        private readonly ConcurrentDictionary<Guid, UserConnection> _connections;
        private readonly ILogger _log;
        private readonly IMessageBus _mBus;
        private readonly ISystemInfo _systemInfo;
        private readonly List<UnsubscribeAction> _unsubscribeActions;

        public ConnectionManager(IMessageBus mBus, ILogger log, ISystemInfo sysinfo)
        {
            _connections = new ConcurrentDictionary<Guid, UserConnection>();
            _systemInfo = sysinfo;
            _log = log;
            _mBus = mBus;
            _cancel = new CancellationTokenSource();
            _unsubscribeActions = new List<UnsubscribeAction>
            {
                mBus.Queue.SubscribeContextHandler<ConnectionConnectRequest>(HandleConnectionRequest),
                mBus.Queue.SubscribeHandler<ConnectionDisconnectedRequest>(HandleDisconnectionRequest),
                mBus.Queue.SubscribeHandler<UserConnectionEvent>(HandleSessionConnection)
            };
            Task.Run(CheckForDisconnectedAsync);
        }

        public void Dispose()
        {
            foreach (var u in _unsubscribeActions)
                u();
        }

        private void HandleSessionConnection(UserConnectionEvent connection)
        {
            UserConnection s;
            if (_connections.TryGetValue(connection.ConnectionId, out s))
                s.Init();
        }

        private async Task CheckForDisconnectedAsync()
        {
            var disconnectedConnections = new List<UserConnection>();
            while (!_cancel.IsCancellationRequested)
            {
                foreach (var disconnected in disconnectedConnections)
                {
                    if (disconnected.IsConnected)
                    {
                        _log.Debug("Reconnected: '{0}'", disconnected.ConnectionId);
                        continue;
                    }
                    UserConnection s;
                    if (_connections.TryRemove(disconnected.ConnectionId, out s))
                    {
                        _log.Info("Disconnecting: '{0}'", s.ConnectionId);
                        s.Dispose();
                    }
                }

                disconnectedConnections.Clear();
                disconnectedConnections.AddRange(_connections.Values.Where(s => !s.IsConnected));
                foreach (var disconnected in disconnectedConnections)
                    _log.Debug("Ready for disconnection: '{0}'", disconnected.ConnectionId);

                await Task.Delay(5000).ConfigureAwait(false);
            }
        }

        private void HandleDisconnectionRequest(ConnectionDisconnectedRequest disconnect)
        {
            UserConnection s;
            if (_connections.TryGetValue(disconnect.ConnectionId, out s))
                s.IsConnected = false;
        }

        private void HandleConnectionRequest(IConsumeContext<ConnectionConnectRequest> ctx)
        {
            _connections.AddOrUpdate(ctx.Message.ConnectionId,
                id => new UserConnection(ctx.Message.ConnectionId, ctx.Message.UserId, _mBus, _log),
                (id, con) =>
                {
// only attach the session if the user id is the same

                    if (con.UserId == ctx.Message.UserId)
                    {
                        con.IsConnected = true;
                        return con;
                    }
                    return new UserConnection(ctx.Message.ConnectionId, _systemInfo.Guid(), _mBus, _log);
                });

            ctx.Respond(new ConnectionConnectResponse(ctx.Message.ConnectionId, ctx.Message.UserId));
        }

        public UserConnection GetConnection(Guid connectionId)
        {
            UserConnection s;
            if (_connections.TryGetValue(connectionId, out s))
                return s;
            return null;
        }
    }
}