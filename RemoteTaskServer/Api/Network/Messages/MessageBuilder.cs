#region

using System;
using System.Text;
using System.Web.Script.Serialization;
using UlteriusServer.Utilities.Security;
using UlteriusServer.WebSocketAPI.Authentication;

#endregion

namespace UlteriusServer.Api.Network.Messages
{
    public class MessageBuilder
    {
        private readonly AuthClient _authClient;
        private readonly string synckey;
        public string Endpoint;


        public MessageBuilder(AuthClient authClient, string endpoint, string syncKey)
        {
            _authClient = authClient;
            Endpoint = endpoint;
            synckey = syncKey;
        }


        /// <summary>
        /// Encrypt a file with AES using only a password
        /// </summary>
        /// <param name="password"></param>
        /// <param name="data"></param>
        /// <returns>encryptedFile</returns>
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

        /// <summary>
        /// Writes a message to the queue.
        /// </summary>
        /// <param name="data"></param>
        public void WriteMessage(object data)
        {
            if (_authClient.Client != null)
            {
                var serializer = new JavaScriptSerializer {MaxJsonLength = int.MaxValue};
                var json = serializer.Serialize(new
                {
                    endpoint = Endpoint,
                    synckey,
                    results = data
                });

                try
                {
                    if (_authClient != null)
                    {
                        if (_authClient.AesShook)
                        {
                            var keyBytes = Encoding.UTF8.GetBytes(Rsa.SecureStringToString(_authClient.AesKey));
                            var keyIv = Encoding.UTF8.GetBytes(Rsa.SecureStringToString(_authClient.AesIv));
                            var encryptedData = UlteriusAes.Encrypt(json, keyBytes, keyIv);
                            var message = new Message(_authClient, encryptedData, Message.MessageType.Binary);
                            UlteriusApiServer.MessageQueueManager.SendQueue.Add(message);
                            return;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Could not send encrypted message: {e.Message}");
                    return;
                }
                var jsonMessage = new Message(_authClient, json, Message.MessageType.Text);
                UlteriusApiServer.MessageQueueManager.SendQueue.Add(jsonMessage);
            }
        }
    }
}