#region

using System;

#endregion

namespace UlteriusServer.TerminalServer.Messaging.TerminalControl.Events
{
    [Serializable]
    public class TerminalOutputEvent : ITerminalEvent
    {
        public string Output { get; set; }
        public string CurrentPath { get; set; }
        public int CorrelationId { get; set; }
        public bool EndOfCommand { get; set; }
        public Guid TerminalId { get; set; }
        public Guid ConnectionId { get; set; }
    }
}