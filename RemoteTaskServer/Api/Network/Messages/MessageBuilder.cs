#region

using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UlteriusServer.Utilities.Security;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;
using static UlteriusServer.Utilities.Security.UlteriusAes;

#endregion

namespace UlteriusServer.Api.Network.Messages
{
    public class MessageBuilder
    {
        private readonly AuthClient _authClient;
        private readonly WebSocket _client;
        private readonly string synckey;
        public string Endpoint;


        public MessageBuilder(AuthClient authClient, WebSocket client, string endpoint, string syncKey)
        {
            _authClient = authClient;
            _client = client;
            Endpoint = endpoint;
            synckey = syncKey;
        }


        /// <summary>
        ///     Encrypt a file with AES using only a password
        /// </summary>
        /// <param name="password"></param>
        /// <param name="data"></param>
        /// <returns>encryptedFile</returns>
        public byte[] PackFile(string password, byte[] data)
        {
            try
            {
                var passwordBytes = Encoding.UTF8.GetBytes(password);
                return EncryptFile(data, passwordBytes);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Console unable to pack file: {e.Message}");
                return null;
            }
        }

        /// <summary>
        ///     Writes a message to the queue.
        /// </summary>
        /// <param name="data"></param>
        public void WriteMessage(object data)
        {
            if (_client != null)
            {
                var json = JsonConvert.SerializeObject(new
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

                            var encryptedData = Encrypt(json, keyBytes, keyIv);
                            if (Program.Headers)
                            {
                                using (var memoryStream = new MemoryStream())
                                {
                                    using (var binaryWriter = new BinaryWriter(memoryStream))
                                    {
                                        binaryWriter.Write(Endpoint.Length);
                                        binaryWriter.Write(Endpoint);
                                        binaryWriter.Write(EncryptionType.CBC.ToString());
                                        binaryWriter.Write(encryptedData.Length);
                                        binaryWriter.Write(encryptedData);
                                    }
                                    encryptedData = memoryStream.ToArray();
                                }
                            }
                            var message = new Message(_client, encryptedData, Message.MessageType.Binary);
                            if (_authClient != null)
                            {
                                var targetPort = _client.LocalEndpoint.Port;
                                _authClient.MessageQueueManagers[targetPort].SendQueue.Add(message);
                            }
                            return;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Could not send encrypted message: {e.Message}");
                    return;
                }
                var jsonMessage = new Message(_client, json, Message.MessageType.Text);
                if (_authClient != null)
                {
                    var targetPort = _client.LocalEndpoint.Port;
                    _authClient.MessageQueueManagers[targetPort].SendQueue.Add(jsonMessage);
                }
            }
        }

        public void WriteScreenFrame(byte[] data)
        {
            if (_authClient != null)
            {
                if (_authClient.AesShook)
                {
                    var keyBytes = Encoding.UTF8.GetBytes(Rsa.SecureStringToString(_authClient.AesKey));
                    var keyIv = Encoding.UTF8.GetBytes(Rsa.SecureStringToString(_authClient.AesIv));

                    var encryptedData = EncryptFrame(data, keyBytes, keyIv);

                    using (var memoryStream = new MemoryStream())
                    {
                        using (var binaryWriter = new BinaryWriter(memoryStream))
                        {
                            binaryWriter.Write(Endpoint.Length);
                            binaryWriter.Write(Endpoint);
                            binaryWriter.Write(EncryptionType.OFB.ToString());
                            binaryWriter.Write(encryptedData.Length);
                            binaryWriter.Write(encryptedData);
                        }
                        encryptedData = memoryStream.ToArray();
                    }
                    var message = new Message(_client, encryptedData, Message.MessageType.Binary);
                    if (_authClient != null)
                    {
                        var targetPort = _client.LocalEndpoint.Port;
                        _authClient.MessageQueueManagers[targetPort].SendQueue.Add(message);
                    }
                }
            }
        }
    }
}



