#region

using System;
using System.Runtime.InteropServices;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

#endregion

namespace UlteriusServer.Api.Win32
{
    internal class WinApi
    {
        public enum FileIdType
        {
            FileIdType = 0,
            ObjectIdType = 1,
            ExtendedFileIdType = 2
        }

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
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public readonly string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public readonly string cAlternateFileName;
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
            [FieldOffset(0)]
            public uint dwSize;
            [FieldOffset(4)]
            public FileIdType type;
            [FieldOffset(8)]
            public Guid guid;
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
                RecordLength = (uint)Marshal.ReadInt32(p);
                FileReferenceNumber = (ulong)Marshal.ReadInt64(p, FrOffset);
                ParentFileReferenceNumber = (ulong)Marshal.ReadInt64(p, PfrOffset);
                FileAttributes = (uint)Marshal.ReadInt32(p, FaOffset);
                FileNameLength = Marshal.ReadInt16(p, FnlOffset);
                FileNameOffset = Marshal.ReadInt16(p, FnOffset);
                FileName = Marshal.PtrToStringUni(new IntPtr(p.ToInt64() + FileNameOffset), FileNameLength / sizeof(char));
            }
        }
    }
}