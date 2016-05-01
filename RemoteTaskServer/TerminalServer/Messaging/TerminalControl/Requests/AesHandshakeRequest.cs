using System;

namespace UlteriusServer.TerminalServer.Messaging.TerminalControl.Requests
{
    [Serializable]
    public class AesHandshakeRequest : ITerminalRequest
    {
        public bool AesShook { get; set; }
        public Guid TerminalId { get; set; }
        public Guid ConnectionId { get; set; }
    }
}