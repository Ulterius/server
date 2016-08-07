#region

using UlteriusServer.Api.Network.Messages;
using UlteriusServer.Api.Network.Models;
using UlteriusServer.WebSocketAPI.Authentication;

#endregion

namespace UlteriusServer.Api.Network.PacketHandlers
{
    public class SystemPacketHandler : PacketHandler
    {
        private MessageBuilder _builder;
        private AuthClient _client;
        private Packet _packet;


        public void GetSystemInformation()
        {
            _builder.WriteMessage(SystemInformation.ToObject());
        }

        public override void HandlePacket(Packet packet)
        {
            _client = packet.AuthClient;
            _packet = packet;
            _builder = new MessageBuilder(_client, _packet.EndPoint, _packet.SyncKey);
            switch (_packet.PacketType)
            {
                case PacketManager.PacketTypes.RequestSystemInformation:
                    GetSystemInformation();
                    break;
            }
        }
    }
}