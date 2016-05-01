#region

using UlteriusServer.TaskServer.Api.Models;
using UlteriusServer.TaskServer.Api.Serialization;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers.Impl
{
    public class SystemController : ApiController
    {
        private readonly WebSocket _client;
        private readonly Packets _packet;
        private readonly ApiSerializator _serializator = new ApiSerializator();

        public SystemController(WebSocket client, Packets packet)
        {
            _client = client;
            _packet = packet;
        }

        public void GetSystemInformation()
        {
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, SystemInformation.ToObject());
        }
    }
}