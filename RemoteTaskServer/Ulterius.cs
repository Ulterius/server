#region

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UlteriusServer.Api;
using UlteriusServer.Api.Services.System;
using UlteriusServer.Forms.Utilities;
using UlteriusServer.TerminalServer;
using UlteriusServer.Utilities;
using UlteriusServer.WebCams;
using UlteriusServer.WebServer;

#endregion

namespace UlteriusServer
{
    public class Ulterius
    {
        private SystemService systemService;
        private bool stop = false;
        public void Start()
        {
            if (Process.GetProcessesByName(
                Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location))
                .Length > 1)
            {
                Process.GetCurrentProcess().Kill();
            }
            if (!Directory.Exists(AppEnvironment.DataPath))
            {
                Directory.CreateDirectory(AppEnvironment.DataPath);
            }
            if (!Debugger.IsAttached)
            {
                ExceptionHandler.AddGlobalHandlers();
                Console.WriteLine("Exception Handlers Attached");
            }

            //Fix screensize issues for Screen Share
            if (Environment.OSVersion.Version.Major >= 6)
            {
                SetProcessDPIAware();
              }
           Setup();
         
        }

        /// <summary>
        /// Starts various parts of the server than loop to keep everything alive.
        /// </summary>
        private void Setup()
        {
            HideWindow();
            Console.WriteLine("Creating settings");
            Settings.Initialize("Config.json");
            Console.WriteLine("Configuring up server");
            Tools.ConfigureServer();
            var useTerminal = Convert.ToBoolean(Settings.Get("Terminal").AllowTerminal);
            var useWebServer = Convert.ToBoolean(Settings.Get("WebServer").ToggleWebServer);
            Console.WriteLine("Loading Webcams");
            WebCamManager.LoadWebcams();
            if (useWebServer)
            {
                Console.WriteLine("Setting up HTTP Server");
              HttpServer.Setup();
            }
            systemService = new SystemService();
            Console.WriteLine("Creating system service");
            systemService.Start();
            UlteriusApiServer.Start();
            if (useTerminal)
            {
                Console.WriteLine("Starting Terminal API");
                TerminalManagerServer.Start();
            }
            try
            {
                var useUpnp = Convert.ToBoolean(Settings.Get("Network").UPnpEnabled);
                if (useUpnp)
                {
                    Console.WriteLine("Trying to forward ports");
                  Tools.ForwardPorts();
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to forward ports");
            }
            UlteriusTray.ShowTray();
            while (!stop)
            {
                Thread.Sleep(1);
            }
        }


        /// <summary>
        ///     Hide the console window from the user
        /// </summary>
        private void HideWindow()
        {
            var handle = GetConsoleWindow();
            ShowWindow(handle, Hide);
        }

        #region win32

        private const int Hide = 0;

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();


        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        #endregion
    }
}