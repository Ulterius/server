#region

using System;
using UlteriusServer.TaskServer.Network.Messages;
using UlteriusServer.TaskServer.Network.Models;
using UlteriusServer.TaskServer.Services.Network;
using UlteriusServer.WebSocketAPI.Authentication;
using static UlteriusServer.TaskServer.Network.PacketManager;

#endregion

namespace UlteriusServer.TaskServer.Network.PacketHandlers
{
    public class NetworkPacketHandler : PacketHandler
    {
        private MessageBuilder _builder;
        private AuthClient _client;
        private Packet _packet;


        public void GetNetworkInformation()
        {
            if (string.IsNullOrEmpty(NetworkInformation.PublicIp))
            {
                NetworkInformation.PublicIp = NetworkUtilities.GetPublicIp();
                NetworkInformation.NetworkComputers = NetworkUtilities.ConnectedDevices();
                NetworkInformation.MacAddress = NetworkUtilities.GetMacAddress().ToString();
                NetworkInformation.InternalIp = NetworkUtilities.GetIpAddress().ToString();
            }
            _builder.WriteMessage(NetworkInformation.ToObject());
        }

        public override void HandlePacket(Packet packet)
        {
            _client = packet.AuthClient;
            _packet = packet;
            _builder = new MessageBuilder(_client, _packet.EndPoint, _packet.SyncKey);
            switch (_packet.PacketType)
            {
                case PacketTypes.RequestNetworkInformation:
                    GetNetworkInformation();
                    break;
            }
        }
    }
}