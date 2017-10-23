using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace UlteriusServer.Utilities.Extensions
{
    public static class ProcessExtensions
    {
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr parentWindow, IntPtr previousChildWindow, string windowClass,
            string windowTitle);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowThreadProcessId(IntPtr window, out int process);

        public static IntPtr[] GetProcessWindows(this Process process)
        {
            var apRet = new IntPtr[256];
            var iCount = 0;
            var pLast = IntPtr.Zero;
            do
            {
                pLast = FindWindowEx(IntPtr.Zero, pLast, null, null);
                int iProcess_;
                GetWindowThreadProcessId(pLast, out iProcess_);
                if (iProcess_ == process.Id)
                {
                    apRet[iCount++] = pLast;
                }
            } while (pLast != IntPtr.Zero);
            Array.Resize(ref apRet, iCount);
            return apRet;
        }

        private static string FindIndexedProcessName(int pid)
        {
            try
            {
                var processName = Process.GetProcessById(pid).ProcessName;
                var processesByName = Process.GetProcessesByName(processName);
                string processIndexdName = null;
                for (var index = 0; index < processesByName.Length; index++)
                {
                    processIndexdName = index == 0 ? processName : processName + "#" + index;
                    var processId = new PerformanceCounter("Process", "ID Process", processIndexdName);
                    if ((int)processId.NextValue() == pid)
                    {
                        return processIndexdName;
                    }
                }
                return processIndexdName;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static List<Process> GetChildrenById(int id)
        {
            try
            {
                var mos = new ManagementObjectSearcher(
                    $"Select * From Win32_Process Where ParentProcessID={id}");
                return (from ManagementObject mo in mos.Get()
                    select Process.GetProcessById(Convert.ToInt32(mo["ProcessID"]))).ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static List<Process> GetChildProcesses(this Process process)
        {
            try
            {
                var mos = new ManagementObjectSearcher(
                    $"Select * From Win32_Process Where ParentProcessID={process.Id}");
                return (from ManagementObject mo in mos.Get()
                    select Process.GetProcessById(Convert.ToInt32(mo["ProcessID"]))).ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool IsService(this Process process)
        {
            if (process == null)
            {
                return false;
            }
            using (var searcher =
                new ManagementObjectSearcher("SELECT * FROM Win32_Service WHERE ProcessId =" + "\"" + process.Id +
                                             "\""))
            {
                if (searcher.Get().Cast<ManagementObject>().Any())
                {
                    return true;
                }
            }
            return false;
        }

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("psapi.dll")]
        private static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName,
            [In] [MarshalAs(UnmanagedType.U4)] int nSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetProcessName(this Process proc)
        {
            var processHandle = OpenProcess(0x0400 | 0x0010, false, proc.Id);

            if (processHandle == IntPtr.Zero)
            {
                return null;
            }

            const int lengthSb = 4000;

            var sb = new StringBuilder(lengthSb);

            string result = null;

            if (GetModuleFileNameEx(processHandle, IntPtr.Zero, sb, lengthSb) > 0)
            {
                result = Path.GetFileName(sb.ToString());
            }

            CloseHandle(processHandle);

            return result;
        }

        public static Process GetProcess(string path)
        {
            try
            {
                return Process.GetProcessesByName(Path.GetFileNameWithoutExtension(path)).FirstOrDefault();
            }
            catch
            {

                return null;
            }
        }

        private static Process FindPidFromIndexedProcessName(string indexedProcessName)
        {
            try
            {
                var parentId = new PerformanceCounter("Process", "Creating Process ID", indexedProcessName);
                return Process.GetProcessById((int)parentId.NextValue());
            }
            catch (Exception)
            {
                return null;
            }
        }


        public static void KillProcessAndChildren(this Process process)
        {
            try
            {
                var searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + process.Id);
                var moc = searcher.Get();
                foreach (var o in moc)
                {
                    var mo = (ManagementObject)o;
                    var child = Process.GetProcessById(Convert.ToInt32(mo["ProcessID"]));
                    KillProcessAndChildren(child);
                    child.Kill();
                }
                process.Kill();
            }
            catch
            {
                // Process already exited.
            }
        }

        public static bool ProgramIsRunning(string fullPath)
        {
            return Process.GetProcessesByName(Path.GetFileNameWithoutExtension(fullPath)).Count() > 1;
        }

        public static Process Parent(this Process process)
        {
            return FindPidFromIndexedProcessName(FindIndexedProcessName(process.Id));
        }
    }
}