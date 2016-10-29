#region

using System;
using System.Runtime.InteropServices;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

#endregion

namespace UlteriusAgent.Api
{
    internal class WinApi
    {
        public enum FileIdType
        {
            FileIdType = 0,
            ObjectIdType = 1,
            ExtendedFileIdType = 2
        }

        [Flags]
        public enum MouseEventFlags : uint
        {
            MouseeventfMove = 0x0001,
            MouseeventfLeftdown = 0x0002,
            MouseeventfLeftup = 0x0004,
            MouseeventfRightdown = 0x0008,
            MouseeventfRightup = 0x0010,
            MouseeventfMiddledown = 0x0020,
            MouseeventfMiddleup = 0x0040,
            MouseeventfXdown = 0x0080,
            MouseeventfXup = 0x0100,
            MouseeventfWheel = 0x0800,
            MouseeventfVirtualdesk = 0x4000,
            MouseeventfAbsolute = 0x8000
        }

        public enum SendInputEventType
        {
            InputMouse,
            InputKeyboard,
            InputHardware
        }

        private const uint CF_UNICODETEXT = 13;
        // See http://msdn.microsoft.com/en-us/library/ms649021%28v=vs.85%29.aspx
        public const int WM_CLIPBOARDUPDATE = 0x031D;

        public const uint GenericRead = 0x80000000;
        public const uint GenericWrite = 0x40000000;
        public const uint FileShareRead = 0x00000001;
        public const uint FileShareWrite = 0x00000002;
        public const uint FileAttributeDirectory = 0x00000010;
        public const uint OpenExisting = 3;
        public const uint FileFlagBackupSemantics = 0x02000000;
        public const int InvalidHandleValue = -1;
        public const uint FsctlQueryUsnJournal = 0x000900f4;
        public const uint FsctlEnumUsnData = 0x000900b3;
        public const uint FsctlCreateUsnJournal = 0x000900e7;

        public const int SmCxscreen = 0;
        public const int SmCyscreen = 1;

        public const int CursorShowing = 0x00000001;
        public static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);
        public static readonly int Srccopy = 0x00CC0020;
        public static readonly int Srcpaint = 0x00EE0086;
        public static readonly int Captureblt = 0x40000000;
        public static readonly int StretchHalftone = 0x04;
        public static readonly uint PmNoremove = 0x00;

        public static readonly uint DibRgbColors = 0; /* color table in RGBs */
        public static readonly uint DibPalColors = 1; /* color table in palette indices */
        public static readonly uint BiRgb = 0;
        public static readonly uint BiRle8 = 1;
        public static readonly uint BiRle4 = 2;
        public static readonly uint BiBitfields = 3;

        [DllImport("user32.dll")]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll")]
        private static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern bool GlobalUnlock(IntPtr hMem);

        public static string GetText()
        {
            if (!IsClipboardFormatAvailable(CF_UNICODETEXT))
                return null;
            if (!OpenClipboard(IntPtr.Zero))
                return null;

            string data = null;
            var hGlobal = GetClipboardData(CF_UNICODETEXT);
            if (hGlobal != IntPtr.Zero)
            {
                var lpwcstr = GlobalLock(hGlobal);
                if (lpwcstr != IntPtr.Zero)
                {
                    data = Marshal.PtrToStringUni(lpwcstr);
                    GlobalUnlock(lpwcstr);
                }
            }
            CloseClipboard();

            return data;
        }

