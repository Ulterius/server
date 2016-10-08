#region

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using UlteriusServer.Api.Network.Messages;
using UlteriusServer.Api.Network.PacketHandlers;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Security;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.Api.Network
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
            SaveSettings,
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
            NoAuth,
            MouseMove,
            MouseDown,
            MouseScroll,
            MouseUp,
            LeftClick,
            LeftDblClick,
            RightClick,
            KeyDown,
            KeyUp,
            FullFrame,
            ListPorts,
            AddOrUpdateJob,
            StopJobDaemon,
            StartJobDaemon,
            GetJobDaemonStatus,
            RemoveJob,
            GetJobContents,
            GetAllJobs,
            GetDescription
        }

        #endregion

        private readonly List<object> _args = new List<object>();
        private readonly AuthClient _authClient;
        private readonly WebSocket _client;
        private readonly string _plainText = string.Empty;
        private string _endPoint;
        private Type _packetHandler;
        private PacketTypes _packetType;
        private string _syncKey;

        /// <summary>
        /// Decrypt a packet
        /// </summary>
        /// <param name="authClient"></param>
        /// <param name="data"></param>
        public PacketManager(AuthClient authClient, WebSocket client, byte[] data)
        {
            _authClient = authClient;
            _client = client;
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

        /// <summary>
        /// Handle a plain text packet
        /// </summary>
        /// <param name="authClient"></param>
        /// <param name="packetData"></param>
        public PacketManager(AuthClient authClient, WebSocket client, string packetData)
        {
      
            _authClient = authClient;
            _client = client;
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

        #region packets

        /// <summary>
        /// Create a PacketInfo based on the packet type.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns>packetInfo</returns>
        public PacketInfo GetPacketInfo(string endpoint)
        {
            switch (endpoint)
            {
                case "listports":
                    return new PacketInfo { Type = PacketTypes.ListPorts, Handler = typeof(ServerPacketHandler) };
                case "authenticate":
                    return new PacketInfo {Type = PacketTypes.Authenticate, Handler = typeof(ServerPacketHandler)};
                case "requestgpuinformation":
                    return new PacketInfo {Type = PacketTypes.RequestGpuInformation, Handler = typeof(GpuPacketHandler)};
                case "createfiletree":
                    return new PacketInfo {Type = PacketTypes.CreateFileTree, Handler = typeof(FilePacketHandler)};
                case "requestprocessinformation":
                    return new PacketInfo
                    {
                        Type = PacketTypes.RequestProcessInformation,
                        Handler = typeof(ProcessPacketHandler)
                    };
                case "requestcpuinformation":
                    return new PacketInfo {Type = PacketTypes.RequestCpuInformation, Handler = typeof(CpuPacketHandler)};
                case "requestosinformation":
                    return new PacketInfo
                    {
                        Type = PacketTypes.RequestOsInformation,
                        Handler = typeof(OperatingSystemPacketHandler)
                    };
                case "requestnetworkinformation":
                    return new PacketInfo
                    {
                        Type = PacketTypes.RequestNetworkInformation,
                        Handler = typeof(NetworkPacketHandler)
                    };
                case "requestsysteminformation":
                    return new PacketInfo
                    {
                        Type = PacketTypes.RequestSystemInformation,
                        Handler = typeof(SystemPacketHandler)
                    };
                case "startprocess":
                    return new PacketInfo {Type = PacketTypes.StartProcess, Handler = typeof(ProcessPacketHandler)};
                case "killprocess":
                    return new PacketInfo {Type = PacketTypes.KillProcess, Handler = typeof(ProcessPacketHandler)};
                case "savesettings":
                    return new PacketInfo { Type = PacketTypes.SaveSettings, Handler = typeof(SettingsPacketHandler) };
                case "startscreenshare":
                    return new PacketInfo
                    {
                        Type = PacketTypes.StartScreenShare,
                        Handler = typeof(ScreenSharePacketHandler)
                    };
                case "stopscreenshare":
                    return new PacketInfo
                    {
                        Type = PacketTypes.StopScreenShare,
                        Handler = typeof(ScreenSharePacketHandler)
                    };
                
                case "getcurrentsettings":
                    return new PacketInfo
                    {
                        Type = PacketTypes.GetCurrentSettings,
                        Handler = typeof(SettingsPacketHandler)
                    };
                case "geteventlogs":
                    return new PacketInfo
                    {
                        Type = PacketTypes.GetEventLogs,
                        Handler = typeof(OperatingSystemPacketHandler)
                    };
                case "checkforupdate":
                    return new PacketInfo {Type = PacketTypes.GetEventLogs, Handler = typeof(ServerPacketHandler)};
                case "restartserver":
                    return new PacketInfo {Type = PacketTypes.RestartServer, Handler = typeof(ServerPacketHandler)};
                case "getwindowsdata":
                    return new PacketInfo {Type = PacketTypes.GetWindowsData, Handler = typeof(WindowsPacketHandler)};
                case "removefile":
                    return new PacketInfo {Type = PacketTypes.RemoveFile, Handler = typeof(FilePacketHandler)};
                case "searchfiles":
                    return new PacketInfo {Type = PacketTypes.SearchFiles, Handler = typeof(FilePacketHandler)};
                case "aeshandshake":
                    return new PacketInfo {Type = PacketTypes.AesHandshake, Handler = typeof(ServerPacketHandler)};
                case "requestfile":
                    return new PacketInfo {Type = PacketTypes.RequestFile, Handler = typeof(FilePacketHandler)};
                case "approvefile":
                    return new PacketInfo {Type = PacketTypes.ApproveFile, Handler = typeof(FilePacketHandler)};
                case "refreshcameras":
                    return new PacketInfo {Type = PacketTypes.RefreshCameras, Handler = typeof(WebCamPacketHandler)};
                case "stopcamerastream":
                    return new PacketInfo {Type = PacketTypes.StopCameraStream, Handler = typeof(WebCamPacketHandler)};
                case "startcamerastream":
                    return new PacketInfo {Type = PacketTypes.StartCameraStream, Handler = typeof(WebCamPacketHandler)};
                case "getcameraframe":
                    return new PacketInfo {Type = PacketTypes.GetCameraFrame, Handler = typeof(WebCamPacketHandler)};
                case "getcameras":
                    return new PacketInfo {Type = PacketTypes.GetCameras, Handler = typeof(WebCamPacketHandler)};
                case "pausecamera":
                    return new PacketInfo {Type = PacketTypes.PauseCamera, Handler = typeof(WebCamPacketHandler)};
                case "stopcamera":
                    return new PacketInfo {Type = PacketTypes.StopCamera, Handler = typeof(WebCamPacketHandler)};
                case "startcamera":
                    return new PacketInfo {Type = PacketTypes.StartCamera, Handler = typeof(WebCamPacketHandler)};
                case "mousemove":
                    return new PacketInfo { Type = PacketTypes.MouseMove, Handler = typeof(ScreenSharePacketHandler) };
                case "mousedown":
                    return new PacketInfo { Type = PacketTypes.MouseDown, Handler = typeof(ScreenSharePacketHandler) };
                case "mousescroll":
                    return new PacketInfo { Type = PacketTypes.MouseScroll, Handler = typeof(ScreenSharePacketHandler) };
                case "mouseup":
                    return new PacketInfo { Type = PacketTypes.MouseUp, Handler = typeof(ScreenSharePacketHandler) };
                case "leftclick":
                    return new PacketInfo { Type = PacketTypes.LeftClick, Handler = typeof(ScreenSharePacketHandler) };
                case "rightclick":
                    return new PacketInfo { Type = PacketTypes.RightClick, Handler = typeof(ScreenSharePacketHandler) };
                case "keydown":
                    return new PacketInfo { Type = PacketTypes.KeyDown, Handler = typeof(ScreenSharePacketHandler) };
                case "keyup":
                    return new PacketInfo { Type = PacketTypes.KeyUp, Handler = typeof(ScreenSharePacketHandler) };
                case "fullframe":
                    return new PacketInfo { Type = PacketTypes.FullFrame, Handler = typeof(ScreenSharePacketHandler) };
                case "addorupdatejob":
                    return new PacketInfo { Type = PacketTypes.AddOrUpdateJob, Handler = typeof(CronJobPacketHandler) };
                case "stopjobdaemon":
                    return new PacketInfo { Type = PacketTypes.StopJobDaemon, Handler = typeof(CronJobPacketHandler) };
                case "startjobdaemon":
                    return new PacketInfo { Type = PacketTypes.StartJobDaemon, Handler = typeof(CronJobPacketHandler) };
                case "getjobdaemonstatus":
                    return new PacketInfo { Type = PacketTypes.GetJobDaemonStatus, Handler = typeof(CronJobPacketHandler) };
                case "removejob":
                    return new PacketInfo { Type = PacketTypes.RemoveJob, Handler = typeof(CronJobPacketHandler) };
                case "getjobcontents":
                    return new PacketInfo { Type = PacketTypes.GetJobContents, Handler = typeof(CronJobPacketHandler) };
                case "getalljobs":
                    return new PacketInfo { Type = PacketTypes.GetAllJobs, Handler = typeof(CronJobPacketHandler) };
                case "getdescription":
                    return new PacketInfo { Type = PacketTypes.GetDescription, Handler = typeof(CronJobPacketHandler) };
                default:
                    return new PacketInfo
                    {
                        Type = PacketTypes.InvalidOrEmptyPacket,
                        Handler = typeof(ErrorPacketHandler)
                    };
            }
        }

        #endregion

        /// <summary>
        /// Create and return a new packet 
        /// </summary>
        /// <returns>Packet</returns>
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
                    _syncKey = deserializedPacket["synckey"]?.ToString().Trim();
                    if (deserializedPacket["args"] != null)
                    {
                        _args.AddRange(JArray.Parse(deserializedPacket["args"]?.ToString()));
                    }
                    var packetInfo = GetPacketInfo(_endPoint);
                    _packetHandler = packetInfo.Handler;
                    _packetType = packetInfo.Type;
                    return new Packet(_authClient, _client, _endPoint, _syncKey, _args, _packetType, _packetHandler);
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