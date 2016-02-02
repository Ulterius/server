#region

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using RemoteTaskServer.WebServer;
using UlteriusServer.Forms;
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
        [STAThread]
        private static void Main(string[] args)
        {
            Console.Title = Resources.Program_Title;
            if (!Debugger.IsAttached)
                ExceptionHandler.AddGlobalHandlers();
            if (args.Length > 0)
            {
                // Command line given, display gui
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Launcher());
            }
            else
            {
                AllocConsole();
                ConsoleMain(args);
            }
        }

        private static void ConsoleMain(string[] args)
        {
            Console.WriteLine("Command line = {0}", Environment.CommandLine);
            for (var ix = 0; ix < args.Length; ++ix)
                Console.WriteLine("Argument{0} = {1}", ix + 1, args[ix]);
            WebCamManager.LoadWebcams();
            PluginManager.LoadPlugins();
            Tools.GenerateSettings();
            HttpServer.Setup();
            var systemUtilities = new SystemUtilities();
            systemUtilities.Start();

            //Keep down here if you actually want a functional program
            TaskManagerServer.Start();
            TerminalManagerServer.Start();
            Console.ReadLine();
        }

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
    }
}