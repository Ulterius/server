#region

using TerminalServer.Session;
using UlteriusServer.TerminalServer.Infrastructure;
using UlteriusServer.TerminalServer.Messaging.TerminalControl.Events;
using UlteriusServer.TerminalServer.Messaging.TerminalControl.Requests;

#endregion

namespace UlteriusServer.TerminalServer.Messaging.TerminalControl.Handlers
{
    public class CloseTerminalRequestHandler : IRequestHandler<CloseTerminalRequest>
    {
        private readonly ConnectionManager _connectionManager;
        private readonly ILogger _log;

        public CloseTerminalRequestHandler(ConnectionManager sessions, ILogger log)
        {
            _connectionManager = sessions;
            _log = log;
        }

        public bool Accept(CloseTerminalRequest message)
        {
            return true;
        }

        public void Consume(CloseTerminalRequest message)
        {
            var connection = _connectionManager.GetConnection(message.ConnectionId);
            connection.Close(message.TerminalId);
            connection.Push(new ClosedTerminalEvent {TerminalId = message.TerminalId});
        }
    }
}