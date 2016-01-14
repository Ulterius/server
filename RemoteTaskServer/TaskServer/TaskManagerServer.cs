#region

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using UlteriusServer.Authentication;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Network;
using UlteriusServer.WebSocketAPI;
using UlteriusServer.Windows.Api;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer
{
    internal class TaskManagerServer
    {
        public static ConcurrentDictionary<string, AuthClient> AllClients { get; set; }

        public static void Start()
        {
            AllClients = new ConcurrentDictionary<string, AuthClient>();
            var settings = new Settings();
            var port = settings.Read("TaskServer", "TaskServerPort", 8387);
            var cancellation = new CancellationTokenSource();
            var endpoint = new IPEndPoint(IPAddress.Parse(NetworkUtilities.GetIPv4Address()), port);
            var server = new WebSocketEventListener(endpoint);
            server.OnConnect += HandleConnect;
            server.OnDisconnect += HandleDisconnect;
            server.OnMessage += (ws, msg) =>
            {
                var packet = new Packets(msg);
                //cheap way to do non-blocking packet handling 
                Task.Factory.StartNew(() => { HandlePacket(ws, packet); }, cancellation.Token);
            };
            server.Start();
            Log("Task TServer started at " + endpoint);
        }

        private static void HandleDisconnect(WebSocket clientSocket)
        {
            foreach (var client in AllClients)
            {
                if (client.Value.Client != clientSocket) continue;
                AuthClient temp = null;
                AllClients.TryRemove(client.Key, out temp);
                Console.WriteLine("Disconnection from " + clientSocket.RemoteEndpoint);
            }
        }


        private static void HandleConnect(WebSocket clientSocket)
        {
            Console.WriteLine("Connection from " + clientSocket.RemoteEndpoint);
            var client = new AuthClient(clientSocket);
            AllClients.AddOrUpdate(client.GetHashCode().ToString(), client, (key, value) => value);
            SendWelcomeMessage(clientSocket);
        }

        private static void SendWelcomeMessage(WebSocket clientSocket)
        {
            var welcomeMessage = new JavaScriptSerializer().Serialize(new
            {
                endpoint = "connectedToUlterius",
                results = new
                {
                    message = "Ulterius server online!",
                    authRequired = true
                }
            });
            clientSocket.WriteStringAsync(welcomeMessage, CancellationToken.None);
        }

        /// <summary>
        ///     Executes certain functions based on a packets job
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <param name="packets"></param>
        /// <returns></returns>
        public static void HandlePacket(WebSocket clientSocket, Packets packets)
        {
            var packetType = packets.packetType;
            var authClient = AllClients.Values.First(r => r.Client == clientSocket);
            if (!authClient.Authenticated && packetType == PacketType.Authenticate)
            {
                var loginDecoder = new UlteriusLoginDecoder();
                var authenticationData = loginDecoder.Login(packets.args, clientSocket);
                clientSocket.WriteStringAsync(authenticationData, CancellationToken.None);
            }
            if (authClient.Authenticated)
            {
                try
                {
                    switch (packetType)
                    {
                        case PacketType.RequestProcess:
                            var processData = TaskApi.GetProcessInformation();
                            clientSocket.WriteStringAsync(processData, CancellationToken.None);
                            break;
                        case PacketType.RequestCpuInformation:
                            var cpuData = TaskApi.GetCpuInformation();
                            clientSocket.WriteStringAsync(cpuData, CancellationToken.None);

                            break;
                        case PacketType.RequestOsInformation:
                            var osData = TaskApi.GetOperatingSystemInformation();
                            clientSocket.WriteStringAsync(osData, CancellationToken.None);
                            break;
                        case PacketType.RestartServer:
                            var restartJson =
                                new JavaScriptSerializer().Serialize(
                                    new
                                    {
                                        endpoint = "restartServer",
                                        results = true
                                    });
                            var restartData = restartJson;
                            clientSocket.WriteStringAsync(restartData, CancellationToken.None);
                            TaskApi.RestartServer();
                            break;
                        case PacketType.RequestNetworkInformation:
                            var networkData = TaskApi.GetNetworkInformation();
                            clientSocket.WriteStringAsync(networkData, CancellationToken.None);
                            break;
                        case PacketType.UseWebServer:
                            var useWebServerData =
                                SettingsApi.ChangeWebServerUse(packets.args);
                            clientSocket.WriteStringAsync(useWebServerData, CancellationToken.None);
                            break;
                        case PacketType.ChangeWebServerPort:
                            var changeWebServerPortData =
                                SettingsApi.ChangeWebServerPort(packets.args);
                            clientSocket.WriteStringAsync(changeWebServerPortData, CancellationToken.None);

                            break;
                        case PacketType.ChangeWebFilePath:
                            var changeWebFilePathData =
                                SettingsApi.ChangeWebFilePath(packets.args);
                            clientSocket.WriteStringAsync(changeWebFilePathData, CancellationToken.None);

                            break;
                        case PacketType.ChangeTaskServerPort:
                            var changeTaskServerPortData =
                                SettingsApi.ChangeTaskServerPort(packets.args);
                            clientSocket.WriteStringAsync(changeTaskServerPortData, CancellationToken.None);

                            break;
                        case PacketType.ChangeNetworkResolve:
                            var changeNetworkResolveData = SettingsApi.ChangeNetworkResolve(packets.args);
                            clientSocket.WriteStringAsync(changeNetworkResolveData, CancellationToken.None);
                            break;
                        case PacketType.GetCurrentSettings:
                            var currentSettingsData = SettingsApi.GetCurrentSettings();
                            clientSocket.WriteStringAsync(currentSettingsData, CancellationToken.None);
                            break;
                        case PacketType.RequestSystemInformation:
                            var systemData = TaskApi.GetSystemInformation();
                            clientSocket.WriteStringAsync(systemData, CancellationToken.None);
                            break;
                        case PacketType.RequestWindowsInformation:
                            var windowsData = WindowsApi.GetWindowsInformation();
                            clientSocket.WriteStringAsync(windowsData, CancellationToken.None);
                            break;
                        case PacketType.GetEventLogs:
                            var eventData = TaskApi.GetEventLogs();
                            clientSocket.WriteStringAsync(eventData, CancellationToken.None);
                            break;
                        case PacketType.StartProcess:
                            var started = TaskApi.StartProcess(packets.args);
                            var processJson =
                                new JavaScriptSerializer().Serialize(
                                    new

                                    {
                                        endpoint = "startProcess",
                                        results = started
                                    });
                            var processStartData = processJson;
                            clientSocket.WriteStringAsync(processStartData, CancellationToken.None);
                            break;
                        case PacketType.KillProcess:
                            var killed = TaskApi.KillProcessById(int.Parse(packets.args));
                            var killedJson =
                                new JavaScriptSerializer().Serialize(
                                    new
                                    {
                                        endpoint = "killProcess",
                                        results = killed
                                    });

                            var processKilledData = killedJson;
                            clientSocket.WriteStringAsync(processKilledData, CancellationToken.None);
                            break;
                        case PacketType.EmptyApiKey:
                            var data = new
                            {
                                emptyApiKey = true,
                                message = "Please generate an API Key"
                            };
                            var emptyApiKeyStatus =
                                new JavaScriptSerializer().Serialize(
                                    new
                                    {
                                        endpoint = "emptyApiKey",
                                        results = data
                                    });

                            var emptyApiKeyData = emptyApiKeyStatus;
                            clientSocket.WriteStringAsync(emptyApiKeyData, CancellationToken.None);
                            break;
                        case PacketType.InvalidApiKey:
                            var invalidApiData = new
                            {
                                invalidApiKey = true,
                                message = "Provided API Key does not match the server key"
                            };
                            var invalidApiKeyStatus =
                                new JavaScriptSerializer().Serialize(
                                    new
                                    {
                                        endpoint = "InvalidApiKey",
                                        resulsts = invalidApiData
                                    });

                            var invalidOAuthData = invalidApiKeyStatus;
                            clientSocket.WriteStringAsync(invalidOAuthData, CancellationToken.None);
                            break;
                        case PacketType.InvalidOrEmptyPacket:
                            //do nothing server won't read it and then the message is pooled forever
                            break;
                        case PacketType.GenerateNewKey:
                            var generateNewKeyStatus = SettingsApi.GenerateNewAPiKey(packets.apiKey);
                            var generateNewKeyData = generateNewKeyStatus;
                            clientSocket.WriteStringAsync(generateNewKeyData, CancellationToken.None);
                            break;
                        case PacketType.CheckUpdate:
                            var checkUpdateData = Tools.CheckForUpdates();
                            clientSocket.WriteStringAsync(checkUpdateData, CancellationToken.None);
                            break;
                        case PacketType.GetActiveWindowsSnapshots:
                            var activeWindowsData = WindowsApi.GetActiveWindowsImages();
                            clientSocket.WriteStringAsync(activeWindowsData, CancellationToken.None);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            else
            {
                var noAuthMessage = new JavaScriptSerializer().Serialize(new
                {
                    endpoint = "authentication",
                    results = new
                    {
                        message = "Please login to continue!",
                        authRequired = true
                    }
                });
                clientSocket.WriteStringAsync(noAuthMessage, CancellationToken.None);
            }
        }

        private static void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}