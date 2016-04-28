#region

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using UlteriusServer.Utilities.Security;
using vtortola.WebSockets;
using Aes = UlteriusServer.Utilities.Security.Aes;

#endregion

namespace UlteriusServer.TaskServer.Api.Serialization
{
    public class ApiSerializator
    {
        public void Serialize(WebSocket client, string endpoint, string synckey, object data)
        {
            var serializer = new JavaScriptSerializer {MaxJsonLength = int.MaxValue};
            var json = serializer.Serialize(new
            {
                endpoint,
                synckey,
                results = data
            });
            //we sanity stuff
            try
            {
                foreach (var encryptedData in from connectedClient in TaskManagerServer.AllClients
                    select connectedClient.Value
                    into authClient
                    where authClient.Client == client
                    where authClient.AesShook
                    let keybytes = Encoding.UTF8.GetBytes(Rsa.SecureStringToString(authClient.AesKey))
                    let iv = Encoding.UTF8.GetBytes(Rsa.SecureStringToString(authClient.AesIv))
                    select Aes.Encrypt(json, keybytes, iv))
                {
                    json = Convert.ToBase64String(encryptedData);
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

        public byte[] SerializeFile(WebSocket client, string password, byte[] data)
        {
            try
            {
                var passwordBytes = Encoding.UTF8.GetBytes(password);
                return Aes.EncryptFile(data, passwordBytes);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task CopyToProgress(Stream stream, Stream target, int bufferSize, string fileName, WebSocket client)
        {
            var totalSize = stream.Length;
            var buffer = new byte[bufferSize];
            var readed = -1;
            var completed = 0;
            while (readed != 0)
            {
                readed = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (readed == 0)
                {
                    //End of file
                    continue;
                }
                await target.WriteAsync(buffer, 0, readed);
                completed += readed;
                var unixTimestamp = (int) DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                var progress = completed/(double) totalSize*100;
                var fileProgress = new
                {
                    fileName,
                    progress,
                    unixTimestamp
                };
                Serialize(client, "fileprogress", string.Empty, fileProgress);
            }
        }

        public async void PushFile(WebSocket client, string fileName, byte[] data)
        {
            try
            {
                using (var messageWriter = client.CreateMessageWriter(WebSocketMessageType.Binary))
                {
                    using (var stream = new MemoryStream(data))
                    {
                        await CopyToProgress(stream, messageWriter, 1000000, fileName, client);
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void Push(WebSocket client, string data)
        {
            try
            {
                client.WriteStringAsync(data, CancellationToken.None);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}