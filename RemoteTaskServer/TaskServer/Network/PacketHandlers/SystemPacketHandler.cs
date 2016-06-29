#region

using UlteriusServer.TaskServer.Network.Messages;
using UlteriusServer.TaskServer.Network.Models;
using UlteriusServer.WebSocketAPI.Authentication;

#endregion

namespace UlteriusServer.TaskServer.Network.PacketHandlers
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