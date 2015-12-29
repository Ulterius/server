#region

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Network;
using UlteriusServer.WebSocketAPI;
using UlteriusServer.Windows.Api;

#endregion

namespace UlteriusServer.Server
{
    internal class TaskServer
    {
        private static Socket listenerSocket;
        private static List<ClientData> clients;
        public static int boundPort;

        /// <summary>
        ///     Starts the actual server
        /// </summary>
        /// <returns></returns>
        public static void Start()
        {
            var settings = new Settings();
            listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clients = new List<ClientData>();
           
            var port = settings.Read("TaskServer", "TaskServerPort", 8387);
            boundPort = port;
            var ip = new IPEndPoint(IPAddress.Parse(NetworkUtilities.GetIPv4Address()), port);
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
                        } // first message from the websocket is always its headers; anything else is a message/args
                        else
                        {
                            var packet = new Packets(buffer, readBytes);
                            //cheap way to do non-blocking packet handling 
                            Task.Run(() => { HandlePacket(clientSocket, packet); });
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
        ///     Executes certain functions based on a packets job
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <param name="packets"></param>
        /// <returns></returns>
        public static void HandlePacket(Socket clientSocket, Packets packets)
        {
            if (packets.action == null) //do nothing if invalid query
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
                case PacketType.RequestCpuInformation:
                    var cpuData = WebSocketFunctions.EncodeMessageToSend(TaskApi.GetCpuInformation());
                    clientSocket.Send(cpuData);
                    break;
                case PacketType.RequestOsInformation:
                    var osData = WebSocketFunctions.EncodeMessageToSend(TaskApi.GetOperatingSystemInformation());
                    clientSocket.Send(osData);
                    break;
                case PacketType.RestartServer:
                    var restartJson =
                        new JavaScriptSerializer().Serialize(
                            new
                            {
                                serverRestarted = true
                            });
                    var restartData = WebSocketFunctions.EncodeMessageToSend(restartJson);
                    clientSocket.Send(restartData);
                    TaskApi.RestartServer();
                    break;
                case PacketType.RequestNetworkInformation:
                    var networkData = WebSocketFunctions.EncodeMessageToSend(TaskApi.GetNetworkInformation());
                    clientSocket.Send(networkData);
                    break;
                case PacketType.UseWebServer:
                    var useWebServerData =
                        WebSocketFunctions.EncodeMessageToSend(SettingsApi.ChangeWebServerUse(packets.args));
                    clientSocket.Send(useWebServerData);
                    break;
                case PacketType.ChangeWebServerPort:
                    var changeWebServerPortData =
                        WebSocketFunctions.EncodeMessageToSend(SettingsApi.ChangeWebServerPort(packets.args));
                    clientSocket.Send(changeWebServerPortData);
                    break;
                case PacketType.ChangeWebFilePath:
                    var changeWebFilePathData =
                        WebSocketFunctions.EncodeMessageToSend(SettingsApi.ChangeWebFilePath(packets.args));
                    clientSocket.Send(changeWebFilePathData);
                    break;
                case PacketType.ChangeTaskServerPort:
                    var changeTaskServerPortData =
                        WebSocketFunctions.EncodeMessageToSend(SettingsApi.ChangeTaskServerPort(packets.args));
                    clientSocket.Send(changeTaskServerPortData);
                    break;
                case PacketType.ChangeNetworkResolve:
                    var changeNetworkResolveData =
                        WebSocketFunctions.EncodeMessageToSend(SettingsApi.ChangeNetworkResolve(packets.args));
                    clientSocket.Send(changeNetworkResolveData);
                    break;
                case PacketType.GetCurrentSettings:
                    var currentSettingsData = WebSocketFunctions.EncodeMessageToSend(SettingsApi.GetCurrentSettings());
                    clientSocket.Send(currentSettingsData);
                    break;
                case PacketType.RequestSystemInformation:
                    var systemData = WebSocketFunctions.EncodeMessageToSend(TaskApi.GetSystemInformation());
                    clientSocket.Send(systemData);
                    break;
                case PacketType.RequestWindowsInformation:
                    var windowsData = WebSocketFunctions.EncodeMessageToSend(WindowsApi.GetWindowsInformation());
                    clientSocket.Send(windowsData);
                    break;
                case PacketType.VerifyWindowsPassword:
                    var passwordData = WebSocketFunctions.EncodeMessageToSend(WindowsApi.VerifyPassword(packets.args));
                    clientSocket.Send(passwordData);
                    break;
                case PacketType.GetEventLogs:
                    var eventData = WebSocketFunctions.EncodeMessageToSend(TaskApi.GetEventLogs());
                    clientSocket.Send(eventData);
                    break;
                case PacketType.StartProcess:
                    var started = TaskApi.StartProcess(packets.args);
                    var processJson =
                        new JavaScriptSerializer().Serialize(
                            new
                            {
                                processStarted = started
                            });
                    var processStartData = WebSocketFunctions.EncodeMessageToSend(processJson);
                    clientSocket.Send(processStartData);
                    break;
                case PacketType.KillProcess:
                    var killed = TaskApi.KillProcessById(int.Parse(packets.args));
                    var killedJson =
                        new JavaScriptSerializer().Serialize(
                            new
                            {
                                processKilled = killed
                            });
                    var processKilledData = WebSocketFunctions.EncodeMessageToSend(killedJson);
                    clientSocket.Send(processKilledData);
                    break;
                case PacketType.EmptyApiKey:
                    var emptyApiKeyStatus =
                        new JavaScriptSerializer().Serialize(
                            new
                            {
                                emptyApiKey = true,
                                message = "Please generate an API Key"
                            });

                    var emptyApiKeyData = WebSocketFunctions.EncodeMessageToSend(emptyApiKeyStatus);
                    clientSocket.Send(emptyApiKeyData);
                    break;
                case PacketType.InvalidApiKey:
                    var invalidApiKeyStatus =
                        new JavaScriptSerializer().Serialize(
                            new
                            {
                                invalidApiKey = true,
                                message = "Provided API Key does not match the server key"
                            });

                    var invalidOAuthData = WebSocketFunctions.EncodeMessageToSend(invalidApiKeyStatus);
                    clientSocket.Send(invalidOAuthData);
                    break;
                case PacketType.InvalidPacket:
                    var invalidPacketStatus =
                        new JavaScriptSerializer().Serialize(
                            new
                            {
                                invalidPacket = true,
                                message = "This packet does not exisit on the server."
                            });

                    var invalidPacketData = WebSocketFunctions.EncodeMessageToSend(invalidPacketStatus);
                    clientSocket.Send(invalidPacketData);
                    break;
                case PacketType.GenerateNewKey:
                    var generateNewKeyStatus = SettingsApi.GenerateNewAPiKey(packets.apiKey);

                    var generateNewKeyData = WebSocketFunctions.EncodeMessageToSend(generateNewKeyStatus);
                    clientSocket.Send(generateNewKeyData);
                    break;
                case PacketType.CheckUpdate:
                    var checkUpdateData = WebSocketFunctions.EncodeMessageToSend(Tools.CheckForUpdates());
                    clientSocket.Send(checkUpdateData);
                    break;
                case PacketType.GetActiveWindowsSnapshots:
                    var activeWindowsData = WebSocketFunctions.EncodeMessageToSend(WindowsApi.GetActiveWindowsImages());
                    clientSocket.Send(activeWindowsData);
                    break;
            }
        }

        /// <summary>
        ///     Listens for new connections
        /// </summary>
        private static void ListenThread()
        {
            while (true)
            {
                listenerSocket.Listen(0);
                clients.Add(new ClientData(listenerSocket.Accept()));
            }
        }
    }
}