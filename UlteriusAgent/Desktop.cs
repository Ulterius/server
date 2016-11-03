#region

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using WindowsInput;
using WindowsInput.Native;

#endregion

namespace UlteriusAgent
{
    /// <summary>
    ///     Encapsulates the Desktop API.
    /// </summary>
    public class Desktop : IDisposable, ICloneable
    {
        #region ICloneable

        /// <summary>
        ///     Creates a new Desktop object with the same desktop open.
        /// </summary>
        /// <returns>Cloned desktop object.</returns>
        public object Clone()
        {
            // make sure object isnt disposed.
            CheckDisposed();

            var desktop = new Desktop();

            // if a desktop is open, make the clone open it.
            if (IsOpen) desktop.Open(DesktopName);

            return desktop;
        }

        #endregion

        #region Overrides

        /// <summary>
        ///     Gets the desktop name.
        /// </summary>
        /// <returns>The desktop name, or a blank string if no desktop open.</returns>
        public override string ToString()
        {
            // return the desktop name.
            return DesktopName;
        }

        #endregion

        #region Imports


        public static int MOD_ALT = 0x1;
        public static int VK_DELETE = 0x2E;
        public static int MOD_CONTROL = 0x2;
        public static int MOD_SHIFT = 0x4;
        public static int MOD_WIN = 0x8;
        public static int WM_HOTKEY = 0x312;

