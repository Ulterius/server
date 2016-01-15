#region

using UlteriusServer.TaskServer.Api.Serialization;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers.Impl
{
    public class ErrorController : ApiController
    {
        private readonly WebSocket client;
        private readonly Packets packet;
        private readonly ApiSerializator serializator = new ApiSerializator();

        public ErrorController(WebSocket client, Packets packet)
        {
            this.client = client;
            this.packet = packet;
        }

        public void InvalidPacket()
        {
            var invalidPacketData = new
            {
                invalidPacket = true,
                message = "This packet is invalid or empty"
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, invalidPacketData);
        }

        public void NoAuth()
        {
            var noAuthData = new
            {
                authRequired = true,
                message = "Please login to continue!"
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, noAuthData);
        }

        public void InvalidApiKey()
        {
            var invalidApiData = new
            {
                invalidApiKey = true,
                message = "Provided API Key does not match the server key"
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, invalidApiData);
        }

        public void EmptyApiKey()
        {
            var data = new
            {
                emptyApiKey = true,
                message = "Please generate an API Key"
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
        }
    }
}