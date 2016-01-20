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
        private readonly Packets packet;
        private readonly ApiSerializator serializator = new ApiSerializator();
        private VncServer vncServer;

        public VncController(WebSocket client, Packets packet)
        {
            this._client = client;
            this.packet = packet;
        }

        public void StartVncServer()
        {
            try
            {
                var settings = new Settings();
                var vncProxyPort = settings.Read("Vnc", "VncProxyPort", 5901);
                var vncPort = settings.Read("Vnc", "VncPort", 5900);
                var vncPass = settings.Read("Vnc", "VncPass", "");
                vncServer = new VncServer(vncPass, vncProxyPort, vncPort, "Ulterius VNC");
                Task.Run(() => {
                    vncServer.Start();
                });
                var endData = new
                {
                    vncStarted = true,
                    proxyPort = vncProxyPort,
                    port = vncPort,
                    message = "VNC Server Started"
                };
                serializator.Serialize(_client, packet.endpoint, packet.syncKey, endData);
            }
            catch (ArgumentNullException)
            {
                vncServer?.Stop();
            }
        }

    }
}