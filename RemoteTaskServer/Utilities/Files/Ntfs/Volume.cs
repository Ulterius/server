#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UlteriusServer.Api.Win32;

#endregion

namespace UlteriusServer.Utilities.Files.Ntfs
{
    internal class Volume
    {
        public void EnumerateVolume(string drive, HashSet<string> fileExtensions
            , out Dictionary<ulong, FileEntry> files
            , out Dictionary<ulong, FileEntry> directories
            )
        {
            directories = new Dictionary<ulong, FileEntry>();
            files = new Dictionary<ulong, FileEntry>();
            var medBuffer = IntPtr.Zero;
            var changeJournalRootHandle = IntPtr.Zero;
            try
            {
                GetRootFrnEntry(drive, directories);
                GetRootHandle(drive, out changeJournalRootHandle);
                CreateChangeJournal(changeJournalRootHandle);
                SetupMFT_Enum_DataBuffer(ref medBuffer, changeJournalRootHandle);
                EnumerateFiles(medBuffer, ref files, fileExtensions, directories, changeJournalRootHandle);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message, e);
                var innerException = e.InnerException;
                while (innerException != null)
                {
                    Console.WriteLine(innerException.Message, innerException);
                    innerException = innerException.InnerException;
                }
                throw new ApplicationException("Error in EnumerateVolume()", e);
            }
            finally
            {

                if (changeJournalRootHandle.ToInt64() != WinApi.InvalidHandleValue)
                {
                    WinApi.CloseHandle(changeJournalRootHandle);
                }
                if (medBuffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(medBuffer);
                }
            }
        }

        private void GetRootFrnEntry(string drive, Dictionary<ulong, FileEntry> directories)
        {
            var driveRoot = string.Concat("\\\\.\\", drive);
            driveRoot = string.Concat(driveRoot, Path.DirectorySeparatorChar);
            var hRoot = WinApi.CreateFile(driveRoot,
                0,
                WinApi.FileShareRead | WinApi.FileShareWrite,
                IntPtr.Zero,
                WinApi.OpenExisting,
                WinApi.FileFlagBackupSemantics,
                IntPtr.Zero
                );

            if (hRoot.ToInt64() != WinApi.InvalidHandleValue)
            {
                WinApi.ByHandleFileInformation fi;
                var bRtn = WinApi.GetFileInformationByHandle(hRoot, out fi);
                if (bRtn)
                {
                    ulong fileIndexHigh = fi.FileIndexHigh;
                    var indexRoot = (fileIndexHigh << 32) | fi.FileIndexLow;

                    var f = new FileEntry(driveRoot, 0);
                    directories.Add(indexRoot, f);
                }
                else
                {
                    throw new IOException("GetFileInformationbyHandle() returned invalid handle",
                        new Win32Exception(Marshal.GetLastWin32Error()));
                }
                WinApi.CloseHandle(hRoot);
            }
            else
            {
                throw new IOException("Unable to get root frn entry", new Win32Exception(Marshal.GetLastWin32Error()));
            }
        }

        private void GetRootHandle(string drive, out IntPtr changeJournalRootHandle)
        {
            var vol = string.Concat("\\\\.\\", drive);
            changeJournalRootHandle = WinApi.CreateFile(vol,
                WinApi.GenericRead | WinApi.GenericWrite,
                WinApi.FileShareRead | WinApi.FileShareWrite,
                IntPtr.Zero,
                WinApi.OpenExisting,
                0,
                IntPtr.Zero);
            if (changeJournalRootHandle.ToInt64() == WinApi.InvalidHandleValue)
            {
                throw new IOException("CreateFile() returned invalid handle",
                    new Win32Exception(Marshal.GetLastWin32Error()));
            }
        }


