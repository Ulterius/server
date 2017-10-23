using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UlteriusServer.Api.Win32
{
    public class DesktopWatcher
    {
        private static string _lastDesktop;
        private static Desktop _lastDesktopInput;
        public static void Start()
        {
            var backgroundThread = new Thread(WatchDesktop) { IsBackground = true };
            backgroundThread.Start();
            Console.WriteLine($"Desktop Task is running on thread: {Desktop.GetCurrentThreadId()}");
        }

        private static void WatchDesktop()
        {
            while (true)
            {
                try
                {
                    using (var inputDesktop = new Desktop())
                    {
                        inputDesktop.OpenInput();
                        if (!inputDesktop.DesktopName.Equals(_lastDesktop))
                        {
                            if (inputDesktop.Show() && Desktop.SetCurrent(inputDesktop))
                            {
                               Console.WriteLine($"Desktop switched from {_lastDesktop} to {inputDesktop.DesktopName} on thread {Desktop.GetCurrentThreadId()}");
                                _lastDesktop = inputDesktop.DesktopName;
                                _lastDesktopInput = inputDesktop;
                                CurrentDesktop = inputDesktop;
                            }
                            else
                            {
                                var errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                                Console.WriteLine(errorMessage);
                                _lastDesktopInput?.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message, ex);
                }
                Thread.Sleep(1000);
            }
        }

        public static string CurrentDesktopName { get; set; }

        public static Desktop CurrentDesktop { get; set; }
    }
}
