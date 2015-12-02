using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteTaskServer.Server
{
     class ClientData
    {
        public static string id;
        public Socket clientSocket;
        public Thread clientThread;

        public ClientData()
        {
            id = Guid.NewGuid().ToString();
            clientThread = new Thread(TaskServer.DataReceived);
            clientThread.Start(clientSocket);
        }

        public ClientData(Socket clientSocket)
        {
            this.clientSocket = clientSocket;
            id = Guid.NewGuid().ToString();
            clientThread = new Thread(TaskServer.DataReceived);
            clientThread.Start(clientSocket);
        }
    }
}
