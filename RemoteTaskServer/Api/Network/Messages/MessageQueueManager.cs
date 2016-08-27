#region

using System;
using System.Collections.Concurrent;
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
            var service = new Task(MessageWorker);
            service.Start();
        }

        private async void MessageWorker()
        {
            while (true)
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
        ///     Sends a JSON based packet
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        private async Task SendJsonPacket(Message packet)
        {
            var json = packet.Json;

            var client = packet.RemoteClient;
            if (client != null && client.IsConnected)
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
        ///     Sends an encrypted packet
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        private async Task SendBinaryPacket(Message packet)
        {
            var client = packet.RemoteClient;
            if (client != null && client.IsConnected)
            {
                try
                {
                    using (var memoryStream = new MemoryStream(packet.Data))
                    using (var messageWriter = client.CreateMessageWriter(WebSocketMessageType.Binary))
                        await memoryStream.CopyToAsync(messageWriter);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }
}