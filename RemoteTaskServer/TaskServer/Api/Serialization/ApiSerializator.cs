#region

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using UlteriusServer.Utilities.Security;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Serialization
{
    public class ApiSerializator
    {
        public void Serialize(WebSocket client, string endpoint, string syncKey, object data)
        {
            var serializer = new JavaScriptSerializer {MaxJsonLength = int.MaxValue};
            var json = serializer.Serialize(new
            {
                endpoint,
                syncKey,
                results = data
            });
            //we sanity stuff
            try
            {
                foreach (var connectedClient in TaskManagerServer.AllClients)
                {
                    var authClient = connectedClient.Value;
                    if (authClient.Client != client) continue;
                    if (authClient.AesShook)
                    {
                        var keybytes = Encoding.UTF8.GetBytes(Rsa.SecureStringToString(authClient.AesKey));
                        var iv = Encoding.UTF8.GetBytes(Rsa.SecureStringToString(authClient.AesIv));
                        //convert packet json into base64
                        json = Convert.ToBase64String(Aes.Encrypt(json, keybytes, iv));
                    }
                }
            }
            catch (Exception)
            {
                if (endpoint != null && !endpoint.Equals("aeshandshake"))
                {
                    return;
                }
            }
            Push(client, json);
        }


        public void SerializeBinary(WebSocket client, string endpoint, string syncKey, string data)
        {

            try
            {
                foreach (var encryptedBinary in from connectedClient in TaskManagerServer.AllClients select connectedClient.Value into authClient where authClient.Client == client where authClient.AesShook let keybytes = Encoding.UTF8.GetBytes(Rsa.SecureStringToString(authClient.AesKey)) let iv = Encoding.UTF8.GetBytes(Rsa.SecureStringToString(authClient.AesIv)) select Aes.Encrypt(data, keybytes, iv))
                {
                    PushBinary(client, endpoint, syncKey, encryptedBinary);
                }
            }
            catch (Exception e )
            {
               
                Console.WriteLine($"Error serilazing binary: {e.Message}");
            }
        }

        public async void PushBinary(WebSocket client, string endpoint, string syncKey, byte[] data)
        {
            try
            {
                using (var messageWriter = client.CreateMessageWriter(WebSocketMessageType.Binary))
                {
                    using (var stream = new MemoryStream(data))
                    {
                        await stream.CopyToAsync(messageWriter);
                    }
                }
            }
            catch (Exception e)
            {
               Console.WriteLine(e.Message);
            }
        }

        public void PushFile(WebSocket client, string filePath)
        {
            using (var messageWriter = client.CreateMessageWriter(WebSocketMessageType.Binary))
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                fs.CopyToAsync(messageWriter);
            }
        }

      
        private void Push(WebSocket client, string data)
        {
            client.WriteStringAsync(data, CancellationToken.None);
        }
    }
}