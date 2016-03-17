#region

using System;
using System.Security;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.Authentication
{
    public class AuthClient
    {
        public AuthClient(WebSocket client)
        {
            Client = client;
            LastUpdate = DateTime.Now;
            Authenticated = false;
            AesShook = false;
        }

        public WebSocket Client { get; set; }
        public DateTime LastUpdate { get; set; }
        public bool Authenticated { get; set; }
        public SecureString PrivateKey { get; set; }
        public SecureString PublicKey { get; set; }
        public SecureString AesKey { get; set; }
        public SecureString AesIv { get; set; }
        public bool AesShook { get; set; }
    }
}