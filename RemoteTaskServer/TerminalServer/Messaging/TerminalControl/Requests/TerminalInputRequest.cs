#region

using System;

#endregion

namespace UlteriusServer.TerminalServer.Messaging.TerminalControl.Requests
{
    [Serializable]
    public class TerminalInputRequest : ITerminalRequest
    {
        public string Input { get; set; }
        public int CorrelationId { get; set; }
        public Guid TerminalId { get; set; }
        public Guid ConnectionId { get; set; }
    }
}