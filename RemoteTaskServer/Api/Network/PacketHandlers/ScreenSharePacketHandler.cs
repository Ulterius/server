#region

using UlteriusServer.Api.Network.Messages;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.Api.Network.PacketHandlers
{
    internal class ScreenSharePacketHandler : PacketHandler
    {
        private readonly ScreenShareService _shareService = UlteriusApiServer.ScreenShareService;
        private MessageBuilder _builder;
        private AuthClient _authClient;
        private Packet _packet;
        private WebSocket _client;


        public void StopScreenShare()
        {
            if (_shareService.Stop())
            {
                var endData = new
                {
                    stopped = true,
                    shareName = _shareService.GetServerName(),
                    message = "Screenshare Stopped"
                };
                _builder.WriteMessage(endData);
            }
            else
            {
                var endData = new
                {
                    stopped = false,
                    shareName = _shareService.GetServerName(),
                    message = "Screenshare Not Stopped"
                };
                _builder.WriteMessage(endData);
            }
        }

        public void CheckServer()
        {
            var serverAvailable = _shareService.ServerAvailable();
            var endData = new
            {
                serverAvailable,
                shareName = _shareService.GetServerName()
            };
            _builder.WriteMessage(endData);
        }

        public void StartScreenShare()
        {
            if (!_shareService.ServerAvailable())
            {
                var endData = new
                {
                    started = false,
                    shareName = _shareService.GetServerName(),
                    message = "Server already running"
                };
                _builder.WriteMessage(endData);
                return;
            }

            if (_shareService.Start())
            {
                var endData = new
                {
                    started = true,
                    shareName = _shareService.GetServerName(),
                    message = "Screenshare Started"
                };
                _builder.WriteMessage(endData);
            }
            else
            {
                var endData = new
                {
                    started = false,
                    shareName = _shareService.GetServerName(),
                    message = "Screenshare Not Started"
                };
                _builder.WriteMessage(endData);
            }
        }

        public override void HandlePacket(Packet packet)
        {
            _client = packet.Client;
            _authClient = packet.AuthClient;
            _packet = packet;
            _builder = new MessageBuilder(_authClient, _client, _packet.EndPoint, _packet.SyncKey);
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