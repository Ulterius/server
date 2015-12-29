#region

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using RemoteTaskServer.Utilities;
using RemoteTaskServer.Utilities.Network;
using RemoteTaskServer.WebServer;
using UlteriusServer.Properties;
using UlteriusServer.Server;
using UlteriusServer.Utilities.System;

#endregion

namespace UlteriusServer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (!Debugger.IsAttached)
                ExceptionHandler.AddGlobalHandlers();

            var settings = new Settings();
            if (!File.Exists("UlteriusServer.ini"))
            {
                Console.WriteLine(Resources.Program_Main_Settings_didn_t_exist__writing_to_disk_);
                var bytes = new byte[Resources.UlteriusServer.Length*sizeof (char)];
                Buffer.BlockCopy(Resources.UlteriusServer.ToCharArray(), 0, bytes, 0, bytes.Length);
                File.WriteAllBytes("UlteriusServer.ini", bytes);
            }


            var myPrincipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            if (myPrincipal.IsInRole(WindowsBuiltInRole.Administrator) == false)
            {
                Console.WriteLine(
                    Resources.Program_Main_Its_recommended_You_need_to_elevate_this_server_to_administrator_);
            }

            var useWebServer = settings.Read("UseWebServer", "WebServer");
            if (useWebServer == "true")
            {
                var root = settings.Read("WebFilePath", "WebServer");
                var port = int.Parse(settings.Read("WebServerPort", "WebServer"));
                var httpServer = new HttpServer(root, port);
                Console.WriteLine(Resources.Program_Main_Web_Server_is_running_on_this_port__ + httpServer.Port);
            }

            Console.Title = Resources.Program_Title;
            TaskServer.Start();
            Console.WriteLine(Resources.Program_Main_Starting_TaskServer_on_ + NetworkUtilities.GetIPv4Address() + ":" +
                              TaskServer.boundPort);
            var systemUtilities = new SystemUtilities();
            systemUtilities.Start();

            Console.ReadLine();
        }
    }
}