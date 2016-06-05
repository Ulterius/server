#region

using UlteriusServer.TaskServer.Api.Serialization;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers.Impl
{
    internal class ScreenShareController : ApiController
    {
        private readonly WebSocket _client;
        private readonly Packets _packet;
        private readonly ApiSerializator _serializator = new ApiSerializator();

        public ScreenShareController(WebSocket client, Packets packet)
        {
            _client = client;
            _packet = packet;
        }

        public void StopScreenShare()
        {
            var share = TaskManagerServer.ScreenShare;
            if (share.Stop())
            {
                var endData = new
                {
                    stopped = true,
                    shareName = share.GetServerName(),
                    message = "Screenshare Stopped"
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, endData);
            }
            else
            {
                var endData = new
                {
                    stopped = false,
                    shareName = share.GetServerName(),
                    message = "Screenshare Not Stopped"
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, endData);
            }
        }

        public void StartScreenShare()
        {
            var share = TaskManagerServer.ScreenShare;
            if (share.ServerAvailable())
            {
                if (share.Start())
                {
                    var endData = new
                    {
                        started = true,
                        shareName = share.GetServerName(),
                        message = "Screenshare Started"
                    };
                    _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, endData);
                }
                else
                {
                    var endData = new
                    {
                        started = false,
                        shareName = share.GetServerName(),
                        message = "Screenshare Not Started"
                    };
                    _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, endData);
                }
            }
            else
            {
                var endData = new
                {
                    started = false,
                    shareName = share.GetServerName(),
                    message = "Server already running"
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, endData);
            }
        }
    }
}