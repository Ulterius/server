using UlteriusServer.TerminalServer.Infrastructure;
using UlteriusServer.TerminalServer.Messaging.TerminalControl.Requests;
using UlteriusServer.TerminalServer.Session;

namespace UlteriusServer.TerminalServer.Messaging.TerminalControl.Handlers
{
    public class AesHandshakeRequestHandler : IRequestHandler<AesHandshakeRequest>
    {
        private readonly ConnectionManager _connectionManager;
        private readonly ILogger _log;

        public AesHandshakeRequestHandler(ConnectionManager sessions, ILogger log)
        {
            _connectionManager = sessions;
            _log = log;
        }

        public bool Accept(AesHandshakeRequest message)
        {
            return true;
        }

        public void Consume(AesHandshakeRequest message)
        {
            var connection = _connectionManager.GetConnection(message.ConnectionId);
            connection.AesInfo();
        }
    }
}