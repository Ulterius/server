#region

using System;
using System.Threading.Tasks;
using NVNC;
using UlteriusServer.TaskServer.Api.Serialization;
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

        public void StartVncServer()
        {
            try
            {
                var settings = new Settings();
                var vncProxyPort = settings.Read("Vnc", "VncProxyPort", 5901);
                var vncPort = settings.Read("Vnc", "VncPort", 5900);
                var vncPass = settings.Read("Vnc", "VncPass", "");
                _vncServer = new VncServer(vncPass, vncProxyPort, vncPort, "Ulterius VNC");
                Task.Run(() => { _vncServer.Start(); });
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
                _vncServer?.Stop();
                var endData = new
                {
                    vncStarted = false,
                    message = $"VNC Server Not Started {e.Message}"
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, endData);
            }
        }
    }
}