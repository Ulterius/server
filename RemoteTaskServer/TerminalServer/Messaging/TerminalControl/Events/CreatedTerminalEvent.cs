#region

using System;

#endregion

namespace UlteriusServer.TerminalServer.Messaging.TerminalControl.Events
{
    [Serializable]
    public class CreatedTerminalEvent : ITerminalEvent
    {
        public string CorrelationId { get; set; }
        public string TerminalType { get; set; }
        public string CurrentPath { get; set; }
        public Guid TerminalId { get; set; }
        public Guid ConnectionId { get; set; }
    }
}