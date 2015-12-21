using System;
using System.Security.Principal;
using RemoteTaskServer.Server;
using RemoteTaskServer.Utilities.Network;
using RemoteTaskServer.Utilities.System;
using RemoteTaskServer.WebServer;

namespace RemoteTaskServer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var myPrincipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            if (myPrincipal.IsInRole(WindowsBuiltInRole.Administrator) == false)
            {
                Console.WriteLine("Its recommended You need to elevate this server to administrator.");
            }
            else
            {
                Console.WriteLine("You are good to go - application running in elevated mode");
            }
            Console.Title = "TaskServer Application";
            Console.WriteLine("Starting TaskServer on " + Packets.GetIPv4Address());
            //var root = "D:/Documents/Visual Studio 2013/Projects/RemoteTaskServer/web/";
           // var httpServer = new HttpServer(root, 9999);
           // Console.WriteLine("Web Server is running on this port: " + httpServer.Port);
            TaskServer.Start();
            var systemUtilities = new SystemUtilities();
            systemUtilities.Start();
            Console.ReadLine();
        }
    }
}