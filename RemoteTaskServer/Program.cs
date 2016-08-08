#region

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using RemoteTaskServer.WebServer;
using UlteriusServer.Api;
using UlteriusServer.Api.Services.System;
using UlteriusServer.Forms.Utilities;
using UlteriusServer.Properties;
using UlteriusServer.TerminalServer;
using UlteriusServer.Utilities;
using UlteriusServer.WebCams;

#endregion

namespace UlteriusServer
{
    internal class Program
    {
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private static bool _quitFlag;

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static void Run()
        {
            var handle = GetConsoleWindow();
            // Hide
            ShowWindow(handle, SW_HIDE);
            Console.CancelKeyPress += delegate { _quitFlag = true; };


            if (!Directory.Exists(AppEnvironment.DataPath))
                Directory.CreateDirectory(AppEnvironment.DataPath);

            if (!Debugger.IsAttached)
                ExceptionHandler.AddGlobalHandlers();


            Console.WriteLine("Exception Handlers attached");


            Settings.Initialize("Config.json");

            Console.Title = Resources.Program_Title;
            Tools.ConfigureServer();
            var useTerminal = Convert.ToBoolean(Settings.Get("Terminal").AllowTerminal);
            //  var usePlugins = Convert.ToBoolean(Settings.Get("Plugins").LoadPlugins);
            var useWebServer = Convert.ToBoolean(Settings.Get("WebServer").ToggleWebServer);
            WebCamManager.LoadWebcams();
            // if (usePlugins)
            // {
            //   PluginHandler.LoadPlugins();
            //    }
            if (useWebServer)
            {
                HttpServer.Setup();
            }
            var systemUtilities = new SystemService();
            systemUtilities.Start();
            //Keep down here if you actually want a functional program
            UlteriusApiServer.Start();
            if (useTerminal)
            {
                TerminalManagerServer.Start();
            }
            Console.WriteLine("Opening");
            try
            {
                var useUpnp = Convert.ToBoolean(Settings.Get("Network").UPnpEnabled);
                if (useUpnp)
                {
                    Tools.OpenPort();
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to forward ports");
            }
            while (!_quitFlag)
            {
                Thread.Sleep(1);
            }
        }

        private static void Main(string[] args)
        {
            if (Process.GetProcessesByName(
                Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location))
                .Length > 1) Process.GetCurrentProcess().Kill();


            //fixes wrong screensize for screen share
            if (Environment.OSVersion.Version.Major >= 6) SetProcessDPIAware();
            Task.Factory.StartNew(Run);
            UlteriusTray.ShowTray();

        }

        //Evan will have to support me and my cat once this gets released into the public.


        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
    }
}