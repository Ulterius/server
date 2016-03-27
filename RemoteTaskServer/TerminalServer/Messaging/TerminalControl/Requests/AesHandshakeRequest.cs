using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlteriusServer.TerminalServer.Messaging.TerminalControl.Requests
{
    [Serializable]
    public class AesHandshakeRequest : ITerminalRequest
    {
        public Guid TerminalId { get; set; }
        public Guid ConnectionId { get; set; }
        public bool AesShook { get; set; }
    }
}