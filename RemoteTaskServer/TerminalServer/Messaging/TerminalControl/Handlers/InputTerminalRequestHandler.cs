#region

using System;
using UlteriusServer.Authentication;
using UlteriusServer.TerminalServer.Infrastructure;
using UlteriusServer.TerminalServer.Messaging.TerminalControl.Requests;
using UlteriusServer.TerminalServer.Session;

#endregion

namespace UlteriusServer.TerminalServer.Messaging.TerminalControl.Handlers
{
    public class InputTerminalRequestHandler : IRequestHandler<TerminalInputRequest>
    {
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
            Console.WriteLine(message.Input + " " + message.CorrelationId);
            if (!connection.IsAuthed && message.Input.Equals("ulterius-auth"))
            {
                connection.TryingAuth = true;
                cli.Output("Please enter your password", 1, false);
            }
            else if (!connection.IsAuthed && connection.TryingAuth)
            {
                cli.Output("Logging in please wait...", 1, false);
                var loginDecoder = new UlteriusLoginDecoder();
                var authed = loginDecoder.Login(message.Input);
                cli.Output(authed ? "Login was successfull" : "Login was unsuccessful, enter your password",
                    Convert.ToInt32(authed), authed);
                connection.IsAuthed = authed;
            }
            else if (!connection.IsAuthed)
            {
                cli.Output("Please login to use this terminal (ulterius-auth)", 0, true);
            }
            else if (connection.IsAuthed)
            {
                cli.Input(message.Input, message.CorrelationId);
            }
        }
    }
}