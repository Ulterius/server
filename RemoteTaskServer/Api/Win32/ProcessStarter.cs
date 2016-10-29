#region

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

#endregion

namespace UlteriusServer.Api.Win32
{
    /// <summary>
    ///     Class that allows running applications with full admin rights. In
    ///     addition the application launched will bypass the Vista UAC prompt.
    /// </summary>
    public class ProcessStarter
    {
        /// <summary>
        ///     Launches the given application with full admin rights, and in addition bypasses the Vista UAC prompt
        /// </summary>
        /// <param name="applicationName">The name of the application to launch</param>
        /// <param name="procInfo">Process information regarding the launched application that gets returned to the caller</param>
        /// <returns></returns>
        public static bool  StartProcessAndBypassUAC(string applicationName, out PROCESS_INFORMATION procInfo)
        {
            uint winlogonPid = 0;
            IntPtr hUserTokenDup = IntPtr.Zero, hPToken = IntPtr.Zero, hProcess = IntPtr.Zero;
            procInfo = new PROCESS_INFORMATION();

            // obtain the currently active session id; every logged on user in the system has a unique session id
            var dwSessionId = WTSGetActiveConsoleSessionId();

            // obtain the process id of the winlogon process that is running within the currently active session
            var processes = Process.GetProcessesByName("winlogon");
            foreach (var p in processes)
            {
                if ((uint) p.SessionId == dwSessionId)
                {
                    winlogonPid = (uint) p.Id;
                }
            }

            // obtain a handle to the winlogon process
            hProcess = OpenProcess(MAXIMUM_ALLOWED, false, winlogonPid);

            // obtain a handle to the access token of the winlogon process
            if (!OpenProcessToken(hProcess, TOKEN_DUPLICATE, ref hPToken))
            {
                CloseHandle(hProcess);
                return false;
            }

            // Security attibute structure used in DuplicateTokenEx and CreateProcessAsUser
            // I would prefer to not have to use a security attribute variable and to just 
            // simply pass null and inherit (by default) the security attributes
            // of the existing token. However, in C# structures are value types and therefore
            // cannot be assigned the null value.
            var sa = new SECURITY_ATTRIBUTES();
            sa.Length = Marshal.SizeOf(sa);

            // copy the access token of the winlogon process; the newly created token will be a primary token
            if (
                !DuplicateTokenEx(hPToken, MAXIMUM_ALLOWED, ref sa,
                    (int) SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, (int) TOKEN_TYPE.TokenPrimary,
                    ref hUserTokenDup))
            {
                CloseHandle(hProcess);
                CloseHandle(hPToken);
                return false;
            }

            // By default CreateProcessAsUser creates a process on a non-interactive window station, meaning
            // the window station has a desktop that is invisible and the process is incapable of receiving
            // user input. To remedy this we set the lpDesktop parameter to indicate we want to enable user 
            // interaction with the new process.
            var si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.lpDesktop = @"winsta0\default";
                // interactive window station parameter; basically this indicates that the process created can display a GUI on the desktop

            // flags that specify the priority and creation method of the process
            var dwCreationFlags = NORMAL_PRIORITY_CLASS | CREATE_NEW_CONSOLE;

            // create a new process in the current user's logon session
            var result = CreateProcessAsUser(hUserTokenDup, // client's access token
                null, // file to execute
                applicationName, // command line
                ref sa, // pointer to process SECURITY_ATTRIBUTES
                ref sa, // pointer to thread SECURITY_ATTRIBUTES
                false, // handles are not inheritable
                dwCreationFlags, // creation flags
                IntPtr.Zero, // pointer to new environment block 
                null, // name of current directory 
                ref si, // pointer to STARTUPINFO structure
                out procInfo // receives information about new process
                );

            // invalidate the handles
            CloseHandle(hProcess);
            CloseHandle(hPToken);
            CloseHandle(hUserTokenDup);

            return result; // return the result
        }

        #region Structures

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int Length;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        #endregion

        #region Enumerations

        private enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation = 2
        }

        private enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous = 0,
            SecurityIdentification = 1,
            SecurityImpersonation = 2,
            SecurityDelegation = 3
        }

        #endregion

        #region Constants

        public const int TOKEN_DUPLICATE = 0x0002;
        public const uint MAXIMUM_ALLOWED = 0x2000000;
        public const int CREATE_NEW_CONSOLE = 0x00000010;

        public const int IDLE_PRIORITY_CLASS = 0x40;
        public const int NORMAL_PRIORITY_CLASS = 0x20;
        public const int HIGH_PRIORITY_CLASS = 0x80;
        public const int REALTIME_PRIORITY_CLASS = 0x100;

        #endregion

        #region Win32 API Imports

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hSnapshot);

        [DllImport("kernel32.dll")]
        private static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUser", SetLastError = true, CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.StdCall)]
        public static extern bool CreateProcessAsUser(IntPtr hToken, string lpApplicationName, string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandle, int dwCreationFlags, IntPtr lpEnvironment,
            string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll")]
        private static extern bool ProcessIdToSessionId(uint dwProcessId, ref uint pSessionId);

        [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx")]
        public static extern bool DuplicateTokenEx(IntPtr ExistingTokenHandle, uint dwDesiredAccess,
            ref SECURITY_ATTRIBUTES lpThreadAttributes, int TokenType,
            int ImpersonationLevel, ref IntPtr DuplicateTokenHandle);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("advapi32", SetLastError = true), SuppressUnmanagedCodeSecurity]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, ref IntPtr TokenHandle);

        #endregion
    }
}