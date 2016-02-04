#region

using UlteriusServer.TaskServer.Api.Models;
using UlteriusServer.TaskServer.Api.Serialization;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers.Impl
{
    public class SystemController : ApiController
    {
        private readonly WebSocket client;
        private readonly Packets packet;
        private readonly ApiSerializator serializator = new ApiSerializator();

        public SystemController(WebSocket client, Packets packet)
        {
            this.client = client;
            this.packet = packet;
        }

        public void GetSystemInformation()
        {
            try
            {
                serializator.Serialize(client, packet.endpoint, packet.syncKey, SystemInformation.ToObject());
            }
            catch (System.Exception e)
            {

                throw e;
            }
        }
    }
}