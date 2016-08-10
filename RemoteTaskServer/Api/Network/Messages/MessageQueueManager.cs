#region

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.Api.Network.Messages
{
    public class MessageQueueManager
    {
        public BlockingCollection<Message> SendQueue =
            new BlockingCollection<Message>(new ConcurrentQueue<Message>());

        public MessageQueueManager()
        {
            var backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += BackgroundWorkerOnDoWork;
            backgroundWorker.RunWorkerAsync();
        }

        private async void BackgroundWorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker) sender;
            while (!worker.CancellationPending)
            {
                var packet = SendQueue.Take();
                if (packet.Type == Message.MessageType.Binary)
                {
                    await SendBinaryPacket(packet);
                }
                else if (packet.Type == Message.MessageType.Text)
                {
                    await SendJsonPacket(packet);
                }
            }
        }

        /// <summary>
        /// Sends a JSON based packet 
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        private async Task SendJsonPacket(Message packet)
        {
            var json = packet.Json;
            var client = packet.AuthClient.Client;
            if (client.IsConnected)
            {
                try
                {
                    using (var msg = client.CreateMessageWriter(WebSocketMessageType.Text))
                    using (var writer = new StreamWriter(msg, Encoding.UTF8))
                    {
                        await writer.WriteAsync(json);
                        await writer.FlushAsync();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// Sends an encrypted packet
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        private async Task SendBinaryPacket(Message packet)
        {
            var authClient = packet.AuthClient;
            if (authClient != null && authClient.Client.IsConnected)
            {
                try
                {
                    if (authClient.AesShook)
                    {
                        using (var memoryStream = new MemoryStream(packet.Data))
                        using (var messageWriter = authClient.Client.CreateMessageWriter(WebSocketMessageType.Binary))
                            await memoryStream.CopyToAsync(messageWriter);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }
}