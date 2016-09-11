#region

using UlteriusServer.Api.Network.Messages;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.Api.Network.PacketHandlers
{
    public class ErrorPacketHandler : PacketHandler
    {
        private MessageBuilder _builder;
        private AuthClient _authClient;
        private Packet _packet;
        private WebSocket _client;

        public override void HandlePacket(Packet packet)
        {
            _client = packet.Client;
            _authClient = packet.AuthClient;
            _packet = packet;
            _builder = new MessageBuilder(_authClient, _client, _packet.EndPoint, _packet.SyncKey);
            switch (_packet.PacketType)
            {
                case PacketManager.PacketTypes.InvalidOrEmptyPacket:
                    InvalidPacket();
                    break;
                case PacketManager.PacketTypes.NoAuth:
                    NoAuth();
                    break;
            }
        }

        public void InvalidPacket()
        {
            var invalidPacketData = new
            {
                invalidPacket = true,
                message = "This packet is invalid or empty",
                invalidEndpoint = _packet.EndPoint
            };
            _builder.WriteMessage(invalidPacketData);
        }

        public void NoAuth()
        {
            var noAuthData = new
            {
                authRequired = true,
                message = "Please login to continue!"
            };
            _builder.WriteMessage(noAuthData);
        }
    }
}