#region

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using UlteriusServer.TaskServer.Api.Controllers.Impl;
using UlteriusServer.TaskServer.Network.Messages;
using UlteriusServer.TaskServer.Network.PacketHandlers;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Security;
using UlteriusServer.WebSocketAPI.Authentication;

#endregion

namespace UlteriusServer.TaskServer.Network
{
    public class PacketManager
    {
        #region types
        public enum PacketTypes
        {
            RequestProcessInformation,
            RequestCpuInformation,
            RequestOsInformation,
            RequestNetworkInformation,
            RequestSystemInformation,
            StartProcess,
            KillProcess,
            GenerateNewKey,
            EmptyApiKey,
            InvalidApiKey,
            InvalidOrEmptyPacket,
            ToggleWebServer,
            ChangeWebServerPort,
            ChangeWebFilePath,
            ChangeTaskServerPort,
            ChangeNetworkResolve,
            GetCurrentSettings,
            GetEventLogs,
            CheckUpdate,
            GetWindowsData,
            RestartServer,
            GetActiveWindowsSnapshots,
            Authenticate,
            CreateFileTree,
            RequestGpuInformation,
            Plugin,
            ChangeScreenSharePort,
            StartScreenShare,
            ChangeScreenSharePass,
            GetPlugins,
            GetBadPlugins,
            PauseCamera,
            StopCamera,
            StartCamera,
            GetCameras,
            GetCameraFrame,
            RefreshCameras,
            StartCameraStream,
            StopCameraStream,
            ChangeLoadPlugins,
            ChangeUseTerminal,
            AesHandshake,
            ApprovePlugin,
            GetPendingPlugins,
            ApproveFile,
            RequestFile,
            RemoveFile,
            SearchFiles,
            StopScreenShare,
            CheckScreenShare,
            NoAuth
        } 
        #endregion

        #region packets
        public PacketInfo GetPacketInfo(string endpoint)
        {
            switch (endpoint)
            {
                case "authenticate":
                    return new PacketInfo { Type = PacketTypes.Authenticate, Handler = typeof(ServerPacketHandler) };
                case "requestgpuinformation":
                    return new PacketInfo { Type = PacketTypes.RequestGpuInformation, Handler = typeof(GpuPacketHandler) };
                case "createfiletree":
                    return new PacketInfo { Type = PacketTypes.CreateFileTree, Handler = typeof(FilePacketHandler) };
                case "requestprocessinformation":
                    return new PacketInfo { Type = PacketTypes.RequestProcessInformation, Handler = typeof(ProcessPacketHandler) };
                case "requestcpuinformation":
                    return new PacketInfo { Type = PacketTypes.RequestCpuInformation, Handler = typeof(CpuPacketHandler) };
                case "requestosinformation":
                    return new PacketInfo { Type = PacketTypes.RequestOsInformation, Handler = typeof(OperatingSystemPacketHandler) };
                case "requestnetworkinformation":
                    return new PacketInfo { Type = PacketTypes.RequestNetworkInformation, Handler = typeof(NetworkPacketHandler) };
                case "requestsysteminformation":
                    return new PacketInfo { Type = PacketTypes.RequestSystemInformation, Handler = typeof(SystemPacketHandler) };
                case "startprocess":
                    return new PacketInfo { Type = PacketTypes.StartProcess, Handler = typeof(ProcessPacketHandler) };
                case "killprocess":
                    return new PacketInfo { Type = PacketTypes.KillProcess, Handler = typeof(ProcessPacketHandler) };
                case "togglewebserver":
                    return new PacketInfo { Type = PacketTypes.ToggleWebServer, Handler = typeof(SettingsPacketHandler) };
                case "changewebserverport":
                    return new PacketInfo { Type = PacketTypes.ChangeWebServerPort, Handler = typeof(SettingsPacketHandler) };
                case "changewebfilepath":
                    return new PacketInfo { Type = PacketTypes.ChangeWebFilePath, Handler = typeof(SettingsPacketHandler) };
                case "startscreenshare":
                    return new PacketInfo { Type = PacketTypes.StartScreenShare, Handler = typeof(ScreenSharePacketHandler) };
                case "stopscreenshare":
                    return new PacketInfo { Type = PacketTypes.StopScreenShare, Handler = typeof(ScreenSharePacketHandler) };
                case "changescreensharepass":
                    return new PacketInfo { Type = PacketTypes.ChangeScreenSharePass, Handler = typeof(SettingsPacketHandler) };
                case "changeloadplugins":
                    return new PacketInfo { Type = PacketTypes.ChangeLoadPlugins, Handler = typeof(SettingsPacketHandler) };
                case "changetaskserverport":
                    return new PacketInfo { Type = PacketTypes.ChangeTaskServerPort, Handler = typeof(SettingsPacketHandler) };
                case "changenetworkresolve":
                    return new PacketInfo { Type = PacketTypes.ChangeNetworkResolve, Handler = typeof(SettingsPacketHandler) };
                case "changescreenshareport":
                    return new PacketInfo { Type = PacketTypes.ChangeScreenSharePort, Handler = typeof(SettingsPacketHandler) };
                case "changeuseterminal":
                    return new PacketInfo { Type = PacketTypes.ChangeUseTerminal, Handler = typeof(SettingsPacketHandler) };
                case "getcurrentsettings":
                    return new PacketInfo { Type = PacketTypes.GetCurrentSettings, Handler = typeof(SettingsPacketHandler) };
                case "geteventlogs":
                    return new PacketInfo { Type = PacketTypes.GetEventLogs, Handler = typeof(OperatingSystemPacketHandler) };
                case "checkforupdate":
                    return new PacketInfo { Type = PacketTypes.GetEventLogs, Handler = typeof(ServerPacketHandler) };
                case "restartserver":
                    return new PacketInfo { Type = PacketTypes.RestartServer, Handler = typeof(ServerPacketHandler) };
                case "getwindowsdata":
                    return new PacketInfo { Type = PacketTypes.GetWindowsData, Handler = typeof(WindowsPacketHandler) };
                case "removefile":
                    return new PacketInfo { Type = PacketTypes.RemoveFile, Handler = typeof(FilePacketHandler) };
                case "searchfiles":
                    return new PacketInfo { Type = PacketTypes.SearchFiles, Handler = typeof(FilePacketHandler) };
                case "getpendingplugins":
                    return new PacketInfo { Type = PacketTypes.GetPendingPlugins, Handler = typeof(PluginPacketHandler) };
                case "approveplugin":
                    return new PacketInfo { Type = PacketTypes.ApprovePlugin, Handler = typeof(PluginPacketHandler) };
                case "aeshandshake":
                    return new PacketInfo { Type = PacketTypes.AesHandshake, Handler = typeof(ServerPacketHandler) };
                case "requestfile":
                    return new PacketInfo { Type = PacketTypes.SearchFiles, Handler = typeof(FilePacketHandler) };
                case "approvefile":
                    return new PacketInfo { Type = PacketTypes.ApproveFile, Handler = typeof(FilePacketHandler) };
                case "refreshcameras":
                    return new PacketInfo { Type = PacketTypes.RefreshCameras, Handler = typeof(WebCamPacketHandler) };
                case "stopcamerastream":
                    return new PacketInfo { Type = PacketTypes.StopCameraStream, Handler = typeof(WebCamPacketHandler) };
                case "startcamerastream":
                    return new PacketInfo { Type = PacketTypes.StartCameraStream, Handler = typeof(WebCamPacketHandler) };
                case "getcameraframe":
                    return new PacketInfo { Type = PacketTypes.GetCameraFrame, Handler = typeof(WebCamPacketHandler) };
                case "getcameras":
                    return new PacketInfo { Type = PacketTypes.GetCameras, Handler = typeof(WebCamPacketHandler) };
                case "pausecamera":
                    return new PacketInfo { Type = PacketTypes.PauseCamera, Handler = typeof(WebCamPacketHandler) };
                case "stopcamera":
                    return new PacketInfo { Type = PacketTypes.StopCamera, Handler = typeof(WebCamPacketHandler) };
                case "startcamera":
                    return new PacketInfo { Type = PacketTypes.StartCamera, Handler = typeof(WebCamPacketHandler) };
                case "getbadplugins":
                    return new PacketInfo { Type = PacketTypes.GetBadPlugins, Handler = typeof(PluginPacketHandler) };
                case "getplugins":
                    return new PacketInfo { Type = PacketTypes.GetPlugins, Handler = typeof(PluginPacketHandler) };
                case "plugin":
                    return new PacketInfo { Type = PacketTypes.Plugin, Handler = typeof(PluginPacketHandler) };
                default:
                    return new PacketInfo { Type = PacketTypes.InvalidOrEmptyPacket, Handler = typeof(ErrorPacketHandler) };
            }
        } 
        #endregion
        private readonly List<object> _args = new List<object>();
        private readonly AuthClient _authClient;
        private readonly string _plainText = string.Empty;
        private string _endPoint;
        private PacketTypes _packetType;
        private string _syncKey;
        private Type _packetHandler;

