#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic.Devices;
using OpenHardwareMonitor.Hardware;
using UlteriusServer.TaskServer.Api.Models;
using Computer = OpenHardwareMonitor.Hardware.Computer;

#endregion

namespace UlteriusServer.TaskServer.Services.System
{
    internal class SystemUtilities
    {
        private string biosCaption;
        private string biosManufacturer;
        private string biosSerial;
        private string cdRom;
        private string motherBoard;


        public void Start()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        SystemInformation.AvailableMemory = GetAvailablePhysicalMemory();
                        SystemInformation.Drives = GetDriveInformation();
                        SystemInformation.UsedMemory = GetUsedMemory();
                        SystemInformation.TotalMemory = GetTotalPhysicalMemory();
                        SystemInformation.RunningProcesses = GetTotalProcesses();
                        SystemInformation.UpTime = GetUpTime().TotalMilliseconds;
                        SystemInformation.RunningAsAdmin = IsRunningAsAdministrator();
                        SystemInformation.NetworkInfo = GetNetworkInfo();
                        SystemInformation.CpuUsage = GetPerformanceCounters();
                        SystemInformation.CpuTemps = GetCpuTemps();
                        SystemInformation.MotherBoard = GetMotherBoard();
                        SystemInformation.CdRom = GetCdRom();
                        SystemInformation.Bios = GetBiosInfo();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            });
        }

        private ulong GetAvailablePhysicalMemory()
        {
            return new ComputerInfo().AvailablePhysicalMemory;
        }

        public string GetMotherBoard()
        {
            if (!string.IsNullOrEmpty(motherBoard)) return motherBoard;
            var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_BaseBoard");
            foreach (var wmi in searcher.Get())
            {
                try
                {
                    motherBoard = wmi.GetPropertyValue("Product").ToString();
                    return motherBoard;
                }

                catch
                {
                    motherBoard = "Board Unknown";
                }
            }
            return motherBoard;
        }


        private object GetNetworkInfo()
        {
            long totalBytesReceived = 0;
            long totalBytesSent = 0;

            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                totalBytesReceived += networkInterface.GetIPv4Statistics().BytesReceived;
                totalBytesSent += networkInterface.GetIPv4Statistics().BytesSent;
            }


            var data = new
            {
                totalNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces().Length,
                networkInterfaces = NetworkInterface.GetAllNetworkInterfaces(),
                totalBytesReceived,
                totalBytesSent
            };


            return data;
        }

        public string GetCdRom()
        {
            if (!string.IsNullOrEmpty(cdRom)) return cdRom;
            var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_CDROMDrive");

            foreach (var wmi in searcher.Get())
            {
                try
                {
                    cdRom = wmi.GetPropertyValue("Drive").ToString();
                    return cdRom;
                }

                catch
                {
                    cdRom = "CD ROM Unknown";
                }
            }
            return cdRom;
        }

        private object GetBiosInfo()
        {
            var data = new
            {
                biosManufacturer = GetBiosManufacturer(),
                biosSerial = GetBiosSerial(),
                biosCaption = GetBiosCaption()
            };
            return data;
        }

        private string GetBiosSerial()
        {
            if (!string.IsNullOrEmpty(biosSerial)) return biosSerial;
            var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_BIOS");

            foreach (ManagementObject wmi in searcher.Get())
            {
                try
                {
                    biosSerial = wmi.GetPropertyValue("SerialNumber").ToString();
                    return biosSerial;
                }

                catch
                {
                    biosSerial = "Unknown BIOS Serial";
                }
            }

            return biosSerial;
        }

        private string GetBiosCaption()
        {
            if (!string.IsNullOrEmpty(biosCaption)) return biosCaption;
            var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_BIOS");

            foreach (var wmi in searcher.Get())
            {
                try
                {
                    biosCaption = wmi.GetPropertyValue("Caption").ToString();
                    return biosCaption;
                }
                catch
                {
                    biosCaption = "Unknown BIOS Caption";
                }
            }
            return biosCaption;
        }

        private string GetBiosManufacturer()
        {
            if (!string.IsNullOrEmpty(biosManufacturer)) return biosManufacturer;
            var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_BIOS");

            foreach (var wmi in searcher.Get())
            {
                try
                {
                    biosManufacturer = wmi.GetPropertyValue("Manufacturer").ToString();
                    return biosManufacturer;
                }

                catch
                {
                    biosManufacturer = "BIOS Manufacturer Unknown";
                }
            }

            return biosManufacturer;
        }


        private ulong GetUsedMemory()
        {
            return GetTotalPhysicalMemory() - GetAvailablePhysicalMemory();
        }

        private ulong GetTotalPhysicalMemory()
        {
            return new ComputerInfo().TotalPhysicalMemory;
        }

        private int GetTotalProcesses()
        {
            return Process.GetProcesses().Length;
        }

        private TimeSpan GetUpTime()
        {
            return TimeSpan.FromMilliseconds(GetTickCount64());
        }

        private bool IsRunningAsAdministrator()
        {
            var myPrincipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return myPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
        }


        public static Dictionary<string, List<EventLogEntry>> GetEventLogs()
        {
            /* var dictionary = new Dictionary<string, List<EventLogEntry>>();
            var d = EventLog.GetEventLogs();
            foreach (var l in d)
            {
                var categoryName = l.LogDisplayName;
                if (!dictionary.ContainsKey(categoryName))
                    dictionary.Add(categoryName, new List<EventLogEntry>());

                foreach (EventLogEntry entry in l.Entries)
                {
                    dictionary[categoryName].Add(entry);
                }
            }
            return dictionary;*/
            return null;
        }

        public List<DriveInformation> GetDriveInformation()
        {
            var q = new WqlObjectQuery("SELECT * FROM Win32_DiskDrive");
            var res = new ManagementObjectSearcher(q);
            var driveNames = (from ManagementBaseObject o in res.Get() select o["Model"]?.ToString()).ToList();
            var driveList = new List<DriveInformation>();
            var drives = DriveInfo.GetDrives();
            for (var index = 0; index < drives.Length; index++)
            {
                var drive = drives[index];
                var driveInfo = new DriveInformation();
                if (!drive.IsReady) continue;
                driveInfo.Model = driveNames.ElementAtOrDefault(index) != null ? driveNames[index] : "Unknown Model";
                driveInfo.Name = drive.Name;
                driveInfo.FreeSpace = drive.TotalFreeSpace;
                driveInfo.TotalSize = drive.TotalSize;
                driveInfo.DriveType = drive.DriveType.ToString();
                driveInfo.DriveFormat = drive.DriveFormat;
                driveInfo.VolumeLabel = drive.VolumeLabel;
                driveInfo.RootDirectory = drive.RootDirectory.ToString();
                driveInfo.IsReady = drive.IsReady;
                driveList.Add(driveInfo);
            }
            return driveList;
        }


        public List<float> GetPerformanceCounters()
        {
            var performanceCounters = new List<float>();
            var procCount = Environment.ProcessorCount;
            for (var i = 0; i < procCount; i++)
            {
                var pc = new PerformanceCounter("Processor", "% Processor Time", i.ToString());
                dynamic firstValue = pc.NextValue();
                Thread.Sleep(1000);
                // now matches task manager reading
                dynamic secondValue = pc.NextValue();
                performanceCounters.Add(secondValue);
            }
            return performanceCounters;
        }


        public List<float> GetCpuTemps()
        {
            var myComputer = new Computer();
            myComputer.Open();
            myComputer.CPUEnabled = true;
            return (from hardwareItem in myComputer.Hardware
                where hardwareItem.HardwareType == HardwareType.CPU
                from sensor in hardwareItem.Sensors
                where sensor.SensorType == SensorType.Temperature
                let value = sensor.Value
                where value != null
                where value != null
                select (float) value).ToList();
        }


        [DllImport("kernel32")]
        private static extern ulong GetTickCount64();
    }
}