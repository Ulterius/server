#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security;
using UlteriusServer.Api.Network.Messages;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.WebSocketAPI.Authentication
{
    public class AuthClient
    {
        public AuthClient()
        {
            LastUpdate = DateTime.Now;
            Authenticated = false;
            AesShook = false;
        }

        public ConcurrentDictionary<int, MessageQueueManager> MessageQueueManagers { get; set; }
 
        public DateTime LastUpdate { get; set; }
        public bool Authenticated { get; set; }
        public SecureString PrivateKey { get; set; }
        public SecureString PublicKey { get; set; }
        public SecureString AesKey { get; set; }
        public SecureString AesIv { get; set; }
        public bool AesShook { get; set; }
    }
}