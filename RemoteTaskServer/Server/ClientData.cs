#region

using System;
using System.Net.Sockets;
using System.Threading;

#endregion

namespace RemoteTaskServer.Server
{
    internal class ClientData
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