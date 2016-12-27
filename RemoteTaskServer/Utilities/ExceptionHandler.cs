#region

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Newtonsoft.Json;

#endregion

namespace UlteriusServer.Utilities
{
    public static class ExceptionHandler
    {

        [DllImport("Dbghelp.dll")]
        static extern bool MiniDumpWriteDump(IntPtr hProcess, uint ProcessId, IntPtr hFile, int DumpType, ref MINIDUMP_EXCEPTION_INFORMATION ExceptionParam, IntPtr UserStreamParam, IntPtr CallbackParam);
        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentProcessId();

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct MINIDUMP_EXCEPTION_INFORMATION
        {
            public uint ThreadId;
            public IntPtr ExceptionPointers;
            public int ClientPointers;
        }
        private static readonly int MiniDumpWithFullMemory = 2;
        public static readonly string LogsPath = Path.Combine(AppEnvironment.DataPath, "Logs");

        public static void AddGlobalHandlers()
        {
         
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                try
                {
                    if (!Directory.Exists(LogsPath))
                        Directory.CreateDirectory(LogsPath);

                    var dumpPath = Path.Combine(LogsPath,
                      $"UnhandledException_{DateTime.Now.ToShortDateString().Replace("/", "-")}_{Guid.NewGuid().ToString("N")}.dmp");

                    FileStream file = new FileStream(dumpPath, FileMode.Create);
                    MINIDUMP_EXCEPTION_INFORMATION info = new MINIDUMP_EXCEPTION_INFORMATION
                    {
                        ClientPointers = 1,
                        ExceptionPointers = Marshal.GetExceptionPointers(),
                        ThreadId = GetCurrentThreadId()
                    };
                    // A full memory dump is necessary in the case of a managed application, other wise no information
                    // regarding the managed code will be available
                    MiniDumpWriteDump(GetCurrentProcess(), GetCurrentProcessId(), file.SafeFileHandle.DangerousGetHandle(), MiniDumpWithFullMemory, ref info, IntPtr.Zero, IntPtr.Zero);
                    file.Close();



                    var filePath = Path.Combine(LogsPath,
                        $"UnhandledException_{DateTime.Now.ToShortDateString().Replace("/", "-")}_{Guid.NewGuid().ToString("N")}.json");

                    File.WriteAllText(filePath,
                        JsonConvert.SerializeObject(args.ExceptionObject, Formatting.Indented));

                    Console.WriteLine(@"An Unhandled Exception was Caught and Logged to:{0}", filePath);
                }
                catch
                {
                    // ignored
                }
            };


            Application.ThreadException += (sender, args) =>
            {
                try
                {
                    if (!Directory.Exists(LogsPath))
                        Directory.CreateDirectory(LogsPath);

                    var dumpPath = Path.Combine(LogsPath,
                     $"ThreadException_{DateTime.Now.ToShortDateString().Replace("/", "-")}_{Guid.NewGuid().ToString("N")}.dmp");

                    FileStream file = new FileStream(dumpPath, FileMode.Create);
                    MINIDUMP_EXCEPTION_INFORMATION info = new MINIDUMP_EXCEPTION_INFORMATION
                    {
                        ClientPointers = 1,
                        ExceptionPointers = Marshal.GetExceptionPointers(),
                        ThreadId = GetCurrentThreadId()
                    };
                    // A full memory dump is necessary in the case of a managed application, other wise no information
                    // regarding the managed code will be available
                    MiniDumpWriteDump(GetCurrentProcess(), GetCurrentProcessId(), file.SafeFileHandle.DangerousGetHandle(), MiniDumpWithFullMemory, ref info, IntPtr.Zero, IntPtr.Zero);
                    file.Close();

                    var filePath = Path.Combine(LogsPath,
                        $"ThreadException_{DateTime.Now.ToShortDateString().Replace("/", "-")}_{Guid.NewGuid().ToString("N")}.json");


                    File.WriteAllText(filePath,
                        JsonConvert.SerializeObject(args.Exception, Formatting.Indented));

                    Console.WriteLine($"An Unhandled Thread Exception was Caught and Logged to:\r\n{filePath}");
                }
                catch
                {
                    // ignored
                }
            };

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        }
    }
}