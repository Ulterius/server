#region

using System;
using System.Diagnostics;
using RemoteTaskServer.WebServer;
using UlteriusServer.Properties;
using UlteriusServer.TaskServer;
using UlteriusServer.TaskServer.Services.System;
using UlteriusServer.TerminalServer;
using UlteriusServer.Utilities;
using UlteriusServer.WebCams;

#endregion

namespace UlteriusServer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Cleanup();
            if (!Debugger.IsAttached)
                ExceptionHandler.AddGlobalHandlers();
            Console.Title = Resources.Program_Title;
            Tools.ConfigureServer();
            var settings = new Settings();
            var useTerminal = settings.Read("Terminal", "AllowTerminal", true);
            var useWebServer = settings.Read("WebServer", "UseWebServer", true);
            WebCamManager.LoadWebcams();
            if (useWebServer)
            {
                HttpServer.Setup();
            }
            var systemUtilities = new SystemUtilities();
            systemUtilities.Start();
            //Keep down here if you actually want a functional program
            TaskManagerServer.Start();
            if (useTerminal)
            {
                TerminalManagerServer.Start();
            }
            Console.ReadLine();
        }

        //Evan will have to support me and oumy cat once this gets released into the public.
        private static void Cleanup()
        {
            var webSockifyInstances = Process.GetProcessesByName("websockify");
            foreach (var instance in webSockifyInstances)
            {
                try
                {
                    instance.Kill();
                }
                catch (Exception)
                {
                    //who cares
                }
            }
        }
    }
}