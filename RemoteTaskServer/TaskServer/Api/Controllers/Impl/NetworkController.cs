#region

using UlteriusServer.TaskServer.Api.Models;
using UlteriusServer.TaskServer.Api.Serialization;
using UlteriusServer.TaskServer.Services.Network;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers.Impl
{
    public class NetworkController : ApiController
    {
        private readonly WebSocket _client;
        private readonly Packets _packet;
        private readonly ApiSerializator _serializator = new ApiSerializator();

        public NetworkController(WebSocket client, Packets packet)
        {
            _client = client;
            _packet = packet;
        }


        public void GetNetworkInformation()
        {
            if (string.IsNullOrEmpty(NetworkInformation.PublicIp))
            {
                NetworkInformation.PublicIp = NetworkUtilities.GetPublicIp();
                NetworkInformation.NetworkComputers = NetworkUtilities.ConnectedDevices();
                NetworkInformation.MacAddress = NetworkUtilities.GetMacAddress().ToString();
                NetworkInformation.InternalIp = NetworkUtilities.GetIPAddress().ToString();
            }
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, NetworkInformation.ToObject());
        }
    }
}