#region

using UlteriusServer.TaskServer.Network.Messages;
using UlteriusServer.WebSocketAPI.Authentication;

#endregion

namespace UlteriusServer.TaskServer.Network.PacketHandlers
{
    internal class ScreenSharePacketHandler : PacketHandler
    {
        private readonly ScreenShare _share = TaskManagerServer.ScreenShare;
        private PacketBuilder _builder;
        private AuthClient _client;
        private Packet _packet;


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
                _builder.WriteMessage(endData);
            }
            else
            {
                var endData = new
                {
                    stopped = false,
                    shareName = _share.GetServerName(),
                    message = "Screenshare Not Stopped"
                };
                _builder.WriteMessage(endData);
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
            _builder.WriteMessage(endData);
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
                _builder.WriteMessage(endData);
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
                _builder.WriteMessage(endData);
            }
            else
            {
                var endData = new
                {
                    started = false,
                    shareName = _share.GetServerName(),
                    message = "Screenshare Not Started"
                };
                _builder.WriteMessage(endData);
            }
        }

        public override void HandlePacket(Packet packet)
        {
            _client = packet.AuthClient;
            _packet = packet;
            _builder = new PacketBuilder(_client, _packet.EndPoint, _packet.SyncKey);
            switch (_packet.PacketType)
            {
                case PacketManager.PacketTypes.CheckScreenShare:
                    CheckServer();
                    break;
                case PacketManager.PacketTypes.StartScreenShare:
                    StartScreenShare();
                    break;
                case PacketManager.PacketTypes.StopScreenShare:
                    StopScreenShare();
                    break;
            }
        }
    }
}