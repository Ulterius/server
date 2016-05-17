#region

using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using NVNC;
using UlteriusServer.TaskServer.Api.Serialization;
using UlteriusServer.TaskServer.Services.Network;
using UlteriusServer.Utilities;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers.Impl
{
    internal class VncController : ApiController
    {
        private readonly WebSocket _client;
        private readonly Packets _packet;
        private readonly ApiSerializator _serializator = new ApiSerializator();
        private VncServer _vncServer;

        public VncController(WebSocket client, Packets packet)
        {
            _client = client;
            _packet = packet;
        }


        public bool IsServerRunning(int port)
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();
            return ipEndPoints.Any(endPoint => endPoint.Port == port);
        }

        public void StartVncServer()
        {
            var settings = new Settings();
            var vncProxyPort = settings.Read("Vnc", "VncProxyPort", 5901);
            var vncPort = settings.Read("Vnc", "VncPort", 5900);
            var vncPass = settings.Read("Vnc", "VncPass", "");
            if (IsServerRunning(vncProxyPort))
            {
                var returnData = new
                {
                    vncStarted = false,
                    proxyPort = vncProxyPort,
                    port = vncPort,
                    message = "Server already running."
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, returnData);
                return;
            }
            _vncServer = new VncServer(vncPass, vncProxyPort, vncPort, "Ulterius VNC");
            try
            {
                _vncServer.Start();
                NetworkUtilities.vncServer = _vncServer;
                var endData = new
                {
                    vncStarted = true,
                    proxyPort = vncProxyPort,
                    port = vncPort,
                    message = "VNC Server Started"
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, endData);
            }
            catch (Exception e)
            {
                var endData = new
                {
                    vncStarted = false,
                    proxyPort = vncProxyPort,
                    port = vncPort,
                    message = e.Message
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, endData);
            }
        }
    }
}