        [DllImport("Kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.U4)]
        public static extern int WTSGetActiveConsoleSessionId();
        [DllImport("Sas.dll", SetLastError = true)]
        public static extern void SendSAS(bool asUser);
        [DllImport("kernel32.dll")]
        private static extern int GetThreadId(IntPtr thread);

        [DllImport("kernel32.dll")]
        private static extern int GetProcessId(IntPtr process);

        [DllImport("kernel32.dll")]
        private static extern int GetCurrentThreadId();
        /// <summary>
        ///     Retrieves a handle to the top-level window whose class name and window name match the specified strings. This
        ///     function does not search child windows. This function does not perform a case-sensitive search. To search child
        ///     windows, beginning with a specified child window, use the
        ///     <see cref="!:https://msdn.microsoft.com/en-us/library/windows/desktop/ms633500%28v=vs.85%29.aspx">FindWindowEx</see>
        ///     function.
        ///     <para>
        ///     Go to https://msdn.microsoft.com/en-us/library/windows/desktop/ms633499%28v=vs.85%29.aspx for FindWindow
        ///     information or https://msdn.microsoft.com/en-us/library/windows/desktop/ms633500%28v=vs.85%29.aspx for
        ///     FindWindowEx
        ///     </para>
        /// </summary>
        /// <param name="lpClassName">
        ///     C++ ( lpClassName [in, optional]. Type: LPCTSTR )<br />The class name or a class atom created by a previous call to
        ///     the RegisterClass or RegisterClassEx function. The atom must be in the low-order word of lpClassName; the
        ///     high-order word must be zero.
        ///     <para>
        ///     If lpClassName points to a string, it specifies the window class name. The class name can be any name
        ///     registered with RegisterClass or RegisterClassEx, or any of the predefined control-class names.
        ///     </para>
        ///     <para>If lpClassName is NULL, it finds any window whose title matches the lpWindowName parameter.</para>
        /// </param>
        /// <param name="lpWindowName">
        ///     C++ ( lpWindowName [in, optional]. Type: LPCTSTR )<br />The window name (the window's
        ///     title). If this parameter is NULL, all window names match.
        /// </param>
        /// <returns>
        ///     C++ ( Type: HWND )<br />If the function succeeds, the return value is a handle to the window that has the
        ///     specified class name and window name. If the function fails, the return value is NULL.
        ///     <para>To get extended error information, call GetLastError.</para>
        /// </returns>
        /// <remarks>
        ///     If the lpWindowName parameter is not NULL, FindWindow calls the <see cref="M:GetWindowText" /> function to
        ///     retrieve the window name for comparison. For a description of a potential problem that can arise, see the Remarks
        ///     for <see cref="M:GetWindowText" />.
        /// </remarks>
        // For Windows Mobile, replace user32.dll with coredll.dll
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        // Find window by Caption only. Note you must pass IntPtr.Zero as the first parameter.

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

        // You can also call FindWindow(default(string), lpWindowName) or FindWindow((string)null, lpWindowName)
        //
        // Imported winAPI functions.
        //
        [DllImport("user32.dll")]
        private static extern IntPtr CreateDesktop(string lpszDesktop, IntPtr lpszDevice, IntPtr pDevmode, int dwFlags,
            long dwDesiredAccess, IntPtr lpsa);

        [DllImport("user32.dll")]
        private static extern bool CloseDesktop(IntPtr hDesktop);

        [DllImport("user32.dll")]
        private static extern IntPtr OpenDesktop(string lpszDesktop, int dwFlags, bool fInherit, long dwDesiredAccess);

        [DllImport("user32.dll")]
        private static extern IntPtr OpenInputDesktop(int dwFlags, bool fInherit, long dwDesiredAccess);

        [DllImport("user32.dll")]
        private static extern bool SwitchDesktop(IntPtr hDesktop);

        [DllImport("user32.dll")]
        private static extern bool EnumDesktops(IntPtr hwinsta, EnumDesktopProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetProcessWindowStation();

        [DllImport("user32.dll")]
        private static extern bool EnumDesktopWindows(IntPtr hDesktop, EnumDesktopWindowsProc lpfn, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool SetThreadDesktop(IntPtr hDesktop);

        [return: MarshalAs(UnmanagedType.Bool)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CloseWindowStation(IntPtr hWinsta);


        [DllImport("user32.dll")]
        private static extern IntPtr GetThreadDesktop(int dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool GetUserObjectInformation(IntPtr hObj, int nIndex, IntPtr pvInfo, int nLength,
            ref int lpnLengthNeeded);

        [DllImport("kernel32.dll")]
        private static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            int dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            ref PROCESS_INFORMATION lpProcessInformation
            );

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, IntPtr lpString, int nMaxCount);

        private delegate bool EnumDesktopProc(string lpszDesktop, IntPtr lParam);

        private delegate bool EnumDesktopWindowsProc(IntPtr desktopHandle, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public readonly IntPtr hProcess;
            public readonly IntPtr hThread;
            public readonly int dwProcessId;
            public readonly int dwThreadId;
        }
        private static int MAKELPARAM(int p, int p_2)
        {
            return ((p_2 << 16) | (p & 0xFFFF));
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFO
        {
            public int cb;
            public readonly string lpReserved;
            public string lpDesktop;
            public readonly string lpTitle;
            public readonly int dwX;
            public readonly int dwY;
            public readonly int dwXSize;
            public readonly int dwYSize;
            public readonly int dwXCountChars;
            public readonly int dwYCountChars;
            public readonly int dwFillAttribute;
            public readonly int dwFlags;
            public readonly short wShowWindow;
            public readonly short cbReserved2;
            public readonly IntPtr lpReserved2;
            public readonly IntPtr hStdInput;
            public readonly IntPtr hStdOutput;
            public readonly IntPtr hStdError;
        }
        [Flags]
        public enum ACCESS_MASK : uint
        {
            DELETE = 0x00010000,
            READ_CONTROL = 0x00020000,
            WRITE_DAC = 0x00040000,
            WRITE_OWNER = 0x00080000,
            SYNCHRONIZE = 0x00100000,

            STANDARD_RIGHTS_REQUIRED = 0x000F0000,

            STANDARD_RIGHTS_READ = 0x00020000,
            STANDARD_RIGHTS_WRITE = 0x00020000,
            STANDARD_RIGHTS_EXECUTE = 0x00020000,

            STANDARD_RIGHTS_ALL = 0x001F0000,

            SPECIFIC_RIGHTS_ALL = 0x0000FFFF,

            ACCESS_SYSTEM_SECURITY = 0x01000000,

            MAXIMUM_ALLOWED = 0x02000000,

            GENERIC_READ = 0x80000000,
            GENERIC_WRITE = 0x40000000,
            GENERIC_EXECUTE = 0x20000000,
            GENERIC_ALL = 0x10000000,

            DESKTOP_READOBJECTS = 0x00000001,
            DESKTOP_CREATEWINDOW = 0x00000002,
            DESKTOP_CREATEMENU = 0x00000004,
            DESKTOP_HOOKCONTROL = 0x00000008,
            DESKTOP_JOURNALRECORD = 0x00000010,
            DESKTOP_JOURNALPLAYBACK = 0x00000020,
            DESKTOP_ENUMERATE = 0x00000040,
            DESKTOP_WRITEOBJECTS = 0x00000080,
            DESKTOP_SWITCHDESKTOP = 0x00000100,

            WINSTA_ENUMDESKTOPS = 0x00000001,
            WINSTA_READATTRIBUTES = 0x00000002,
            WINSTA_ACCESSCLIPBOARD = 0x00000004,
            WINSTA_CREATEDESKTOP = 0x00000008,
            WINSTA_WRITEATTRIBUTES = 0x00000010,
            WINSTA_ACCESSGLOBALATOMS = 0x00000020,
            WINSTA_EXITWINDOWS = 0x00000040,
            WINSTA_ENUMERATE = 0x00000100,
            WINSTA_READSCREEN = 0x00000200,

            WINSTA_ALL_ACCESS = 0x0000037F
        }
        [DllImport("User32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr OpenWindowStation(string lpszWinSta, bool fInherit, ACCESS_MASK dwDesiredAccess);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetProcessWindowStation(IntPtr hWinSta);

        [DllImport("User32.Dll")]
        public static extern long SetCursorPos(int x, int y);

        [DllImport("User32.Dll")]
        public static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        public static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);
        public static readonly IntPtr HWND_TOP = new IntPtr(0);
        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        public static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);

        [DllImport("User32.dll")]
        public static extern int PostMessage(IntPtr hWnd, int uMsg, int wParam, int lParam);
        #endregion

        #region Constants

        /// <summary>
        ///     Size of buffer used when retrieving window names.
        /// </summary>
        public const int MaxWindowNameLength = 100;

        //
        // winAPI constants.
        //
        private const short SW_HIDE = 0;
        private const short SW_NORMAL = 1;
        private const int STARTF_USESTDHANDLES = 0x00000100;
        private const int STARTF_USESHOWWINDOW = 0x00000001;
        private const int UOI_NAME = 2;
        private const int STARTF_USEPOSITION = 0x00000004;
        private const int NORMAL_PRIORITY_CLASS = 0x00000020;
        private const long DESKTOP_CREATEWINDOW = 0x0002L;
        private const long WINSTA_WRITEATTRIBUTES = 0x00000010;
        private const long DESKTOP_ENUMERATE = 0x0040L;
        private const long DESKTOP_WRITEOBJECTS = 0x0080L;
        private const long DESKTOP_SWITCHDESKTOP = 0x0100L;
        private const long DESKTOP_CREATEMENU = 0x0004L;
        private const long DESKTOP_HOOKCONTROL = 0x0008L;
        private const long DESKTOP_READOBJECTS = 0x0001L;
        private const long DESKTOP_JOURNALRECORD = 0x0010L;
        private const long DESKTOP_JOURNALPLAYBACK = 0x0020L;

        private const long AccessRights =
            DESKTOP_JOURNALRECORD | DESKTOP_JOURNALPLAYBACK | DESKTOP_CREATEWINDOW | DESKTOP_ENUMERATE |
            DESKTOP_WRITEOBJECTS | DESKTOP_SWITCHDESKTOP | DESKTOP_CREATEMENU | DESKTOP_HOOKCONTROL |
            DESKTOP_READOBJECTS | WINSTA_WRITEATTRIBUTES;

        #endregion

        #region Structures

        /// <summary>
        ///     Stores window handles and titles.
        /// </summary>
        public struct Window
        {
            #region Private Variables

            #endregion

            #region Public Properties

            /// <summary>
            ///     Gets the window handle.
            /// </summary>
            public IntPtr Handle { get; }

            /// <summary>
            ///     Gets teh window title.
            /// </summary>
            public string Text { get; }

            #endregion

            #region Construction

            /// <summary>
            ///     Creates a new window object.
            /// </summary>
            /// <param name="handle">Window handle.</param>
            /// <param name="text">Window title.</param>
            public Window(IntPtr handle, string text)
            {
                Handle = handle;
                Text = text;
            }

            #endregion
        }

        /// <summary>
        ///     A collection for Window objects.
        /// </summary>
        public class WindowCollection : CollectionBase
        {
            #region Public Properties

            /// <summary>
            ///     Gets a window from teh collection.
            /// </summary>
            public Window this[int index] => (Window) List[index];

            #endregion

            #region Methods

            /// <summary>
            ///     Adds a window to the collection.
            /// </summary>
            /// <param name="wnd">Window to add.</param>
            public void Add(Window wnd)
            {
                // adds a widow to the collection.
                List.Add(wnd);
            }

            #endregion
        }

        #endregion

        #region Private Variables

        private static StringCollection m_sc;
        private readonly ArrayList m_windows;
        private bool m_disposed;

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets if a desktop is open.
        /// </summary>
        public bool IsOpen => DesktopHandle != IntPtr.Zero;

        /// <summary>
        ///     Gets the name of the desktop, returns null if no desktop is open.
        /// </summary>
        public string DesktopName { get; private set; }

        /// <summary>
        ///     Gets a handle to the desktop, IntPtr.Zero if no desktop open.
        /// </summary>
        public IntPtr DesktopHandle { get; private set; }

        /// <summary>
        ///     Opens the default desktop.
        /// </summary>
        public static readonly Desktop Default = OpenDefaultDesktop();

        /// <summary>
        ///     Opens the desktop the user if viewing.
        /// </summary>
        public static readonly Desktop Input = OpenInputDesktop();

        #endregion

        #region Construction/Destruction

        /// <summary>
        ///     Creates a new Desktop object.
        /// </summary>
        public Desktop()
        {
            // init variables.
            DesktopHandle = IntPtr.Zero;
            DesktopName = string.Empty;
            m_windows = new ArrayList();
            m_disposed = false;
        }

        // constructor is private to prevent invalid handles being passed to it.
        private Desktop(IntPtr desktop)
        {
            // init variables.
            DesktopHandle = desktop;
            DesktopName = GetDesktopName(desktop);
            m_windows = new ArrayList();
            m_disposed = false;
        }

        ~Desktop()
        {
            // clean up, close the desktop.
            Close();
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Creates a new desktop.  If a handle is open, it will be closed.
        /// </summary>
        /// <param name="name">The name of the new desktop.  Must be unique, and is case sensitive.</param>
        /// <returns>True if desktop was successfully created, otherwise false.</returns>
        public bool Create(string name)
        {
            // make sure object isnt disposed.
            CheckDisposed();

            // close the open desktop.
            if (DesktopHandle != IntPtr.Zero)
            {
                // attempt to close the desktop.
                if (!Close()) return false;
            }

            // make sure desktop doesnt already exist.
            if (Exists(name))
            {
                // it exists, so open it.
                return Open(name);
            }

            // attempt to create desktop.
            DesktopHandle = CreateDesktop(name, IntPtr.Zero, IntPtr.Zero, 0, AccessRights, IntPtr.Zero);

            DesktopName = name;

            // something went wrong.
            if (DesktopHandle == IntPtr.Zero) return false;

            return true;
        }

        /// <summary>
        ///     Closes the handle to a desktop.
        /// </summary>
        /// <returns>True if an open handle was successfully closed.</returns>
        public bool Close()
        {
            // make sure object isnt disposed.
            CheckDisposed();

            // check there is a desktop open.
            if (DesktopHandle != IntPtr.Zero)
            {
                // close the desktop.
                var result = CloseDesktop(DesktopHandle);

                if (result)
                {
                    DesktopHandle = IntPtr.Zero;

                    DesktopName = string.Empty;
                }

                return result;
            }

            // no desktop was open, so desktop is closed.
            return true;
        }

        /// <summary>
        ///     Opens a desktop.
        /// </summary>
        /// <param name="name">The name of the desktop to open.</param>
        /// <returns>True if the desktop was successfully opened.</returns>
        public bool Open(string name)
        {
            // make sure object isnt disposed.
            CheckDisposed();

            // close the open desktop.
            if (DesktopHandle != IntPtr.Zero)
            {
                // attempt to close the desktop.
                if (!Close()) return false;
            }

            // open the desktop.
            DesktopHandle = OpenDesktop(name, 0, true, AccessRights);

            // something went wrong.
            if (DesktopHandle == IntPtr.Zero) return false;

            DesktopName = name;

            return true;
        }

        /// <summary>
        ///     Opens the current input desktop.
        /// </summary>
        /// <returns>True if the desktop was succesfully opened.</returns>
        public bool OpenInput()
        {
            // make sure object isnt disposed.
            CheckDisposed();

            // close the open desktop.
            if (DesktopHandle != IntPtr.Zero)
            {
                // attempt to close the desktop.
                if (!Close()) return false;
            }

            // open the desktop.
            DesktopHandle = OpenInputDesktop(0, true, AccessRights);

            // something went wrong.
            if (DesktopHandle == IntPtr.Zero) return false;

            // get the desktop name.
            DesktopName = GetDesktopName(DesktopHandle);

            return true;
        }

        /// <summary>
        ///     Switches input to the currently opened desktop.
        /// </summary>
        /// <returns>True if desktops were successfully switched.</returns>
        public bool Show()
        {
            // make sure object isnt disposed.
            CheckDisposed();

            // make sure there is a desktop to open.
            if (DesktopHandle == IntPtr.Zero) return false;

            // attempt to switch desktops.
            var result = SwitchDesktop(DesktopHandle);

            return result;
        }

        /// <summary>
        ///     Enumerates the windows on a desktop.
        /// </summary>
        /// <param name="windows">Array of Desktop.Window objects to recieve windows.</param>
        /// <returns>A window colleciton if successful, otherwise null.</returns>
        public WindowCollection GetWindows()
        {
            // make sure object isnt disposed.
            CheckDisposed();

            // make sure a desktop is open.
            if (!IsOpen) return null;

            // init the arraylist.
            m_windows.Clear();
            var windows = new WindowCollection();

            // get windows.
            var result = EnumDesktopWindows(DesktopHandle, DesktopWindowsProc, IntPtr.Zero);

            // check for error.
            if (!result) return null;

            // get window names.
            windows = new WindowCollection();

            var ptr = Marshal.AllocHGlobal(MaxWindowNameLength);

            foreach (IntPtr wnd in m_windows)
            {
                GetWindowText(wnd, ptr, MaxWindowNameLength);
                windows.Add(new Window(wnd, Marshal.PtrToStringAnsi(ptr)));
            }

            Marshal.FreeHGlobal(ptr);

            return windows;
        }

        private bool DesktopWindowsProc(IntPtr wndHandle, IntPtr lParam)
        {
            // add window handle to colleciton.
            m_windows.Add(wndHandle);

            return true;
        }

        /// <summary>
        ///     Creates a new process in a desktop.
        /// </summary>
        /// <param name="path">Path to application.</param>
        /// <returns>The process object for the newly created process.</returns>
        public Process CreateProcess(string path)
        {
            // make sure object isnt disposed.
            CheckDisposed();

            // make sure a desktop is open.
            if (!IsOpen) return null;

            // set startup parameters.
            var si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.lpDesktop = DesktopName;

            var pi = new PROCESS_INFORMATION();

            // start the process.
            var result = CreateProcess(null, path, IntPtr.Zero, IntPtr.Zero, true, NORMAL_PRIORITY_CLASS, IntPtr.Zero,
                null, ref si, ref pi);

            // error?
            if (!result) return null;

            // Get the process.
            return Process.GetProcessById(pi.dwProcessId);
        }

        /// <summary>
        ///     Prepares a desktop for use.  For use only on newly created desktops, call straight after CreateDesktop.
        /// </summary>
        public void Prepare()
        {
            // make sure object isnt disposed.
            CheckDisposed();

            // make sure a desktop is open.
            if (IsOpen)
            {
                // load explorer.
                CreateProcess("explorer.exe");
            }
        }

        #endregion

        #region Static Methods

        /// <summary>
        ///     Enumerates all of the desktops.
        /// </summary>
        /// <param name="desktops">String array to recieve desktop names.</param>
        /// <returns>True if desktop names were successfully enumerated.</returns>
        public static string[] GetDesktops()
        {
            // attempt to enum desktops.
            var windowStation = GetProcessWindowStation();

            // check we got a valid handle.
            if (windowStation == IntPtr.Zero) return new string[0];

            string[] desktops;

            // lock the object. thread safety and all.
            lock (m_sc = new StringCollection())
            {
                var result = EnumDesktops(windowStation, DesktopProc, IntPtr.Zero);

                // something went wrong.
                if (!result) return new string[0];

                //	// turn the collection into an array.
                desktops = new string[m_sc.Count];
                for (var i = 0; i < desktops.Length; i++) desktops[i] = m_sc[i];
            }

            return desktops;
        }

        private static bool DesktopProc(string lpszDesktop, IntPtr lParam)
        {
            // add the desktop to the collection.
            m_sc.Add(lpszDesktop);

            return true;
        }

        /// <summary>
        ///     Switches to the specified desktop.
        /// </summary>
        /// <param name="name">Name of desktop to switch input to.</param>
        /// <returns>True if desktops were successfully switched.</returns>
        public static bool Show(string name)
        {
            // attmempt to open desktop.
            var result = false;

            using (var d = new Desktop())
            {
                result = d.Open(name);

                // something went wrong.
                if (!result) return false;

                // attempt to switch desktops.
                result = d.Show();
            }

            return result;
        }

        /// <summary>
        ///     Gets the desktop of the calling thread.
        /// </summary>
        /// <returns>Returns a Desktop object for the valling thread.</returns>
        public static Desktop GetCurrent()
        {
            // get the desktop.
            return new Desktop(GetThreadDesktop(GetCurrentThreadId()));
        }

        public static bool SimulateCtrlAltDel()
        {

            SendSAS(false);
            return true;
        }

        public static int MakeLong(int low, int high)
        {
            return (high << 16) | (low & 0xffff);
        }

        /// <summary>
        ///     Sets the desktop of the calling thread.
        ///     NOTE: Function will fail if thread has hooks or windows in the current desktop.
        /// </summary>
        /// <param name="desktop">Desktop to put the thread in.</param>
        /// <returns>True if the threads desktop was successfully changed.</returns>
        public static bool SetCurrent(Desktop desktop)
        {
            // set threads desktop.
            return desktop.IsOpen && SetThreadDesktop(desktop.DesktopHandle);
        }

        /// <summary>
        ///     Opens a desktop.
        /// </summary>
        /// <param name="name">The name of the desktop to open.</param>
        /// <returns>If successful, a Desktop object, otherwise, null.</returns>
        public static Desktop OpenDesktop(string name)
        {
            // open the desktop.
            var desktop = new Desktop();
            var result = desktop.Open(name);

            // somethng went wrong.
            if (!result) return null;

            return desktop;
        }

        /// <summary>
        ///     Opens the current input desktop.
        /// </summary>
        /// <returns>If successful, a Desktop object, otherwise, null.</returns>
        public static Desktop OpenInputDesktop()
        {
            // open the desktop.
            var desktop = new Desktop();
            var result = desktop.OpenInput();

            // somethng went wrong.
            if (!result) return null;

            return desktop;
        }

        /// <summary>
        ///     Opens the default desktop.
        /// </summary>
        /// <returns>If successful, a Desktop object, otherwise, null.</returns>
        public static Desktop OpenDefaultDesktop()
        {
            // opens the default desktop.
            return OpenDesktop("Default");
        }

        /// <summary>
        ///     Creates a new desktop.
        /// </summary>
        /// <param name="name">The name of the desktop to create.  Names are case sensitive.</param>
        /// <returns>If successful, a Desktop object, otherwise, null.</returns>
        public static Desktop CreateDesktop(string name)
        {
            // open the desktop.
            var desktop = new Desktop();
            var result = desktop.Create(name);

            // somethng went wrong.
            if (!result) return null;

            return desktop;
        }

        /// <summary>
        ///     Gets the name of a given desktop.
        /// </summary>
        /// <param name="desktop">Desktop object whos name is to be found.</param>
        /// <returns>If successful, the desktop name, otherwise, null.</returns>
        public static string GetDesktopName(Desktop desktop)
        {
            // get name.
            if (desktop.IsOpen) return null;

            return GetDesktopName(desktop.DesktopHandle);
        }

        /// <summary>
        ///     Gets the name of a desktop from a desktop handle.
        /// </summary>
        /// <param name="desktopHandle"></param>
        /// <returns>If successful, the desktop name, otherwise, null.</returns>
        public static string GetDesktopName(IntPtr desktopHandle)
        {
            // check its not a null pointer.
            // null pointers wont work.
            if (desktopHandle == IntPtr.Zero) return null;

            // get the length of the name.
            var needed = 0;
            var name = string.Empty;
            GetUserObjectInformation(desktopHandle, UOI_NAME, IntPtr.Zero, 0, ref needed);

            // get the name.
            var ptr = Marshal.AllocHGlobal(needed);
            var result = GetUserObjectInformation(desktopHandle, UOI_NAME, ptr, needed, ref needed);
            name = Marshal.PtrToStringAnsi(ptr);
            Marshal.FreeHGlobal(ptr);

            // something went wrong.
            if (!result) return null;

            return name;
        }

        /// <summary>
        ///     Checks if the specified desktop exists (using a case sensitive search).
        /// </summary>
        /// <param name="name">The name of the desktop.</param>
        /// <returns>True if the desktop exists, otherwise false.</returns>
        public static bool Exists(string name)
        {
            return Exists(name, false);
        }

        /// <summary>
        ///     Checks if the specified desktop exists.
        /// </summary>
        /// <param name="name">The name of the desktop.</param>
        /// <param name="caseInsensitive">If the search is case INsensitive.</param>
        /// <returns>True if the desktop exists, otherwise false.</returns>
        public static bool Exists(string name, bool caseInsensitive)
        {
            // enumerate desktops.
            var desktops = GetDesktops();

            // return true if desktop exists.
            foreach (var desktop in desktops)
            {
                if (caseInsensitive)
                {
                    // case insensitive, compare all in lower case.
                    if (desktop.ToLower() == name.ToLower()) return true;
                }
                else
                {
                    if (desktop == name) return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Creates a new process on the specified desktop.
        /// </summary>
        /// <param name="path">Path to application.</param>
        /// <param name="desktop">Desktop name.</param>
        /// <returns>A Process object for the newly created process, otherwise, null.</returns>
        public static Process CreateProcess(string path, string desktop)
        {
            if (!Exists(desktop)) return null;

            // create the process.
            var d = OpenDesktop(desktop);
            return d.CreateProcess(path);
        }

        /// <summary>
        ///     Gets an array of all the processes running on the Input desktop.
        /// </summary>
        /// <returns>An array of the processes.</returns>
        public static Process[] GetInputProcesses()
        {
            // get all processes.
            var processes = Process.GetProcesses();

            var m_procs = new ArrayList();

            // get the current desktop name.
            var currentDesktop = GetDesktopName(Input.DesktopHandle);

            // cycle through the processes.
            foreach (var process in processes)
            {
                // check the threads of the process - are they in this one?
                if (
                    process.Threads.Cast<ProcessThread>()
                        .Any(pt => GetDesktopName(GetThreadDesktop(pt.Id)) == currentDesktop))
                {
                    m_procs.Add(process);
                }
            }

            // put ArrayList into array.
            var procs = new Process[m_procs.Count];

            for (var i = 0; i < procs.Length; i++) procs[i] = (Process) m_procs[i];

            return procs;
        }

        #endregion

        #region IDisposable

        /// <summary>
        ///     Dispose Object.
        /// </summary>
        public void Dispose()
        {
            // dispose
            Dispose(true);

            // suppress finalisation
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Dispose Object.
        /// </summary>
        /// <param name="disposing">True to dispose managed resources.</param>
        public virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                // dispose of managed resources,
                // close handles
                Close();
            }

            m_disposed = true;
        }

        private void CheckDisposed()
        {
            // check if disposed
            if (m_disposed)
            {
                // object disposed, throw exception
                throw new ObjectDisposedException("");
            }
        }

        #endregion
    }
}