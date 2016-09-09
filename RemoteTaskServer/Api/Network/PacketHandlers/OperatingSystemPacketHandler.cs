#region

using System;
using System.Linq;
using System.Management;
using UlteriusServer.Api.Network.Messages;
using UlteriusServer.Api.Network.Models;
using UlteriusServer.Api.Services.LocalSystem;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.Api.Network.PacketHandlers
{
    public class OperatingSystemPacketHandler : PacketHandler
    {
        private MessageBuilder _builder;
        private AuthClient _authClient;
        private Packet _packet;
        private WebSocket _client;


        public void GetEventLogs()
        {
            _builder.WriteMessage(SystemService.GetEventLogs());
            GC.Collect();
        }

        public void GetOperatingSystemInformation()
        {
            _builder.WriteMessage(ServerOperatingSystem.ToObject());
        }

        public override void HandlePacket(Packet packet)
        {
            _client = packet.Client;
            _authClient = packet.AuthClient;
            _packet = packet;
            _builder = new MessageBuilder(_authClient, _client, _packet.EndPoint, _packet.SyncKey);
            switch (_packet.PacketType)
            {
                case PacketManager.PacketTypes.RequestOsInformation:
                    GetOperatingSystemInformation();
                    break;
                case PacketManager.PacketTypes.GetEventLogs:
                    // GetEventLogs();
                    break;
            }
        }
    }
}