#region

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using UlteriusServer.Utilities.Security;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Serialization
{
    public class ApiSerializator
    {
        public void Serialize(WebSocket client, string endpoint, string syncKey, object data, bool file = false,
            string fileName = "")
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
                        var encryptedData = Aes.Encrypt(json, keybytes, iv);
                        if (file)
                        {
                            PushFile(client, fileName, encryptedData);
                            return;
                        }
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
                //should never happen
            }
        }

        private void Push(WebSocket client, string data)
        {
            client.WriteStringAsync(data, CancellationToken.None);
        }
    }
}