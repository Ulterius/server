#region

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UlteriusAgent.Networking;

#endregion

namespace UlteriusAgent
{
    internal class Program
    {
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private static bool SetLogging()
        {
            try
            {
                var filestream = new FileStream(Path.Combine(AppEnvironment.DataPath, "agent.log"), FileMode.Create);
                var streamwriter = new StreamWriter(filestream, Encoding.UTF8) { AutoFlush = true };
                Console.SetOut(streamwriter);
                Console.SetError(streamwriter);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void Main(string[] args)
        {
            SetLogging();
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
            }
            catch (Exception ex)
            {
                File.WriteAllText("Exception-" + Path.GetRandomFileName() + ".txt", ex.Message + " \n " + ex.StackTrace);
            }
        }
    }
}