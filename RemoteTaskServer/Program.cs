#region

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using UlteriusServer.Utilities;
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

        private static void Main(string[] args)

        {
            //Fix screensize issues for Screen Share
            if (Environment.OSVersion.Version.Major >= 6)
                SetProcessDPIAware();
            HideWindow();
            try
            {
                if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location))
                        .Count() > 1)
                {
                    return;
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
                ProfileOptimization.SetProfileRoot(AppEnvironment.DataPath);
                ProfileOptimization.StartProfile("Startup.Profile");
                var ulterius = new Ulterius();
                ulterius.Start();
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Something unexpected occured");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
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