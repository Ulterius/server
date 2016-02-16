#region

using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
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
            _client = client;
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

        public void SearchFile()
        {
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
                    path,
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
                    path,
                    fileValid = false
                };
                serializator.Serialize(_client, packet.endpoint, packet.syncKey, data);
            }
        }


        public void UploadFile()
        {
            var path = packet.args.First().ToString();
            var fileName = Path.GetFileName(path);
            try
            {
                var dataArray = JsonConvert.DeserializeObject(packet.args[1].ToString(), typeof (sbyte[]));
                var unsigned = (byte[]) (Array) dataArray;
                File.WriteAllBytes(path, unsigned);
                var data = new
                {
                    path,
                    fileUploaded = true,
                    fileName
                };
                serializator.Serialize(_client, packet.endpoint, packet.syncKey, data);
            }
            catch (Exception)
            {
                var data = new
                {
                    path,
                    fileUploaded = false,
                    fileName
                };
                serializator.Serialize(_client, packet.endpoint, packet.syncKey, data);
            }
        }
    }
}