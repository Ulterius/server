#region

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using Topshelf;
using UlteriusServer.Forms.Utilities;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Usage;

#endregion

namespace UlteriusServer
{
    internal class Program
    {
        //Evan will have to support me and my cat once this gets released into the public.
        /// <summary>
        ///     Hide the console window from the user
        /// </summary>
        private static void HideWindow()
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

        private static void Main(string[] args)

        {
            //Fix screensize issues for Screen Share
            if (Environment.OSVersion.Version.Major >= 6)
            {
                SetProcessDPIAware();
            }
            HideWindow();
            try
            {
                if (
                    Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location))
                        .Count() > 1) return;

                ProfileOptimization.SetProfileRoot(AppEnvironment.DataPath);
                ProfileOptimization.StartProfile("Startup.Profile");

                if (args.Length > 0)
                {
                    HostFactory.Run(x => //1
                    {
                        x.Service<UlteriusAgent>(s => //2
                        {
                            s.ConstructUsing(name => new UlteriusAgent()); //3
                            s.WhenStarted(tc => tc.Start()); //4
                            s.WhenStopped(tc => tc.Stop());
                            s.WhenSessionChanged((se, e, id) => { se.HandleEvent(e, id); }); //5
                        });
                        x.RunAsLocalSystem(); //6
                        x.EnableSessionChanged();
                        x.EnableServiceRecovery(r => { r.RestartService(1); });
                        x.SetDescription("The server that powers Ulterius"); //7
                        x.SetDisplayName("Ulterius Server"); //8
                        x.SetServiceName("UlteriusServer"); //9
                    });
                }
                else
                {
                    var ulterius = new Ulterius();
                    ulterius.Start();
                    var hardware = new HardwareSurvey();
                    hardware.Setup();
                    if (Tools.RunningPlatform() == Tools.Platform.Windows)
                    {
                        UlteriusTray.ShowTray();
                    }
                    else
                    {
                        Console.ReadKey(true);
                    }
                }
            }
            catch
            {
                Console.WriteLine("Something unexpected occured");
            }
        }
    }
}