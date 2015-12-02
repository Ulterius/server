using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RemoteTaskServer.Server;
using RemoteTaskServer.WebServer;

namespace RemoteTaskServer
{
    class Program
    {
        private static void Main(string[] args)
        {
            Console.Title = "TaskServer Application";
            Console.WriteLine("Starting TaskServer on " + Packets.GetIPv4Address());

            string root = "D:/Documents/Visual Studio 2013/Projects/RemoteTaskServer/web/";
            HttpServer httpServer = new HttpServer(root, 9999);

            Console.WriteLine("Web Server is running on this port: " + httpServer.Port.ToString());

            TaskServer.Start();

  

            Console.ReadLine();
        }
    }

}
