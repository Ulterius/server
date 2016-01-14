#region

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Network;
using UlteriusServer.WebSocketAPI;
using UlteriusServer.Windows.Api;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.Server
{
    internal class TaskServer
    {
        public static void Start()
        {
            var settings = new Settings();
            var port = settings.Read("TaskServer", "TaskServerPort", 8387);
            var cancellation = new CancellationTokenSource();
            var endpoint = new IPEndPoint(IPAddress.Parse(NetworkUtilities.GetIPv4Address()), port);
            var server = new WebSocketEventListener(endpoint);
            server.OnConnect += ws => Console.WriteLine("Connection from " + ws.RemoteEndpoint);
            server.OnDisconnect += ws => Console.WriteLine("Disconnection from " + ws.RemoteEndpoint);
            server.OnMessage += (ws, msg) =>
            {
                var packet = new Packets(msg);
                //cheap way to do non-blocking packet handling 
                Task.Factory.StartNew(() => { HandlePacket(ws, packet); }, cancellation.Token);
            };
            server.Start();
            Log("Task Server started at " + endpoint);
            Console.ReadKey(true);
            Console.WriteLine("Server stoping");
            cancellation.Cancel();
            Console.ReadKey(true);
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
                    case PacketType.VerifyWindowsPassword:
                        var passwordData = WindowsApi.VerifyPassword(packets.args);
                        clientSocket.WriteStringAsync(passwordData, CancellationToken.None);
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
                                    processStarted = started
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
                                    processKilled = killed
                                });
                        var processKilledData = killedJson;
                        clientSocket.WriteStringAsync(processKilledData, CancellationToken.None);
                        break;
                    case PacketType.EmptyApiKey:
                        var emptyApiKeyStatus =
                            new JavaScriptSerializer().Serialize(
                                new
                                {
                                    endpoint = "emptyApiKey",
                                    emptyApiKey = true,
                                    message = "Please generate an API Key"
                                });

                        var emptyApiKeyData = emptyApiKeyStatus;
                        clientSocket.WriteStringAsync(emptyApiKeyData, CancellationToken.None);
                        break;
                    case PacketType.InvalidApiKey:
                        var invalidApiKeyStatus =
                            new JavaScriptSerializer().Serialize(
                                new
                                {
                                    endpoint = "InvalidApiKey",
                                    invalidApiKey = true,
                                    message = "Provided API Key does not match the server key"
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

        private static void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}