#region

using System;
using System.IO;
using System.Runtime.InteropServices;
using UlteriusAgent.Networking;

#endregion

namespace UlteriusAgent
{
    internal class Program
    {
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        public static string LastDesktop;

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static void Main(string[] args)
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                SetProcessDPIAware();
            }
            var handle = GetConsoleWindow();

            // Hide
            ShowWindow(handle, SW_HIDE);

            Tools.KillAllButMe();
            try
            {
                AgentServer.Start();
                Console.WriteLine("Agent Started");
            }
            catch (Exception ex)
            {
                File.WriteAllText("Exception-" + Path.GetRandomFileName() + ".txt", ex.Message + " \n " + ex.StackTrace);
            }
        }
    }
}