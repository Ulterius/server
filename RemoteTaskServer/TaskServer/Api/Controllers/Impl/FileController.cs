#region

using System.IO;
using System.Linq;
using UlteriusServer.TaskServer.Api.Serialization;
using UlteriusServer.Utilities.Files;
using vtortola.WebSockets;
using File = System.IO.File;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers.Impl
{
    public class FileController : ApiController
    {
        private readonly WebSocket _client;
        private readonly Packets packet;
        private readonly ApiSerializator serializator = new ApiSerializator();

        public FileController(WebSocket client, Packets packet)
        {
            this._client = client;
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
            serializator.Serialize(_client, packet.endpoint, packet.syncKey, tree);
        }

        public void DownloadFile()
        {
            var path = packet.args.First().ToString();
            if (File.Exists(path))
            {
                var fileName = Path.GetFileName(path);
                var size = new FileInfo(path).Length;
                var data = new
                {
                    fileValid = true,
                    fileName,
                    size
                };
                serializator.Serialize(_client, packet.endpoint, packet.syncKey, data);
                serializator.PushFile(_client, path);
            }
            else
            {
                var data = new
                {
                    fileValid = false
                };
                serializator.Serialize(_client, packet.endpoint, packet.syncKey, data);
            }
        }
    }
}