        private unsafe void EnumerateFiles(IntPtr medBuffer
            , ref Dictionary<ulong, FileEntry> files
            , HashSet<string> fileExtensions
            , Dictionary<ulong, FileEntry> directories
            , IntPtr changeJournalRootHandle
            )
        {
            var pData = Marshal.AllocHGlobal(sizeof(ulong) + 0x10000);
            WinApi.ZeroMemory(pData, sizeof(ulong) + 0x10000);
            uint outBytesReturned;

            while (WinApi.DeviceIoControl(changeJournalRootHandle, WinApi.FsctlEnumUsnData, medBuffer,
                sizeof(WinApi.MftEnumData), pData, sizeof(ulong) + 0x10000, out outBytesReturned,
                IntPtr.Zero))
            {
                var pUsnRecord = new IntPtr(pData.ToInt64() + sizeof(long));
                while (outBytesReturned > 60)
                {
                    var usn = new WinApi.UsnRecord(pUsnRecord);

                    if (0 != (usn.FileAttributes & WinApi.FileAttributeDirectory))
                    {
                        //
                        // handle directories
                        //
                        if (!directories.ContainsKey(usn.FileReferenceNumber))
                        {
                            directories.Add(usn.FileReferenceNumber,
                                new FileEntry(usn.FileName, usn.ParentFileReferenceNumber));
                        }
                        else
                        {
                            // this is debug code and should be removed when we are certain that
                            // duplicate frn's don't exist on a given drive.  To date, this exception has
                            // never been thrown.  Removing this code improves performance....
                            throw new Exception($"Duplicate FRN: {usn.FileReferenceNumber} for {usn.FileName}"
                                );
                        }
                    }
                    else
                    {
                        //
                        // handle files
                        //
                        var add = true;
                        if (fileExtensions != null)
                        {
                            var s = Path.GetExtension(usn.FileName);
                            add = fileExtensions.Contains(s);
                        }
                        if (add)
                        {
                            if (!files.ContainsKey(usn.FileReferenceNumber))
                            {
                                files.Add(usn.FileReferenceNumber,
                                    new FileEntry(usn.FileName, usn.ParentFileReferenceNumber));
                            }
                            else
                            {
                                var frn = files[usn.FileReferenceNumber];

                                if (0 != string.Compare(usn.FileName, frn.Name, StringComparison.OrdinalIgnoreCase))
                                {
                                    Console.WriteLine(
                                        $"Attempt to add duplicate file reference number: {usn.FileReferenceNumber} for file {usn.FileName}, file from index {frn.Name}");
                                    throw new Exception(
                                        $"Duplicate FRN: {usn.FileReferenceNumber} for {usn.FileName}"
                                        );
                                }
                            }
                        }
                    }
                    pUsnRecord = new IntPtr(pUsnRecord.ToInt64() + usn.RecordLength);
                    outBytesReturned -= usn.RecordLength;
                }
                Marshal.WriteInt64(medBuffer, Marshal.ReadInt64(pData, 0));
            }
            Marshal.FreeHGlobal(pData);
        }

        private void CreateChangeJournal(IntPtr changeJournalRootHandle)
        {
            // This function creates a journal on the volume. If a journal already
            // exists this function will adjust the MaximumSize and AllocationDelta
            // parameters of the journal
            ulong MaximumSize = 0x800000;
            ulong AllocationDelta = 0x100000;
            uint cb;
            WinApi.CreateUsnJournalData cujd;
            cujd.MaximumSize = MaximumSize;
            cujd.AllocationDelta = AllocationDelta;

            var sizeCujd = Marshal.SizeOf(cujd);
            var cujdBuffer = Marshal.AllocHGlobal(sizeCujd);
            WinApi.ZeroMemory(cujdBuffer, sizeCujd);
            Marshal.StructureToPtr(cujd, cujdBuffer, true);

            var fOk = WinApi.DeviceIoControl(changeJournalRootHandle, WinApi.FsctlCreateUsnJournal,
                cujdBuffer, sizeCujd, IntPtr.Zero, 0, out cb, IntPtr.Zero);
            if (!fOk)
            {
                throw new IOException("DeviceIoControl() returned false",
                    new Win32Exception(Marshal.GetLastWin32Error()));
            }
        }


        private unsafe void SetupMFT_Enum_DataBuffer(ref IntPtr medBuffer, IntPtr changeJournalRootHandle)
        {
            uint bytesReturned;
            var ujd = new WinApi.UsnJournalData();

            var bOk = WinApi.DeviceIoControl(
                changeJournalRootHandle, // Handle to drive
                WinApi.FsctlQueryUsnJournal, // IO Control Code
                IntPtr.Zero, // In Buffer
                0, // In Buffer Size
                out ujd, // Out Buffer
                sizeof(WinApi.UsnJournalData), // Size Of Out Buffer
                out bytesReturned, // Bytes Returned
                IntPtr.Zero // lpOverlapped
                );
            if (bOk)
            {
                WinApi.MftEnumData med;
                med.StartFileReferenceNumber = 0;
                med.LowUsn = 0;
                med.HighUsn = ujd.NextUsn;
                var sizeMftEnumData = Marshal.SizeOf(med);
                medBuffer = Marshal.AllocHGlobal(sizeMftEnumData);
                WinApi.ZeroMemory(medBuffer, sizeMftEnumData);
                Marshal.StructureToPtr(med, medBuffer, true);
            }
            else
            {
                throw new IOException("DeviceIoControl() returned false",
                    new Win32Exception(Marshal.GetLastWin32Error()));
            }
        }
    }
}