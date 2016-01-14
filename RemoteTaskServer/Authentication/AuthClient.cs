using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vtortola.WebSockets;

namespace UlteriusServer.Authentication
{
    public class AuthClient
    {
        public AuthClient(WebSocket client)
        {
            Client = client;
            LastUpdate = DateTime.Now;
            Authenticated = false;
        }

        public WebSocket Client { get; set; }
        public DateTime LastUpdate { get; set; }
        public bool Authenticated { get; set; }
    }
}
