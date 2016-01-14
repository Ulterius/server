#region

using System;
using MassTransit;

#endregion

namespace UlteriusServer.TerminalServer.Messaging.Connection
{
    [Serializable]
    public class ConnectionConnectRequest : CorrelatedBy<Guid>
    {
        public ConnectionConnectRequest(Guid connectionId, Guid userId)
        {
            ConnectionId = connectionId;
            UserId = userId;
        }

        public Guid ConnectionId { get; private set; }
        public Guid UserId { get; set; }
        public Guid CorrelationId { get; set; }
    }

    [Serializable]
    public class ConnectionConnectResponse : CorrelatedBy<Guid>
    {
        public ConnectionConnectResponse(Guid connectionId, Guid userId)
        {
            ConnectionId = connectionId;
            UserId = userId;
        }

        public Guid ConnectionId { get; private set; }
        public Guid UserId { get; private set; }
        public Guid CorrelationId { get; set; }
    }

    [Serializable]
    public class ConnectionDisconnectedRequest
    {
        public ConnectionDisconnectedRequest(Guid connectionId, Guid userId)
        {
            ConnectionId = connectionId;
            UserId = userId;
        }

        public Guid ConnectionId { get; private set; }
        public Guid UserId { get; private set; }
    }

    [Serializable]
    public class UserConnectionEvent
    {
        public UserConnectionEvent(Guid connectionId, Guid userId)
        {
            UserId = userId;
            ConnectionId = connectionId;
        }

        public Guid UserId { get; private set; }
        public Guid ConnectionId { get; private set; }
    }
}