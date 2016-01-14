#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UlteriusServer.Authentication;
using UlteriusServer.TaskServer;
using UlteriusServer.TerminalServer.Cli;
using UlteriusServer.TerminalServer.Infrastructure;
using UlteriusServer.TerminalServer.Messaging;
using UlteriusServer.TerminalServer.Messaging.Connection;
using UlteriusServer.TerminalServer.Messaging.TerminalControl.Events;

#endregion

namespace UlteriusServer.TerminalServer.Session
{
    public class UserConnection : IDisposable
    {
        private readonly IMessageBus _bus;
        private readonly IDictionary<Guid, ICliSession> _cliSessions;
        private readonly ILogger _log;

        public UserConnection(Guid connectionId, Guid sessionId, IMessageBus bus, ILogger log)
        {
            _bus = bus;
            _log = log;
            ConnectionId = connectionId;
            UserId = sessionId;
            IsConnected = true;
            IsAuthed = false;
            TryingAuth = false;
            _cliSessions = new Dictionary<Guid, ICliSession>();
        }

        public Guid UserId { get; }
        public Guid ConnectionId { get; }
        public bool IsConnected { get; set; }
        public bool IsAuthed { get; set; }
        public bool TryingAuth { get; set; }

        public void Dispose()
        {
            _log.Debug("UserSession '{0}' dispose", UserId);
            foreach (var cli in _cliSessions)
                cli.Value.Dispose();
            _cliSessions.Clear();
        }

      

        public void Init()
        {
            Push(new SessionStateEvent
            {
                ConnectionId = ConnectionId,
                UserId = UserId,
                Terminals = _cliSessions.Select(kv => new TerminalDescriptor
                {
                    TerminalId = kv.Key,
                    TerminalType = kv.Value.Type,
                    CurrentPath = kv.Value.CurrentPath
                }).ToArray()
            });
        }

        public void Append(Guid id, ICliSession cliSession)
        {
            _cliSessions.Add(id, cliSession);
            cliSession.Output = (s, c, e) => Push(new TerminalOutputEvent
            {
                TerminalId = id,
                Output = s,
                CurrentPath = cliSession.CurrentPath,
                ConnectionId = ConnectionId,
                CorrelationId = c,
                EndOfCommand = e
            });
        }

        public ICliSession GetTerminalSession(Guid id)
        {
            return _cliSessions[id];
        }

        public void Close(Guid id)
        {
            ICliSession cli;
            if (_cliSessions.TryGetValue(id, out cli))
            {
                _cliSessions.Remove(id);
                cli.Dispose();
                Push(new ClosedTerminalEvent
                {
                    ConnectionId = ConnectionId,
                    TerminalId = id
                });
            }
        }

        public void Push(IConnectionEvent evt)
        {
            evt.ConnectionId = ConnectionId;
            _bus.Queue.Publish(evt, evt.GetType());
        }
    }
}