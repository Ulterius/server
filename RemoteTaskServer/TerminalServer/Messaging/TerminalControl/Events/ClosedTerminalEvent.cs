#region

using System;

#endregion

namespace UlteriusServer.TerminalServer.Messaging.TerminalControl.Events
{
    [Serializable]
    public class ClosedTerminalEvent : ITerminalEvent
    {
        public Guid TerminalId { get; set; }
        public Guid ConnectionId { get; set; }
    }
}