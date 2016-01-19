#region

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Serialization
{
    public class ApiSerializator
    {
        public void Serialize(WebSocket client, string endpoint, string syncKey, object data, bool binary = false)
        {
            var serializer = new JavaScriptSerializer {MaxJsonLength = int.MaxValue};
            var json = serializer.Serialize(new
            {
                endpoint,
                syncKey,
                results = data
            });


            if (binary)
            {
                PushFile(client, "");
            }
            else
            {
                Push(client, json);
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
            catch (Exception)
            {

              //should never happen
            }
        }

        public async void PushFile(WebSocket client, string filePath)
        {

            using (var messageWriter = client.CreateMessageWriter(WebSocketMessageType.Binary))
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                await fs.CopyToAsync(messageWriter);

            }
        }
        private async void Push(WebSocket client, string data)
        {
            client.WriteStringAsync(data, CancellationToken.None);
        }

        
    }
}