#region

using System;
using System.Diagnostics;
using System.IO;
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
            Settings.Initialize("Settings.json");
            Cleanup();

            if (!Debugger.IsAttached)          
                ExceptionHandler.AddGlobalHandlers();
                Console.WriteLine("Exception Handlers attached");

            if (!Directory.Exists(AppEnvironment.DataPath))
                Directory.CreateDirectory(AppEnvironment.DataPath);



            Console.Title = Resources.Program_Title;
            Tools.ConfigureServer();
            var useTerminal = Convert.ToBoolean(Settings.Get("Terminal").AllowTerminal);
            var useWebServer = Convert.ToBoolean(Settings.Get("WebServer").UseWebServer);
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