        public PacketManager(AuthClient authClient, byte[] data)
        {
         
            _authClient = authClient;
            try
            {
                var keyBytes = Encoding.UTF8.GetBytes(Rsa.SecureStringToString(authClient.AesKey));
                var ivBytes = Encoding.UTF8.GetBytes(Rsa.SecureStringToString(authClient.AesIv));
                _plainText = UlteriusAes.Decrypt(data, keyBytes, ivBytes);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Packet failed to decrypt: {e.Message}");
                _packetType = PacketTypes.InvalidOrEmptyPacket;
            }
        }

        public PacketManager(AuthClient authClient, string packetData)
        {
            _authClient = authClient;
            try
            {
                if ((bool) Settings.Get("TaskServer").Encryption)
                {
                    var validHandshake = JObject.Parse(packetData);
                    var endpoint = validHandshake["endpoint"].ToString().Trim().ToLower();
                    if (!endpoint.Equals("aeshandshake") || authClient.AesShook)
                    {
                        Console.WriteLine("Invalid handshake protocol");
                        _packetType = PacketTypes.InvalidOrEmptyPacket;
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                _packetType = PacketTypes.InvalidOrEmptyPacket;
                Console.WriteLine($"Packet failed: {e.Message}");
                return;
            }

            _plainText = packetData;
        }


        public Packet GetPacket()
        {
            if (string.IsNullOrEmpty(_plainText))
            {
                return null;
            }
            try
            {
                var deserializedPacket = JObject.Parse(_plainText);
                if (deserializedPacket != null)
                {
                    _endPoint = deserializedPacket["endpoint"]?.ToString().Trim().ToLower();
                    _syncKey = deserializedPacket["syncKey"]?.ToString().Trim();
                    if (deserializedPacket["args"] != null)
                    {
                        _args.AddRange(JArray.Parse(deserializedPacket["args"]?.ToString()));
                    }
                    var packetInfo = GetPacketInfo(_endPoint);
                    _packetHandler = packetInfo.Handler;
                    _packetType = packetInfo.Type;
                    return new Packet(_authClient, _endPoint, _syncKey, _args, _packetType, _packetHandler);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Packet failed to deserialize: {e.StackTrace}{e.Message}");
                _packetType = PacketTypes.InvalidOrEmptyPacket;
            }
            return null;
        }

        public class PacketInfo
        {
            public Type Handler;
            public PacketTypes Type;
        }
    }
}