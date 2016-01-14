#region

using System;

#endregion

namespace UlteriusServer.TerminalServer.Messaging.Connection
{
    public interface IConnectionRequest
    {
        Guid ConnectionId { get; set; }
    }
}