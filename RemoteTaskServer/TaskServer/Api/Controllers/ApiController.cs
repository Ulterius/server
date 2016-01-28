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
        public WebSocket Client;

        public ApiController(WebSocket client)
        {
            this.Client = client;
        }

        public ApiController()
        {
            //ignored
        }


        public void HandlePacket(Packets packet)
        {
            var packetType = packet.packetType;
            Console.WriteLine(packetType);
            var errorController = new ErrorController(Client, packet);
            var windowsController = new WindowsController(Client, packet);

            if (packetType == PacketType.InvalidOrEmptyPacket)
            {
                errorController.InvalidPacket();
                return;
            }
            if (packetType == PacketType.InvalidApiKey)
            {
                errorController.InvalidApiKey();
                return;
            }
            if (!authClient.Authenticated && packetType == PacketType.Authenticate)
            {
                var loginDecoder = new UlteriusLoginDecoder();
                var password = packet.args.First().ToString();
                var authenticationData = loginDecoder.Login(password, Client);
                Client.WriteStringAsync(authenticationData, CancellationToken.None);
            }
            if (packetType == PacketType.RequestWindowsInformation)
            {
                windowsController.GetWindowsInformation();
            }
            if (authClient.Authenticated)
            {
                #region

                //Build a controller workshop!
                var fileController = new FileController(Client, packet);
                var processController = new ProcessController(Client, packet);
                var cpuController = new CpuController(Client, packet);
                var systemController = new SystemController(Client, packet);
                var operatingSystemController = new OperatingSystemController(Client, packet);
                var networkController = new NetworkController(Client, packet);
                var serverController = new ServerController(Client, packet);
                var settingsController = new SettingsController(Client, packet);
                var gpuController = new GpuController(Client, packet);
                var vncController = new VncController(Client, packet);
                var pluginController = new PluginController(Client, packet);
                var webcamController = new WebCamController(Client, packet);

                #endregion

                switch (packetType)
                {
                    case PacketType.DownloadFile:
                        fileController.DownloadFile();
                        break;
                    case PacketType.RequestGpuInformation:
                        gpuController.GetGpuInformation();
                        break;
                    case PacketType.Plugin:
                        pluginController.StartPlugin();
                        break;
                    case PacketType.GetPlugins:
                        pluginController.ListPlugins();
                        break;
                    case PacketType.GetBadPlugins:
                        pluginController.ListBadPlugins();
                        break;
                    case PacketType.CreateFileTree:
                        fileController.CreateFileTree();
                        break;
                    case PacketType.StartWebCam:
                       webcamController.StartCamera();
                        break;
                    case PacketType.StopWebCam:
                       webcamController.StopCamera();
                        break;
                    case PacketType.PauseWebCam:
                        webcamController.PauseCamera();
                        break;
                    case PacketType.GetCameras:
                        webcamController.GetCameras();
                        break;
                    case PacketType.GetCameraFrame:
                        webcamController.GetWebCamFrame();
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
                    case PacketType.ChangeVncPort:
                        settingsController.ChangeVncPort();
                        break;
                    case PacketType.ChangeVncPass:
                        settingsController.ChangeVncPassword();
                        break;
                    case PacketType.ChangeVncProxyPort:
                        settingsController.ChangeVncProxyPort();
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
                    case PacketType.StartVncServer:
                        vncController.StartVncServer();
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