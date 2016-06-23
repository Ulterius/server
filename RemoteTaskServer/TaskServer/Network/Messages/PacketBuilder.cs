#region

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using UlteriusServer.Utilities.Security;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Network.Messages
{
    public class PacketBuilder
    {
        private readonly AuthClient _authClient;
        private readonly string endpoint;
        private readonly string synckey;


        public PacketBuilder(AuthClient authClient, string endpoint, string syncKey)
        {
            _authClient = authClient;
            this.endpoint = endpoint;
            synckey = syncKey;
        }

        private void WriteBinary(WebSocket client, byte[] data)
        {
            using (var messageWriter = client.CreateMessageWriter(WebSocketMessageType.Binary))
            {
                using (var stream = new MemoryStream(data))
                {
                    stream.CopyTo(messageWriter);
                }
            }
        }

        public byte[] PackFile(string password, byte[] data)
        {
            try
            {
                var passwordBytes = Encoding.UTF8.GetBytes(password);
                return UlteriusAes.EncryptFile(data, passwordBytes);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Console unable to pack file: {e.Message}");
                return null;
            }
        }

        private void WriteString(WebSocket client, string data)
        {
            try
            {
                client.WriteStringAsync(data, CancellationToken.None);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to write string to client: {e.Message}");
            }
        }


        public void WriteMessage(object data)
        {
            if (_authClient.Client != null)
            {
                var serializer = new JavaScriptSerializer {MaxJsonLength = int.MaxValue};
                var json = serializer.Serialize(new
                {
                    endpoint,
                    synckey,
                    results = data
                });
                Console.WriteLine(json);
                try
                {
                    if (_authClient != null)
                    {
                        if (_authClient.AesShook)
                        {
                            var keyBytes = Encoding.UTF8.GetBytes(Rsa.SecureStringToString(_authClient.AesKey));
                            var keyIv = Encoding.UTF8.GetBytes(Rsa.SecureStringToString(_authClient.AesIv));
                            var encryptedData = UlteriusAes.Encrypt(json, keyBytes, keyIv);
                            WriteBinary(_authClient.Client, encryptedData);
                            return;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Could not send encrypted message: {e.Message}");
                    return;
                }
                WriteString(_authClient.Client, json);
            }
        }
    }
}