#region

using UlteriusServer.TaskServer.Network.Messages;
using UlteriusServer.WebSocketAPI.Authentication;

#endregion

namespace UlteriusServer.TaskServer.Network.PacketHandlers
{
    public class ErrorPacketHandler : PacketHandler
    {
        private PacketBuilder _builder;
        private AuthClient _client;
        private Packet _packet;

        public override void HandlePacket(Packet packet)
        {
            _client = packet.AuthClient;
            _packet = packet;
            _builder = new PacketBuilder(_client, _packet.EndPoint, _packet.SyncKey);
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
                message = "This packet is invalid or empty"
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