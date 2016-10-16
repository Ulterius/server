#region

using System;
using UlteriusServer.TerminalServer.Infrastructure;
using UlteriusServer.TerminalServer.Messaging.TerminalControl.Requests;
using UlteriusServer.TerminalServer.Session;
using UlteriusServer.Utilities.Security;

#endregion

namespace UlteriusServer.TerminalServer.Messaging.TerminalControl.Handlers
{
    public class InputTerminalRequestHandler : IRequestHandler<TerminalInputRequest>
    {
        private static readonly byte INVALID_PASSWORD = 3;
        private static readonly byte AUTHENTICATED = 2;
        private readonly ConnectionManager _connections;
        private readonly ILogger _log;


        public InputTerminalRequestHandler(ConnectionManager sessions, ILogger log)
        {
            _connections = sessions;
            _log = log;
        }

        public bool Accept(TerminalInputRequest message)
        {
            return true;
        }

        public void Consume(TerminalInputRequest message)
        {
            var connection = _connections.GetConnection(message.ConnectionId);
            if (connection == null)
                throw new ArgumentException("Connection does not exist");
            var cli = connection.GetTerminalSession(message.TerminalId);
            if (cli == null)
                throw new ArgumentException("CLI does not exist");
            if (!connection.IsAuthed && message.Input.Equals("ulterius-auth"))
            {
                connection.TryingAuth = true;
                cli.Output("Please enter your password", 2, false, true);
            }
            else if (!connection.IsAuthed && connection.TryingAuth)
            {
                cli.Output("Logging in please wait...", message.CorrelationId, false, false);
                var authed = Login(message.Input);
                cli.Output(authed ? "Login was successful" : "Login was unsuccessful, enter your password",
                    message.CorrelationId, authed, authed == false);
                connection.IsAuthed = authed;
            }
            else if (!connection.IsAuthed)
            {
                cli.Output("Please login to use this terminal (ulterius-auth)", 0, true, false);
            }
            else if (connection.IsAuthed)
            {
                cli.Input(message.Input, message.CorrelationId);
            }
        }

        private string GetUsername()
        {
            return Environment.UserName;
        }

        private bool Login(string password)
        {
            var code = 3;
            if (string.IsNullOrEmpty(password))
            {
                code = INVALID_PASSWORD;
            }

            code = AuthUtils.Authenticate(password) ? 2 : 3;

            var authenticated = code == AUTHENTICATED;
            return authenticated;
        }
    }
}
