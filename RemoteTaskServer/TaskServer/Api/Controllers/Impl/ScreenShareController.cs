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
        private readonly ScreenShare _share = TaskManagerServer.ScreenShare;

        public ScreenShareController(WebSocket client, Packets packet)
        {
            _client = client;
            _packet = packet;
        }

        public void StopScreenShare()
        {
            if (_share.Stop())
            {
                var endData = new
                {
                    stopped = true,
                    shareName = _share.GetServerName(),
                    message = "Screenshare Stopped"
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, endData);
            }
            else
            {
                var endData = new
                {
                    stopped = false,
                    shareName = _share.GetServerName(),
                    message = "Screenshare Not Stopped"
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, endData);
            }
        }

        public void CheckServer()
        {
            var serverAvailable = _share.ServerAvailable();
            var endData = new
            {
                serverAvailable,
                shareName = _share.GetServerName()
            };
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, endData);
        }

        public void StartScreenShare()
        {
            if (!_share.ServerAvailable())
            {
                var endData = new
                {
                    started = false,
                    shareName = _share.GetServerName(),
                    message = "Server already running"
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, endData);
                return;
            }

            if (_share.Start())
            {
                var endData = new
                {
                    started = true,
                    shareName = _share.GetServerName(),
                    message = "Screenshare Started"
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, endData);
            }
            else
            {
                var endData = new
                {
                    started = false,
                    shareName = _share.GetServerName(),
                    message = "Screenshare Not Started"
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, endData);
            }
        }
    }
}

