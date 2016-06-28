#region

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using RemoteTaskServer.WebServer;
using UlteriusServer.Plugins;
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
        private static bool _quitFlag;

        private static void Main(string[] args)
        {
            Console.CancelKeyPress += delegate { _quitFlag = true; };

            //fixes wrong screensize for screen share
            if (Environment.OSVersion.Version.Major >= 6) SetProcessDPIAware();

            if (!Directory.Exists(AppEnvironment.DataPath))
                Directory.CreateDirectory(AppEnvironment.DataPath);

            if (!Debugger.IsAttached)
                ExceptionHandler.AddGlobalHandlers();


            Console.WriteLine("Exception Handlers attached");


            Settings.Initialize("Config.json");

            Console.Title = Resources.Program_Title;
            Tools.ConfigureServer();
            var useTerminal = Convert.ToBoolean(Settings.Get("Terminal").AllowTerminal);
            var usePlugins = Convert.ToBoolean(Settings.Get("Plugins").LoadPlugins);
            var useWebServer = Convert.ToBoolean(Settings.Get("WebServer").UseWebServer);
            WebCamManager.LoadWebcams();
            if (usePlugins)
            {
                PluginHandler.LoadPlugins();
            }
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
            while (!_quitFlag)
            {
                Thread.Sleep(1);
            }
        }

        //Evan will have to support me and oumy cat once this gets released into the public.


        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
    }
}