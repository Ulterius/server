
#region

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.Api.Network.Messages
{
    public class MessageQueueManager
    {
        public BlockingCollection<Message> SendQueue = new BlockingCollection<Message>();

        public MessageQueueManager()
        {
            var service = new Task(MessageWorker);
            service.Start();
        }

        private async void MessageWorker()
        {
            foreach (var packet in SendQueue.GetConsumingEnumerable())  //it will block here automatically waiting from new items to be added and it will not take cpu down 
            {
                switch (packet.Type)
                {
                    case Message.MessageType.Binary:
                        await SendBinaryPacket(packet);
                        break;
                    case Message.MessageType.Text:
                        await SendJsonPacket(packet);
                        break;
                    case Message.MessageType.Service:
                        await SendServiceMessage(packet);
                        break;
                }
            }
        }

        /// <summary>
        ///    Sends a message to the local ulterius agent
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        private async Task SendServiceMessage(Message packet)
        {
            var command = packet.PlainTextData;
            try
            {
                using (var tcpClient = new TcpClient())
                {
                    await tcpClient.ConnectAsync(IPAddress.Loopback, 22005);
                    using (var stream = tcpClient.GetStream())
                    using (var sw = new StreamWriter(stream, Encoding.UTF8))
                    {

                        if (tcpClient.Connected)
                        {
                            await sw.WriteLineAsync(command);
                            await sw.FlushAsync();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }


        /// <summary>
        ///     Sends a JSON based packet
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        private async Task SendJsonPacket(Message packet)
        {
            var json = packet.PlainTextData;

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