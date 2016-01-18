#region

using System;
using System.Linq;
using System.Threading;
using UlteriusServer.Authentication;
using UlteriusServer.TaskServer.Api.Controllers.Impl;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers
{
    public class ApiController
    {
        public AuthClient authClient;
        public WebSocket client;

        public ApiController(WebSocket client)
        {
            this.client = client;
        }

        public ApiController()
        {
            //ignored
        }


        public void HandlePacket(Packets packet)
        {
            var packetType = packet.packetType;
       
            var errorController = new ErrorController(client, packet);
            var windowsController = new WindowsController(client, packet);

            if (packetType == PacketType.InvalidOrEmptyPacket)
            {
                errorController.InvalidPacket();
                return;
            }
            if (!authClient.Authenticated && packetType == PacketType.Authenticate)
            {
               
                var loginDecoder = new UlteriusLoginDecoder();
                var password = packet.args.First().ToString();
                var authenticationData = loginDecoder.Login(password, client);
                client.WriteStringAsync(authenticationData, CancellationToken.None);


            }
            if (packetType == PacketType.RequestWindowsInformation)
            {
                windowsController.GetWindowsInformation();
            }
            if (authClient.Authenticated)
            {
                #region

                //Build a controller workshop!
                var fileController = new FileController(client, packet);
                var processController = new ProcessController(client, packet);
                var cpuController = new CpuController(client, packet);
                var systemController = new SystemController(client, packet);
                var operatingSystemController = new OperatingSystemController(client, packet);
                var networkController = new NetworkController(client, packet);
                var serverController = new ServerController(client, packet);
                var settingsController = new SettingsController(client, packet);

                #endregion

                switch (packetType)
                {
                    case PacketType.DownloadFile:
                        fileController.DownloadFile();
                        break;
                    case PacketType.CreateFileTree:
                        fileController.CreateFileTree();
                        break;
                    case PacketType.RequestProcess:
                        processController.RequestProcessInformation();
                        break;
                    case PacketType.StreamProcesses:
                        processController.StreamProcessInformation();
                        break;
                    case PacketType.StopProcessStream:
                        processController.StopProcessStream();
                        break;
                    case PacketType.RequestCpuInformation:
                        cpuController.GetCpuInformation();
                        break;
                    case PacketType.RequestOsInformation:
                        operatingSystemController.GetOperatingSystemInformation();
                        break;
                    case PacketType.RestartServer:
                        serverController.RestartServer();
                        break;
                    case PacketType.RequestNetworkInformation:
                        networkController.GetNetworkInformation();
                        break;
                    case PacketType.UseWebServer:
                        settingsController.ChangeWebServerUse();
                        break;
                    case PacketType.ChangeWebServerPort:
                        settingsController.ChangeWebServerPort();
                        break;
                    case PacketType.ChangeWebFilePath:
                        settingsController.ChangeWebFilePath();
                        break;
                    case PacketType.ChangeTaskServerPort:
                        settingsController.ChangeTaskServerPort();
                        break;
                    case PacketType.ChangeNetworkResolve:
                        settingsController.ChangeNetworkResolve();
                        break;
                    case PacketType.GetCurrentSettings:
                        settingsController.GetCurrentSettings();
                        break;
                    case PacketType.RequestSystemInformation:
                        systemController.GetSystemInformation();
                        break;
                    case PacketType.GetEventLogs:
                        operatingSystemController.GetEventLogs();
                        break;
                    case PacketType.StartProcess:
                        processController.StartProcess();
                        break;
                    case PacketType.KillProcess:
                        processController.KillProcess();
                        break;
                    case PacketType.EmptyApiKey:
                        errorController.EmptyApiKey();
                        break;
                    case PacketType.InvalidApiKey:
                        errorController.InvalidApiKey();
                        break;
                    case PacketType.InvalidOrEmptyPacket:
                        errorController.InvalidPacket();
                        break;
                    case PacketType.GenerateNewKey:
                        settingsController.GenerateNewAPiKey();
                        break;
                    case PacketType.CheckUpdate:
                        serverController.CheckForUpdate();
                        break;
                    case PacketType.GetActiveWindowsSnapshots:
                        windowsController.GetActiveWindowsImages();
                        break;
                    default:
                        break;
                }
            }
            else
            {
                errorController.NoAuth();
            }
        }

       
    }
}