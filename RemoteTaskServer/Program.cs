#region

using System;
using System.Diagnostics;
using System.Security.Principal;
using RemoteTaskServer.Api;
using RemoteTaskServer.Server;
using RemoteTaskServer.Utilities;
using RemoteTaskServer.Utilities.Network;
using RemoteTaskServer.Utilities.System;
using RemoteTaskServer.WebServer;

#endregion

namespace RemoteTaskServer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (!Debugger.IsAttached)
                ExceptionHandler.AddGlobalHandlers();

            var settings = new Settings();

            var myPrincipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            if (myPrincipal.IsInRole(WindowsBuiltInRole.Administrator) == false)
            {
                Console.WriteLine("Its recommended You need to elevate this server to administrator.");
            }

            var useWebServer = settings.Read("UseWebServer", "WebServer");
            if (useWebServer == "true")
            {
                var root = settings.Read("WebFilePath", "WebServer");
                var port = int.Parse(settings.Read("WebServerPort", "WebServer"));
                var httpServer = new HttpServer(root, port);
                Console.WriteLine("Web Server is running on this port: " + httpServer.Port);
            }

            Console.Title = "Strike Task Manager";
            TaskServer.Start();
            Console.WriteLine("Starting TaskServer on " + NetworkUtilities.GetIPv4Address() + ":" + TaskServer.boundPort);
            var systemUtilities = new SystemUtilities();
            systemUtilities.Start();
            Console.ReadLine();
        }
    }
}