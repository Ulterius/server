using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlteriusServer.TaskServer.Api.Serialization;
using vtortola.WebSockets;

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

        public void DownloadTestFile()
        {
            serializator.PushBinary(client, "D:/Pictures/Saver/kek.jpg");
        }
    }
}
