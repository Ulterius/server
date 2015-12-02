using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using RemoteTaskServer.Api;
using RemoteTaskServer.Utilities;
using RemoteTaskServer.WebSocketAPI;

namespace RemoteTaskServer.Server
{
    internal class TaskServer
    {
        private static Socket listenerSocket;
        private static List<ClientData> clients;

        /// <summary>
        ///    Starts the actual server
        /// </summary>
        /// <returns></returns>
        public static void Start()
        {
            listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clients = new List<ClientData>();
            var ip = new IPEndPoint(IPAddress.Parse(Packets.GetIPv4Address()), 8387);
            listenerSocket.Bind(ip);
            var listenThread = new Thread(ListenThread);
            listenThread.Start();
        }

        /// <summary>
        ///     Called when a client connects
        /// </summary>
        /// <param name="cSocket"></param>
        /// <returns></returns>
        public static void DataReceived(object cSocket)
        {
            var clientSocket = (Socket) cSocket;
            byte[] buffer;
            int readBytes;
            while (true)
            {
                try
                {
                    buffer = new byte[clientSocket.SendBufferSize];
                    readBytes = clientSocket.Receive(buffer);

                    if (readBytes > 0)
                    {
                        var decodedHeader = Encoding.ASCII.GetString(buffer, 0, readBytes);
                        if (new Regex("^GET").IsMatch(decodedHeader))
                        {
                            var response =
                                Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + Environment.NewLine
                                                       + "Connection: Upgrade" + Environment.NewLine
                                                       + "Upgrade: websocket" + Environment.NewLine
                                                       + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                                                           SHA1.Create().ComputeHash(
                                                               Encoding.UTF8.GetBytes(
                                                                   new Regex("Sec-WebSocket-Key: (.*)").Match(
                                                                       decodedHeader).Groups[1].Value.Trim() +
                                                                   "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                                                                   )
                                                               )
                                                           ) + Environment.NewLine
                                                       + Environment.NewLine);

                            //Tells the web socket to stay connected
                            clientSocket.Send(response);
                        } // first message from the websocket is always its headers; anything else is a message/command
                        else
                        {
                            var packet = new Packets(buffer, readBytes);
                            HandlePacket(clientSocket, packet);
                        }
                      
                    }
                }
                catch (SocketException ex)
                {
                    //Console.Write(ex.Message);
                }
            }
        }

        /// <summary>
        ///    Executes certain functions based on a packets job
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <param name="packets"></param>
        /// <returns></returns>
        public static void HandlePacket(Socket clientSocket, Packets packets)
        {
            if (packets.query == null) //do nothing if invalid query
            {
                return;
            }
            var packetType = packets.packetType;
            switch (packetType)
            {
                case PacketType.RequestProcess:
                     var processData = WebSocketFunctions.EncodeMessageToSend(TaskApi.GetProcessInformation());
                     clientSocket.Send(processData);
                    break;
            }
        }

        /// <summary>
        ///    Listens for new connections
        /// </summary>
        private static void ListenThread()
        {
            while (true)
            {
                listenerSocket.Listen(0);
                clients.Add((new ClientData(listenerSocket.Accept())));
            }
        }
    }


}