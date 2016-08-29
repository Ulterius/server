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
using UlteriusServer.Utilities.Drive;
using Computer = OpenHardwareMonitor.Hardware.Computer;

#endregion

namespace UlteriusServer.Api.Services.System
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

        private void SetNetworkInformation()
        {
            if (string.IsNullOrEmpty(NetworkInformation.PublicIp))
            {
                NetworkInformation.PublicIp = NetworkService.GetPublicIp();
                NetworkInformation.NetworkComputers = NetworkService.ConnectedDevices();
                NetworkInformation.MacAddress = NetworkService.GetMacAddress().ToString();
                NetworkInformation.InternalIp = NetworkService.GetIpAddress().ToString();
            }
        }


        public void SetOperatingSystemInformation()
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

        public void SetCpuInformation()
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
                CpuInformation.AddressWidth = (ushort) cpu["AddressWidth"];
                CpuInformation.DataWidth = (ushort) cpu["DataWidth"];
                CpuInformation.Architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                CpuInformation.SpeedMHz = (uint) cpu["MaxClockSpeed"];
                CpuInformation.BusSpeedMHz = (uint) cpu["ExtClock"];
                CpuInformation.L2Cache = (uint) cpu["L2CacheSize"]*(ulong) 1024;
                CpuInformation.L3Cache = (uint) cpu["L3CacheSize"]*(ulong) 1024;
                CpuInformation.Cores = (uint) cpu["NumberOfCores"];
                CpuInformation.Threads = (uint) cpu["NumberOfLogicalProcessors"];
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
            var wdSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");

            // extract model and interface information
            var iDriveIndex = 0;
            var driveList = new List<DriveInformation>();
            var di = DriveInfo.GetDrives();

            foreach (var o in wdSearcher.Get())
            {
                var driveInfo = new DriveInformation();
                var drive = (ManagementObject) o;
                var type = drive["InterfaceType"].ToString().Trim();
                var model = drive["Model"].ToString().Trim();
                if (type.Equals("IDE"))
                {
                    var driveData = di[iDriveIndex];
                    driveInfo.Model = model;
                    driveInfo.Name = driveData.Name;
                    driveInfo.FreeSpace = driveData.TotalFreeSpace;
                    driveInfo.TotalSize = driveData.TotalSize;
                    driveInfo.DriveType = driveData.DriveType.ToString();
                    driveInfo.DriveFormat = driveData.DriveFormat;
                    driveInfo.VolumeLabel = driveData.VolumeLabel;
                    driveInfo.RootDirectory = driveData.RootDirectory.ToString();
                    driveInfo.IsReady = driveData.IsReady;
                    driveInfo.SmartInfo = new Disk
                    {
                        Model = driveInfo.Model,
                        Type = type
                    };
                    driveList.Add(driveInfo);
                    iDriveIndex++;
                }
            }
            var pmsearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMedia");
            // retrieve hdd serial number
            iDriveIndex = 0;
            foreach (var o in pmsearcher.Get())
            {
                var drive = (ManagementObject) o;
                // because all physical media will be returned we need to exit
                // after the hard drives serial info is extracted
                if (iDriveIndex >= driveList.Count)
                    break;
                driveList[iDriveIndex].SmartInfo.Serial = drive["SerialNumber"]?.ToString().Trim() ?? "None";
                iDriveIndex++;
            }
            var searcher = new ManagementObjectSearcher("Select * from Win32_DiskDrive")
            {
                Scope = new ManagementScope(@"\root\wmi"),
                Query = new ObjectQuery("Select * from MSStorageDriver_FailurePredictStatus")
            };
            // check if SMART reports the drive is failing
            iDriveIndex = 0;
            foreach (var o in searcher.Get())
            {
                var drive = (ManagementObject) o;
                driveList[iDriveIndex].SmartInfo.IsOk = (bool) drive.Properties["PredictFailure"].Value == false;
                iDriveIndex++;
            }
            searcher.Query = new ObjectQuery("Select * from MSStorageDriver_FailurePredictData");
            iDriveIndex = 0;
            foreach (var o in searcher.Get())
            {
                var data = (ManagementObject) o;
                var bytes = (byte[]) data.Properties["VendorSpecific"].Value;
                for (var i = 0; i < 30; ++i)
                {
                    try
                    {
                        int id = bytes[i*12 + 2];

                        int flags = bytes[i*12 + 4];
                        var failureImminent = (flags & 0x1) == 0x1;
                        int value = bytes[i*12 + 5];
                        int worst = bytes[i*12 + 6];
                        var vendordata = BitConverter.ToInt32(bytes, i*12 + 7);
                        if (id == 0) continue;

                        var attr = driveList[iDriveIndex].SmartInfo.Attributes[id];
                        attr.Current = value;
                        attr.Worst = worst;
                        attr.Data = vendordata;
                        attr.IsOk = failureImminent == false;
                    }
                    catch
                    {
                        // ignored
                    }
                }
                iDriveIndex++;
            }
            searcher.Query = new ObjectQuery("Select * from MSStorageDriver_FailurePredictThresholds");
            iDriveIndex = 0;
            foreach (var o in searcher.Get())
            {
                var data = (ManagementObject) o;
                var bytes = (byte[]) data.Properties["VendorSpecific"].Value;
                for (var i = 0; i < 30; ++i)
                {
                    try
                    {
                        int id = bytes[i*12 + 2];
                        int thresh = bytes[i*12 + 3];
                        if (id == 0) continue;

                        var attr = driveList[iDriveIndex].SmartInfo.Attributes[id];
                        attr.Threshold = thresh;
                    }
                    catch
                    {
                        // ignored
                    }
                }

                iDriveIndex++;
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