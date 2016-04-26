#region

using UlteriusServer.TaskServer.Api.Serialization;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers.Impl
{
    public class ErrorController : ApiController
    {
        private readonly WebSocket _client;
        private readonly Packets _packet;
        private readonly ApiSerializator _serializator = new ApiSerializator();

        public ErrorController(WebSocket client, Packets packet)
        {
            _client = client;
            this._packet = packet;
        }

        public void InvalidPacket()
        {
            var invalidPacketData = new
            {
                invalidPacket = true,
                message = "This packet is invalid or empty"
            };
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, invalidPacketData);
        }

        public void NoAuth()
        {
            var noAuthData = new
            {
                authRequired = true,
                message = "Please login to continue!"
            };
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, noAuthData);
        }
    }
}