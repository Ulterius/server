#region

using System;
using System.Linq;
using UlteriusServer.TerminalServer.Cli;
using UlteriusServer.TerminalServer.Infrastructure;
using UlteriusServer.TerminalServer.Messaging.TerminalControl.Events;
using UlteriusServer.TerminalServer.Messaging.TerminalControl.Requests;
using UlteriusServer.TerminalServer.Session;

#endregion

namespace UlteriusServer.TerminalServer.Messaging.TerminalControl.Handlers
{
    public class CreateTerminalRequestHandler : IRequestHandler<CreateTerminalRequest>
    {
        private readonly ConnectionManager _connections;
        private readonly ICliSessionFactory[] _factories;
        private readonly ILogger _log;
        private readonly ISystemInfo _sysinfo;

        public CreateTerminalRequestHandler(ConnectionManager sessions, ICliSessionFactory[] factories, ILogger log,
            ISystemInfo sysinfo)
        {
            _factories = factories;
            _connections = sessions;
            _log = log;
            _sysinfo = sysinfo;
        }

        public bool Accept(CreateTerminalRequest message)
        {
            return true;
        }

        public void Consume(CreateTerminalRequest message)
        {
            var factory = _factories.SingleOrDefault(f => f.Type == message.TerminalType);
            if (factory == null)
                throw new ArgumentException("There is no factory for this type");

            var connection = _connections.GetConnection(message.ConnectionId);
            if (connection == null)
                throw new ArgumentException("The connection does not exists");
            var id = _sysinfo.Guid();
            var cli = factory.Create();
            connection.Append(id, cli);
            connection.Push(new CreatedTerminalEvent
            {
                TerminalId = id,
                TerminalType = message.TerminalType,
                CurrentPath = cli.CurrentPath,
                CorrelationId = message.CorrelationId
            });
        }
    }
}