#region

using System;
using System.Text;
using System.Web.Script.Serialization;
using UlteriusServer.Utilities.Security;
using UlteriusServer.WebSocketAPI.Authentication;

#endregion

namespace UlteriusServer.TaskServer.Network.Messages
{
    public class MessageBuilder
    {
        private readonly AuthClient _authClient;
        public  string Endpoint;
        private readonly string synckey;


        public MessageBuilder(AuthClient authClient, string endpoint, string syncKey)
        {
            _authClient = authClient;
            this.Endpoint = endpoint;
            synckey = syncKey;
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

        private byte[] EncryptBytes(byte[] data, byte[] keyBytes, byte[] keyIv)
        { 
            return UlteriusAes.EncryptFile(data, keyBytes, keyIv);
        }

        public void WriteBinary(byte[] data)
        {
            if (_authClient.Client == null) return;
            try
            {
                if (_authClient == null) return;
                if (!_authClient.AesShook) return;
                var keyBytes = Encoding.UTF8.GetBytes(Rsa.SecureStringToString(_authClient.AesKey));
                var keyIv = Encoding.UTF8.GetBytes(Rsa.SecureStringToString(_authClient.AesIv));
                var encryptedData = EncryptBytes(data, keyBytes, keyIv);
                var message = new Message(_authClient, encryptedData, Message.MessageType.Binary);
                TaskManagerServer.MessageQueueManager.SendQueue.Add(message);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not send encrypted message: {e.Message}");
            }
        }

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
                            TaskManagerServer.MessageQueueManager.SendQueue.Add(message);
                            return;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Could not send encrypted message: {e.Message}");
                    return;
                }
                var jsonMessage = new Message(_authClient, json, Message.MessageType.Binary);
                TaskManagerServer.MessageQueueManager.SendQueue.Add(jsonMessage);
            }
        }
    }
}