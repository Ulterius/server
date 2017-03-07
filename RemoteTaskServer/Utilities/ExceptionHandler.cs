#region

using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using AgentInterface.Settings;
using Newtonsoft.Json;

#endregion

namespace UlteriusServer.Utilities
{
    public static class ExceptionHandler
    {
        private static readonly int MiniDumpWithFullMemory = 2;
        public static readonly string LogsPath;
        public static readonly string DumpPath;

        [DllImport("Dbghelp.dll")]
        private static extern bool MiniDumpWriteDump(IntPtr hProcess, uint processId, IntPtr hFile, int dumpType,
            ref MinidumpExceptionInformation exceptionParam, IntPtr userStreamParam, IntPtr callbackParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentProcessId();

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        static ExceptionHandler()
        {
            LogsPath = Path.Combine(AppEnvironment.DataPath, "Logs");
            DumpPath = Path.Combine(LogsPath, "Dumps");;
            if (!Directory.Exists(LogsPath))
            {
                Directory.CreateDirectory(LogsPath);
                Directory.CreateDirectory(DumpPath);
            }
        }

        public static void AddGlobalHandlers()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // Catch all handled exceptions in managed code, before the runtime searches the Call Stack 
            AppDomain.CurrentDomain.FirstChanceException += FirstChanceException;

            // Catch all unhandled exceptions in all threads.
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;

            // Catch all unobserved task exceptions.
            TaskScheduler.UnobservedTaskException += UnobservedTaskException;

            // Catch all unhandled exceptions.
            Application.ThreadException += ThreadException;

            // Catch all WPF unhandled exceptions.
           Dispatcher.CurrentDispatcher.UnhandledException += DispatcherUnhandledException;
        }

        [HandleProcessCorruptedStateExceptions]
        private static void WriteLogs(string type, string information)
        {
          
            
           /* var dumpPath = Path.Combine(DumpPath,
                $"{type}_{DateTime.Now.ToShortDateString().Replace("/", "-")}_{Guid.NewGuid().ToString("N")}.dmp");
            using (var file = new FileStream(dumpPath, FileMode.Create))
            {
                var info = new MinidumpExceptionInformation
                {
                    ClientPointers = 1,
                    ExceptionPointers = Marshal.GetExceptionPointers(),
                    ThreadId = GetCurrentThreadId()
                };
                // A full memory dump is necessary in the case of a managed application, other wise no information
                // regarding the managed code will be available
                if (file.SafeFileHandle != null)
                    MiniDumpWriteDump(GetCurrentProcess(), GetCurrentProcessId(),
                        file.SafeFileHandle.DangerousGetHandle(), MiniDumpWithFullMemory, ref info, IntPtr.Zero,
                        IntPtr.Zero);
            }*/
            var filePath = Path.Combine(LogsPath,
                $"{type}_{DateTime.Now.ToShortDateString().Replace("/", "-")}_{Guid.NewGuid().ToString("N")}.json");

            File.WriteAllText(filePath, information);
        }

        [HandleProcessCorruptedStateExceptions]
        private static void DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                
                var type = "DispatcherUnhandledException";
                var information = JsonConvert.SerializeObject(e.Exception, Formatting.Indented);
                WriteLogs(type, information);
            }
            catch (Exception)
            {

                
            }
        }

        [HandleProcessCorruptedStateExceptions]
        private static void ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            try
            {
               
                var type = "ThreadException";
                var information = JsonConvert.SerializeObject(e.Exception, Formatting.Indented);
                WriteLogs(type, information);
            }
            catch (Exception)
            {

               
            }
        }

        [HandleProcessCorruptedStateExceptions]
        private static void UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                var type = "UnobservedTaskException";
                var information = JsonConvert.SerializeObject(e.Exception, Formatting.Indented);
                WriteLogs(type, information);
            }
            catch (Exception)
            {

                
            }
        }

        [HandleProcessCorruptedStateExceptions]
        private static void UnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            try
            {
                var e = (Exception)args.ExceptionObject;
               
                var type = "UnhandledException";
                var information = JsonConvert.SerializeObject(e, Formatting.Indented);
                WriteLogs(type, information);
            }
            catch (Exception)
            {

               
            }
        }

        /// <summary>
        /// Currently spamming the disk and is unused
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [HandleProcessCorruptedStateExceptions]
        private static void FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            
           //var type = "FirstChanceException";
          //  var information = JsonConvert.SerializeObject(e.Exception, Formatting.Indented);
          //  WriteLogs(type, information);
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct MinidumpExceptionInformation
        {
            public uint ThreadId;
            public IntPtr ExceptionPointers;
            public int ClientPointers;
        }
    }
}