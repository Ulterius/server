#region

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using UlteriusServer.Api.Network.Messages;
using UlteriusServer.Api.Network.PacketHandlers;
using UlteriusServer.Utilities.Security;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.Api.Network
{
    public class PacketManager
    {
        #region types

        /// <summary>
        ///     This is an enum containing all the endpoints the server can understand. Not every endpoint is in use.
        /// </summary>
        public enum EndPoints
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
            CheckVersion,
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
            GetDescription,
            CtrlAltDel,
            RightDown,
            RightUp,
            ChangeDisplayResolution,
            RotateDisplay,
            SetPrimaryDisplay,
            GetLogs,
            GetAvailableMonitors,
            SetActiveMonitor
        }

        #endregion

        private readonly List<object> _args = new List<object>();
        private readonly AuthClient _authClient;
        private readonly WebSocket _client;
        private readonly string _plainText = string.Empty;
        private string _endPointName;
        private EndPoints _endPoints;
        private Type _packetHandler;
        private string _syncKey;

        /// <summary>
        ///     Decrypt a packet
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
                _endPoints = EndPoints.InvalidOrEmptyPacket;
            }
        }

        /// <summary>
        ///     Handle a plain text packet
        /// </summary>
        /// <param name="authClient"></param>
        /// <param name="packetData"></param>
        public PacketManager(AuthClient authClient, WebSocket client, string packetData)
        {
            _authClient = authClient;
            _client = client;
            try
            {
                var validHandshake = JObject.Parse(packetData);
                var endpoint = validHandshake["endpoint"].ToString().Trim().ToLower();
                if (!endpoint.Equals("aeshandshake") || authClient.AesShook)
                {
                    Console.WriteLine("Invalid handshake protocol");
                    _endPoints = EndPoints.InvalidOrEmptyPacket;
                    return;
                }
            }
            catch (Exception e)
            {
                _endPoints = EndPoints.InvalidOrEmptyPacket;
                Console.WriteLine($"Packet failed: {e.Message}");
                return;
            }

            _plainText = packetData;
        }

        #region packets

        /// <summary>
        ///     Create a PacketInfo based on the packet type.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns>packetInfo</returns>
        public PacketInfo GetPacketInfo(string endpoint)
        {
            if (PacketLoader.Packets.ContainsKey(endpoint))
            {
                return PacketLoader.Packets[endpoint];
            }
            return new PacketInfo
            {
                EndPoint = EndPoints.InvalidOrEmptyPacket,
                Handler = typeof(ErrorPacketHandler)
            };
        }

        #endregion

        /// <summary>
        ///     Create and return a new packet
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
                    _endPointName = deserializedPacket["endpoint"]?.ToString().Trim().ToLower();
                    _syncKey = deserializedPacket["synckey"]?.ToString().Trim();
                    if (deserializedPacket["args"] != null)
                    {
                        _args.AddRange(JArray.Parse(deserializedPacket["args"]?.ToString()));
                    }
                    var packetInfo = GetPacketInfo(_endPointName);
                    _packetHandler = packetInfo.Handler;
                    _endPoints = packetInfo.EndPoint;
                    return new Packet(_authClient, _client, _endPointName, _syncKey, _args, _endPoints, _packetHandler);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Packet failed to deserialize: {e.StackTrace}{e.Message}");
                _endPoints = EndPoints.InvalidOrEmptyPacket;
            }
            return null;
        }

        public class PacketInfo
        {
            public EndPoints EndPoint;
            public Type Handler;
        }
    }
}