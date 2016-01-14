#region

using System;

#endregion

namespace UlteriusServer.TerminalServer.Messaging.TerminalControl.Requests
{
    [Serializable]
    public class CloseTerminalRequest : ITerminalRequest
    {
        public Guid TerminalId { get; set; }
        public Guid ConnectionId { get; set; }
    }
}