        // See http://msdn.microsoft.com/en-us/library/ms632599%28VS.85%29.aspx#message_only
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);

        // See http://msdn.microsoft.com/en-us/library/ms633541%28v=vs.85%29.aspx
        // See http://msdn.microsoft.com/en-us/library/ms649033%28VS.85%29.aspx
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("kernel32.dll")]
        public static extern uint GetCompressedFileSizeW([In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
            [Out, MarshalAs(UnmanagedType.U4)] out uint lpFileSizeHigh);

        [DllImport("kernel32.dll", SetLastError = true, PreserveSig = true)]
        public static extern int GetDiskFreeSpaceW([In, MarshalAs(UnmanagedType.LPWStr)] string lpRootPathName,
            out uint lpSectorsPerCluster, out uint lpBytesPerSector, out uint lpNumberOfFreeClusters,
            out uint lpTotalNumberOfClusters);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenFileById(IntPtr hFile, FileIdDescriptor desc, uint dwDesiredAccess,
            int dwShareMode, int lpSecurityAttributes, int dwFlagas);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess,
            uint dwShareMode, IntPtr lpSecurityAttributes,
            uint dwCreationDisposition, uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetFileInformationByHandle(IntPtr hFile,
            out ByHandleFileInformation lpFileInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeviceIoControl(IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer, int nInBufferSize,
            out UsnJournalData lpOutBuffer, int nOutBufferSize,
            out uint lpBytesReturned, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeviceIoControl(IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer, int nInBufferSize,
            IntPtr lpOutBuffer, int nOutBufferSize,
            out uint lpBytesReturned, IntPtr lpOverlapped);

        [DllImport("kernel32.dll")]
        public static extern void ZeroMemory(IntPtr ptr, int size);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindFirstFile(string lpFileName, out Win32FindData lpFindFileData);

        public static uint GetFileSizeA(string filename)
        {
            Win32FindData findData;
            FindFirstFile(filename, out findData);
            return findData.nFileSizeLow;
        }

        [DllImport("user32.dll", EntryPoint = "GetDesktopWindow")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", EntryPoint = "GetDC")]
        public static extern IntPtr GetDC(IntPtr ptr);

        [DllImport("user32.dll", EntryPoint = "GetSystemMetrics")]
        public static extern int GetSystemMetrics(int abc);

        [DllImport("user32.dll", EntryPoint = "GetWindowDC")]
        public static extern IntPtr GetWindowDC(int ptr);

        [DllImport("user32.dll", EntryPoint = "ReleaseDC")]
        public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDc);

        [DllImport("user32.dll")]
        public static extern int GetWindowRect(IntPtr hwnd, out Rect lpRect);

        [DllImport("user32.dll", EntryPoint = "GetCursorInfo")]
        public static extern bool GetCursorInfo(out Cursorinfo pci);

        [DllImport("user32.dll", EntryPoint = "CopyIcon")]
        public static extern IntPtr CopyIcon(IntPtr hIcon);

        [DllImport("user32.dll", EntryPoint = "GetIconInfo")]
        public static extern bool GetIconInfo(IntPtr hIcon, out Iconinfo piconinfo);

        [DllImport("user32.dll", EntryPoint = "DestroyIcon")]
        public static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        public static extern bool ClientToScreen(IntPtr hWnd, ref Point point);


        [DllImport("user32.dll")]
        public static extern long SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(SystemMetric smIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, ref Input pInputs, int cbSize);

        public static int CalculateAbsoluteCoordinateX(int x)
        {
            return x*65536/GetSystemMetrics(SystemMetric.SM_CXSCREEN);
        }

        public static int CalculateAbsoluteCoordinateY(int y)
        {
            return y*65536/GetSystemMetrics(SystemMetric.SM_CXSCREEN);
        }

        public static void LeftMouseButton(MouseEventFlags mouseEventFlags, int x, int y)
        {
            var mouseInput = new Input {type = SendInputEventType.InputMouse};
            mouseInput.mkhi.mi.Dx = CalculateAbsoluteCoordinateX(x);
            mouseInput.mkhi.mi.Dy = CalculateAbsoluteCoordinateY(y);
            mouseInput.mkhi.mi.MouseData = 0;

            mouseInput.mkhi.mi.DwFlags = MouseEventFlags.MouseeventfMove | MouseEventFlags.MouseeventfAbsolute;
            SendInput(1, ref mouseInput, Marshal.SizeOf(new Input()));

            mouseInput.mkhi.mi.DwFlags = mouseEventFlags;
            SendInput(1, ref mouseInput, Marshal.SizeOf(new Input()));
        }

        public static void ClickRightMouseButton(int x, int y)
        {
            var mouseInput = new Input {type = SendInputEventType.InputMouse};
            mouseInput.mkhi.mi.Dx = CalculateAbsoluteCoordinateX(x);
            mouseInput.mkhi.mi.Dy = CalculateAbsoluteCoordinateY(y);
            mouseInput.mkhi.mi.MouseData = 0;

            mouseInput.mkhi.mi.DwFlags = MouseEventFlags.MouseeventfMove | MouseEventFlags.MouseeventfAbsolute;
            SendInput(1, ref mouseInput, Marshal.SizeOf(new Input()));

            mouseInput.mkhi.mi.DwFlags = MouseEventFlags.MouseeventfRightdown;
            SendInput(1, ref mouseInput, Marshal.SizeOf(new Input()));

            mouseInput.mkhi.mi.DwFlags = MouseEventFlags.MouseeventfRightup;
            SendInput(1, ref mouseInput, Marshal.SizeOf(new Input()));
        }

        /// <summary>
        ///     Retrieves the cursor's position, in screen coordinates.
        /// </summary>
        /// <see>See MSDN documentation for further information.</see>
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out Point lpPoint);

        public static Point GetCursorPosition()
        {
            Point lpPoint;
            GetCursorPos(out lpPoint);
            //bool success = User32.GetCursorPos(out lpPoint);
            // if (!success)

            return lpPoint;
        }

        public static void MoveMouse(int x, int y)
        {
            var mouseInput = new Input {type = SendInputEventType.InputMouse};
            mouseInput.mkhi.mi.Dx = CalculateAbsoluteCoordinateX(x);
            mouseInput.mkhi.mi.Dy = CalculateAbsoluteCoordinateY(y);
            mouseInput.mkhi.mi.MouseData = 0;

            mouseInput.mkhi.mi.DwFlags = MouseEventFlags.MouseeventfMove | MouseEventFlags.MouseeventfAbsolute;
            SendInput(1, ref mouseInput, Marshal.SizeOf(new Input()));
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ByHandleFileInformation
        {
            public uint FileAttributes;
            public Filetime CreationTime;
            public Filetime LastAccessTime;
            public Filetime LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Filetime
        {
            public uint DateTimeLow;
            public uint DateTimeHigh;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct Win32FindData
        {
            public readonly uint dwFileAttributes;
            public readonly FILETIME ftCreationTime;
            public readonly FILETIME ftLastAccessTime;
            public readonly FILETIME ftLastWriteTime;
            public readonly uint nFileSizeHigh;
            public readonly uint nFileSizeLow;
            public readonly uint dwReserved0;
            public readonly uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public readonly string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)] public readonly string cAlternateFileName;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct UsnJournalData
        {
            public ulong UsnJournalID;
            public long FirstUsn;
            public long NextUsn;
            public long LowestValidUsn;
            public long MaxUsn;
            public ulong MaximumSize;
            public ulong AllocationDelta;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MftEnumData
        {
            public ulong StartFileReferenceNumber;
            public long LowUsn;
            public long HighUsn;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CreateUsnJournalData
        {
            public ulong MaximumSize;
            public ulong AllocationDelta;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct FileIdDescriptor
        {
            [FieldOffset(0)] public uint dwSize;
            [FieldOffset(4)] public FileIdType type;
            [FieldOffset(8)] public Guid guid;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct Bitmapinfoheader
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rgbquad
        {
            public byte rgbBlue;
            public byte rgbGreen;
            public byte rgbRed;
            public byte rgbReserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Bitmapinfo
        {
            public Bitmapinfoheader bmiHeader;
            public Rgbquad bmiColors_1;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Bitmap
        {
            public int bmType;
            public int bmWidth;
            public int bmHeight;
            public int bmWidthBytes;
            public byte bmPlanes;
            public byte bmBitsPixel;
            public IntPtr bmBits;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Iconinfo
        {
            public bool fIcon;
            // Specifies whether this structure defines an icon or a cursor. A value of TRUE specifies 

            public int xHotspot;
            // Specifies the x-coordinate of a cursor's hot spot. If this structure defines an icon, the hot 

            public int yHotspot;
            // Specifies the y-coordinate of the cursor's hot spot. If this structure defines an icon, the hot 

            public IntPtr hbmMask;
            // (HBITMAP) Specifies the icon bitmask bitmap. If this structure defines a black and white icon, 

            public IntPtr hbmColor; // (HBITMAP) Handle to the icon color bitmap. This member can be optional if this 
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Cursorinfo
        {
            public int cbSize; // Specifies the size, in bytes, of the structure. 
            public int flags; // Specifies the cursor state. This parameter can be one of the following values:
            public IntPtr hCursor; // Handle to the cursor. 
            public Point ptScreenPos; // A POINT structure that receives the screen coordinates of the cursor. 
        }

        private enum SystemMetric
        {
            SM_CXSCREEN = 0,
            SM_CYSCREEN = 1
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Hardwareinput
        {
            public int uMsg;
            public short wParamL;
            public short wParamH;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Keybdinput
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct MouseKeybdhardwareInputUnion
        {
            [FieldOffset(0)] public MouseInputData mi;

            [FieldOffset(0)] public Keybdinput ki;

            [FieldOffset(0)] public Hardwareinput hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Input
        {
            public SendInputEventType type;
            public MouseKeybdhardwareInputUnion mkhi;
        }

        public struct MouseInputData
        {
            public int Dx;
            public int Dy;
            public uint MouseData;
            public MouseEventFlags DwFlags;
            public uint Time;
            public IntPtr DwExtraInfo;
        }

        public class UsnRecord
        {
            private const int FrOffset = 8;
            private const int PfrOffset = 16;
            private const int FaOffset = 52;
            private const int FnlOffset = 56;
            private const int FnOffset = 58;
            public uint FileAttributes;
            public string FileName;
            public int FileNameLength;
            public int FileNameOffset;
            public ulong FileReferenceNumber;
            public ulong ParentFileReferenceNumber;
            public uint RecordLength;

            public UsnRecord(IntPtr p)
            {
                RecordLength = (uint) Marshal.ReadInt32(p);
                FileReferenceNumber = (ulong) Marshal.ReadInt64(p, FrOffset);
                ParentFileReferenceNumber = (ulong) Marshal.ReadInt64(p, PfrOffset);
                FileAttributes = (uint) Marshal.ReadInt32(p, FaOffset);
                FileNameLength = Marshal.ReadInt16(p, FnlOffset);
                FileNameOffset = Marshal.ReadInt16(p, FnOffset);
                FileName = Marshal.PtrToStringUni(new IntPtr(p.ToInt64() + FileNameOffset), FileNameLength/sizeof(char));
            }
        }
    }
}