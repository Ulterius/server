#region

using System.Linq;
using System.Web.UI.WebControls;
using UlteriusServer.TaskServer.Api.Serialization;
using UlteriusServer.Utilities.Files;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers.Impl
{
    public class FileController : ApiController
    {
        private readonly WebSocket client;
        private readonly Packets packet;
        private readonly ApiSerializator serializator = new ApiSerializator();

        public FileController(WebSocket client, Packets packet)
        {
            this.client = client;
            this.packet = packet;
        }

        public void CreateFileTree()
        {
            var argumentSize = packet.args.Count;
            var path = packet.args.First().ToString();
            var deepWalk = false;
            if (argumentSize > 1)
            {
                deepWalk = (bool) packet.args[1];
            }
            var tree = new FileTree(path, deepWalk);
            serializator.Serialize(client, packet.endpoint, packet.syncKey, tree);
        }

        public void DownloadFile()
        {
            var path = packet.args.First().ToString();
            serializator.PushBinary(client, path);
        }
    }
}