#region

using System;
using UlteriusServer.TerminalServer.Messaging.Connection;

#endregion

namespace UlteriusServer.TerminalServer.Messaging.TerminalControl.Events
{
    public interface ITerminalEvent : IConnectionEvent
    {
        Guid TerminalId { get; set; }
    }
}