#region

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using RemoteTaskServer.WebServer;
using UlteriusServer.Properties;
using UlteriusServer.Server;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Network;
using UlteriusServer.Utilities.System;
using UlteriusServer.Windows.Api;

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
               SettingsApi.GenerateSettings();
            }


            var myPrincipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            if (myPrincipal.IsInRole(WindowsBuiltInRole.Administrator) == false)
            {
                Console.WriteLine(
                    Resources.Program_Main_Its_recommended_You_need_to_elevate_this_server_to_administrator_);
            }

            var useWebServer = settings.Read("WebServer", "UseWebServer", false);
            if (useWebServer == true)
            {
                var root = settings.Read("WebServer", "WebFilePath", "");
                var port = settings.Read("WebServer", "WebServerPort", 9999);
                var httpServer = new HttpServer(root, port);
                Console.WriteLine(Resources.Program_Main_Web_Server_is_running_on_this_port__ + httpServer.Port);
            }

            Console.Title = Resources.Program_Title;
            var systemUtilities = new SystemUtilities();
            systemUtilities.Start();
            settings.Write("TaskServer", "ApiKey", "test");

            //Keep down here if you actually want a functional program
            TaskServer.Start();
            Console.ReadLine();
        }
    }
}