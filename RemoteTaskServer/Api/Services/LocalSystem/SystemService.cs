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
using UlteriusServer.Api.Network.Models;
using UlteriusServer.Api.Services.Network;
using Computer = OpenHardwareMonitor.Hardware.Computer;

#endregion

namespace UlteriusServer.Api.Services.LocalSystem
{
    internal class SystemService
    {
        private string _biosCaption;
        private string _biosManufacturer;
        private string _biosSerial;
        private string _cdRom;
        private string _motherBoard;

        private static ulong AvailablePhysicalMemory => new ComputerInfo().AvailablePhysicalMemory;


        public void Start()
        {
            //static info

            try
            {
                SetNetworkInformation();
                SetCpuInformation();
                SetOperatingSystemInformation();
                SystemInformation.MotherBoard = GetMotherBoard();
                SystemInformation.CdRom = GetCdRom();
                SystemInformation.Bios = GetBiosInfo();
                SystemInformation.RunningAsAdmin = IsRunningAsAdministrator();
                var service = new Task(Updater);
                service.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }


        // ReSharper disable once UnusedMethodReturnValue.Local
        private void SetNetworkInformation()
        {
            try
            {
                if (string.IsNullOrEmpty(NetworkInformation.PublicIp))
                {
                    NetworkInformation.PublicIp = NetworkService.GetPublicIp();
                    NetworkInformation.MacAddress = NetworkService.GetMacAddress().ToString();
                    NetworkInformation.InternalIp = NetworkService.GetIpAddress().ToString();
                    NetworkInformation.NetworkComputers = NetworkService.ConnectedDevices();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }


        public void SetOperatingSystemInformation()
        {
            try
            {
                if (string.IsNullOrEmpty(ServerOperatingSystem.Name))
                {
                    var wmi =
                        new ManagementObjectSearcher("select * from Win32_OperatingSystem")
                            .Get()
                            .Cast<ManagementObject>()
                            .First();

                    ServerOperatingSystem.Name = ((string) wmi["Caption"]).Trim();
                    ServerOperatingSystem.Version = (string) wmi["Version"];
                    ServerOperatingSystem.MaxProcessCount = (uint) wmi["MaxNumberOfProcesses"];
                    ServerOperatingSystem.MaxProcessRam = (ulong) wmi["MaxProcessMemorySize"];
                    ServerOperatingSystem.Architecture = (string) wmi["OSArchitecture"];
                    ServerOperatingSystem.SerialNumber = (string) wmi["SerialNumber"];
                    ServerOperatingSystem.Build = (string) wmi["BuildNumber"];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        public void SetCpuInformation()
        {
            try
            {
                if (string.IsNullOrEmpty(CpuInformation.Name))
                {
                    var cpu =
                        new ManagementObjectSearcher("select * from Win32_Processor")
                            .Get()
                            .Cast<ManagementObject>()
                            .First();

                    CpuInformation.Id = (string) cpu["ProcessorId"];
                    CpuInformation.Socket = (string) cpu["SocketDesignation"];
                    CpuInformation.Name = (string) cpu["Name"];
                    CpuInformation.Description = (string) cpu["Caption"];
                    CpuInformation.AddressWidth = (ushort?) cpu["AddressWidth"] ?? 0;
                    CpuInformation.DataWidth = (ushort?) cpu["DataWidth"] ?? 0;
                    CpuInformation.Architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                    CpuInformation.SpeedMHz = (uint?) cpu["MaxClockSpeed"] ?? 0;
                    CpuInformation.BusSpeedMHz = (uint?) cpu["ExtClock"] ?? 0;
                    CpuInformation.L2Cache = (uint?) cpu["L2CacheSize"]*(ulong) 1024 ?? 0;
                    CpuInformation.L3Cache = (uint?) cpu["L3CacheSize"]*(ulong) 1024 ?? 0;
                    CpuInformation.Cores = (uint?) cpu["NumberOfCores"] ?? 0;
                    CpuInformation.Threads = (uint?) cpu["NumberOfLogicalProcessors"] ?? 0;
                    CpuInformation.Name =
                        CpuInformation.Name
                            .Replace("(TM)", "™")
                            .Replace("(tm)", "™")
                            .Replace("(R)", "®")
                            .Replace("(r)", "®")
                            .Replace("(C)", "©")
                            .Replace("(c)", "©")
                            .Replace("    ", " ")
                            .Replace("  ", " ");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private async void Updater()
        {
            while (true)
            {
                try
                {
                    SystemInformation.AvailableMemory = AvailablePhysicalMemory;
                    SystemInformation.Drives = GetDriveInformation();
                    SystemInformation.UsedMemory = GetUsedMemory();
                    SystemInformation.TotalMemory = GetTotalPhysicalMemory();
                    SystemInformation.RunningProcesses = GetTotalProcesses();
                    SystemInformation.UpTime = GetUpTime().TotalMilliseconds;
                    SystemInformation.NetworkInfo = GetNetworkInfo();
                    SystemInformation.CpuUsage = GetPerformanceCounters();
                    SystemInformation.CpuTemps = GetCpuTemps();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine(ex.Message);
                }
                await Task.Delay(new TimeSpan(0, 0, 10));
            }
        }

       
        public string GetMotherBoard()
        {
            if (!string.IsNullOrEmpty(_motherBoard)) return _motherBoard;
            var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_BaseBoard");
            foreach (var wmi in searcher.Get())
            {
                try
                {
                    _motherBoard = wmi.GetPropertyValue("Product").ToString();
                    return _motherBoard;
                }

                catch
                {
                    _motherBoard = "Board Unknown";
                }
            }
            return _motherBoard;
        }


        private static object GetNetworkInfo()
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
            if (!string.IsNullOrEmpty(_cdRom)) return _cdRom;
            var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_CDROMDrive");

            foreach (var wmi in searcher.Get())
            {
                try
                {
                    _cdRom = wmi.GetPropertyValue("Drive").ToString();
                    return _cdRom;
                }

                catch
                {
                    _cdRom = "CD ROM Unknown";
                }
            }
            return _cdRom;
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
            if (!string.IsNullOrEmpty(_biosSerial)) return _biosSerial;
            var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_BIOS");

            foreach (var wmi in searcher.Get().Cast<ManagementObject>())
            {
                try
                {
                    _biosSerial = wmi.GetPropertyValue("SerialNumber").ToString();
                    return _biosSerial;
                }

                catch
                {
                    _biosSerial = "Unknown BIOS Serial";
                }
            }

            return _biosSerial;
        }

        private string GetBiosCaption()
        {
            if (!string.IsNullOrEmpty(_biosCaption)) return _biosCaption;
            var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_BIOS");

            foreach (var wmi in searcher.Get())
            {
                try
                {
                    _biosCaption = wmi.GetPropertyValue("Caption").ToString();
                    return _biosCaption;
                }
                catch
                {
                    _biosCaption = "Unknown BIOS Caption";
                }
            }
            return _biosCaption;
        }

        private string GetBiosManufacturer()
        {
            if (!string.IsNullOrEmpty(_biosManufacturer)) return _biosManufacturer;
            var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_BIOS");

            foreach (var wmi in searcher.Get())
            {
                try
                {
                    _biosManufacturer = wmi.GetPropertyValue("Manufacturer").ToString();
                    return _biosManufacturer;
                }

                catch
                {
                    _biosManufacturer = "BIOS Manufacturer Unknown";
                }
            }

            return _biosManufacturer;
        }


        private static ulong GetUsedMemory()
        {
            return GetTotalPhysicalMemory() - AvailablePhysicalMemory;
        }

        private static ulong GetTotalPhysicalMemory()
        {
            return new ComputerInfo().TotalPhysicalMemory;
        }

        private static int GetTotalProcesses()
        {
            return Process.GetProcesses().Length;
        }

        private static TimeSpan GetUpTime()
        {
            return TimeSpan.FromMilliseconds(GetTickCount64());
        }

        private static bool IsRunningAsAdministrator()
        {
            return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        }

        //Everyday We Stray Further From God's Light
        //If you enjoy your computer eating GB's of ram 
        //If you enjoy timeouts
        //When you give up trying to optimize this
        //Call me maybe.
        public static Dictionary<string, List<EventLogEntry>> GetEventLogs()
        {
            var dictionary = new Dictionary<string, List<EventLogEntry>>();
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
            return dictionary;
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
                try
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
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
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
                pc.NextValue();
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
            var temps = (from hardwareItem in myComputer.Hardware
                where hardwareItem.HardwareType == HardwareType.CPU
                from sensor in hardwareItem.Sensors
                where sensor.SensorType == SensorType.Temperature
                let value = sensor.Value
                where value != null
                where value != null
                select (float) value).ToList();
            if (temps.Count != 0) return temps;
            var tempTemps = new List<float>();
            var procCount = Environment.ProcessorCount;
            for (int i = 0; i < procCount; i++)
            {
                tempTemps.Add(-1);
            }
            return tempTemps;
        }


        [DllImport("kernel32")]
        private static extern ulong GetTickCount64();
    }
}