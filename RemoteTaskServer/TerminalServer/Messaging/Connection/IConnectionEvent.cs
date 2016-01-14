#region

using System;

#endregion

namespace UlteriusServer.TerminalServer.Messaging.Connection
{
    public interface IConnectionEvent
    {
        Guid ConnectionId { get; set; }
    }
}