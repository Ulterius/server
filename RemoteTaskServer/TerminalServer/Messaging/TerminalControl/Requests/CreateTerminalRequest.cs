#region

using System;
using UlteriusServer.TerminalServer.Messaging.Connection;

#endregion

namespace UlteriusServer.TerminalServer.Messaging.TerminalControl.Requests
{
    [Serializable]
    public class CreateTerminalRequest : IConnectionRequest
    {
        public string TerminalType { get; set; }
        public string CorrelationId { get; set; }
        public Guid ConnectionId { get; set; }
    }
}