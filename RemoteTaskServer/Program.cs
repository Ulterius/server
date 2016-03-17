#region

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using RemoteTaskServer.WebServer;
using UlteriusServer.Forms.Utilities;
using UlteriusServer.Plugins;
using UlteriusServer.Properties;
using UlteriusServer.TaskServer;
using UlteriusServer.TaskServer.Api.Controllers.Impl;
using UlteriusServer.TaskServer.Services.System;
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

        [STAThread]
        private static void Main(string[] args)
        {
            var handle = GetConsoleWindow();
            // Hide
            ShowWindow(handle, SW_HIDE);
            var filestream = new FileStream("log.txt", FileMode.Create);
            var streamwriter = new StreamWriter(filestream) {AutoFlush = true};
            Console.SetOut(streamwriter);
            Console.SetError(streamwriter);
            Console.Title = Resources.Program_Title;
            if (!Debugger.IsAttached)
                ExceptionHandler.AddGlobalHandlers();

            var notifyThread = new Thread(
                  UlteriusTray.ShowTray);
            notifyThread.Start();
            AllocConsole();
            ConsoleMain(args);
        }


        private static void ConsoleMain(string[] args)
        {
            Tools.ConfigureServer();
            var settings = new Settings();
            var useTerminal = settings.Read("Terminal", "AllowTerminal", true);
            var usePlugins = settings.Read("Plugins", "LoadPlugins", true);
            var useWebServer = settings.Read("WebServer", "UseWebServer", true);
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
            Console.ReadLine();
        }


        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
    }
}