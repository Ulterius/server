#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using OpenHardwareMonitor.Hardware;
using UlteriusServer.Api.Network.Models;
using UlteriusServer.Api.Services.Network;
using UlteriusServer.Utilities.Drive;
using static UlteriusServer.Api.Win32.Display;

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
                    NetworkInformation.MacAddress = NetworkService.GetMacAddress();
                    NetworkInformation.InternalIp = NetworkService.GetDisplayAddress();
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
                if (string.IsNullOrEmpty(OperatingSystemInformation.Name))
                {
                    var wmi =
                        new ManagementObjectSearcher("select * from Win32_OperatingSystem")
                            .Get()
                            .Cast<ManagementObject>()
                            .First();

                    OperatingSystemInformation.Name = ((string) wmi["Caption"]).Trim();
                    OperatingSystemInformation.Version = (string) wmi["Version"];
                    OperatingSystemInformation.MaxProcessCount = (uint) wmi["MaxNumberOfProcesses"];
                    OperatingSystemInformation.MaxProcessRam = (ulong) wmi["MaxProcessMemorySize"];
                    OperatingSystemInformation.Architecture = (string) wmi["OSArchitecture"];
                    OperatingSystemInformation.SerialNumber = (string) wmi["SerialNumber"];
                    OperatingSystemInformation.Build = (string) wmi["BuildNumber"];
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
        static long ConvertKilobytesToBytes(long kilobytes)
        {
            return kilobytes * 1024;
        }

        private static long GetAvailablePhysicalMemory()
        {
            var winQuery = new ObjectQuery("SELECT * FROM CIM_OperatingSystem");
            var searcher = new ManagementObjectSearcher(winQuery);
            foreach (var o in searcher.Get())
            {
                var item = (ManagementObject) o;
                return ConvertKilobytesToBytes(long.Parse(item["FreePhysicalMemory"].ToString()));
            }
            return -1;
        }
      

        private async void Updater()
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
                    SystemInformation.NetworkInfo = GetNetworkInfo();
                    SystemInformation.CpuUsage = GetPerformanceCounters();
                    SystemInformation.CpuTemps = GetCpuTemps();
                    SystemInformation.Displays = DisplayInformation();
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


        private static long GetUsedMemory()
        {
            return GetTotalPhysicalMemory() - GetAvailablePhysicalMemory();
        }

        private static long GetTotalPhysicalMemory()
        {
            var winQuery = new ObjectQuery("SELECT * FROM CIM_OperatingSystem");
            var searcher = new ManagementObjectSearcher(winQuery);
            foreach (var o in searcher.Get())
            {
                var item = (ManagementObject)o;
                return ConvertKilobytesToBytes(long.Parse(item["TotalVisibleMemorySize"].ToString()));
            }
            return -1;
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
                    //just set it for now
                    driveInfo.SmartData = new List<SmartModel>();
                    driveInfo.Partitions = new List<PartitionModel>();
                    driveList.Add(driveInfo);
                    try
                    {
                        var mosDisks =
                            new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE Model = '" +
                                                         driveInfo.Model + "'").Get().GetEnumerator();
                        if (!mosDisks.MoveNext()) continue;
                        driveInfo.MediaType = mosDisks.Current.GetPropertyValue("MediaType")?.ToString() ?? "Unknown";
                        driveInfo.Serial = mosDisks.Current.GetPropertyValue("SerialNumber")?.ToString()?.Trim() ??
                                           "Unknown";
                        driveInfo.Interface = mosDisks.Current.GetPropertyValue("InterfaceType")?.ToString() ??
                                              "Unknown";
                        driveInfo.TotalPartitions = mosDisks.Current.GetPropertyValue("Partitions")?.ToString() ??
                                                    "Unknown";
                        driveInfo.Signature = mosDisks.Current.GetPropertyValue("Signature")?.ToString() ?? "Unknown";
                        driveInfo.Firmware = mosDisks.Current.GetPropertyValue("FirmwareRevision")?.ToString() ??
                                             "Unknown";
                        driveInfo.Cylinders = mosDisks.Current.GetPropertyValue("TotalCylinders")?.ToString() ??
                                              "Unknown";
                        driveInfo.Sectors = mosDisks.Current.GetPropertyValue("TotalSectors")?.ToString() ?? "Unknown";
                        driveInfo.Heads = mosDisks.Current.GetPropertyValue("TotalHeads")?.ToString() ?? "Unknown";
                        driveInfo.Tracks = mosDisks.Current.GetPropertyValue("TotalTracks")?.ToString() ?? "Unknown";
                        driveInfo.BytesPerSecond = mosDisks.Current.GetPropertyValue("BytesPerSector")?.ToString() ??
                                                   "Unknown";
                        driveInfo.SectorsPerTrack = mosDisks.Current.GetPropertyValue("SectorsPerTrack")?.ToString() ??
                                                    "Unknown";
                        driveInfo.TracksPerCylinder =
                            mosDisks.Current.GetPropertyValue("TracksPerCylinder")?.ToString() ?? "Unknown";
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                    }

                    try
                    {
                        var mosPartition =
                            new ManagementObjectSearcher("SELECT * FROM Win32_DiskPartition WHERE DiskIndex = '" +
                                                         index + "'").Get().GetEnumerator();
                        while (mosPartition.MoveNext())
                        {
                            var partion = new PartitionModel
                            {
                                Name = mosPartition.Current.GetPropertyValue("Name")?.ToString() ?? "Unknown",
                                Size = mosPartition.Current.GetPropertyValue("Size")?.ToString() ?? "Unknown",
                                BlockSize = mosPartition.Current.GetPropertyValue("BlockSize")?.ToString() ?? "Unknown",
                                StartingOffset =
                                    mosPartition.Current.GetPropertyValue("StartingOffset")?.ToString() ?? "Unknown",
                                Index = mosPartition.Current.GetPropertyValue("Index")?.ToString() ?? "Unknown",
                                DiskIndex = mosPartition.Current.GetPropertyValue("DiskIndex")?.ToString() ?? "Unknown",
                                BootPartition =
                                    mosPartition.Current.GetPropertyValue("BootPartition")?.ToString() ?? "Unknown",
                                PrimaryPartition =
                                    mosPartition.Current.GetPropertyValue("PrimaryPartition")?.ToString() ?? "Unknown",
                                Bootable = mosPartition.Current.GetPropertyValue("Bootable")?.ToString() ?? "Unknown"
                            };
                            driveInfo.Partitions.Add(partion);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }

            using (
                var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM MSStorageDriver_ATAPISmartData")
                )
            using (
                var thresSearcher = new ManagementObjectSearcher("root\\WMI",
                    "SELECT * FROM MSStorageDriver_FailurePredictThresholds"))
            using (
                var failureSearch = new ManagementObjectSearcher("root\\WMI",
                    "SELECT * FROM MSStorageDriver_FailurePredictStatus"))
            {
                try
                {
                    var searcherEnumerator = searcher.Get().GetEnumerator();
                    var thresSearcherEnumerator = thresSearcher.Get().GetEnumerator();
                    var failureSearchEnumerator = failureSearch.Get().GetEnumerator();

                    var index = 0;
                    while (searcherEnumerator.MoveNext() && thresSearcherEnumerator.MoveNext())
                    {
                        var arrVendorSpecific = (byte[]) searcherEnumerator.Current.GetPropertyValue("VendorSpecific");
                        var arrThreshold = (byte[]) thresSearcherEnumerator.Current.GetPropertyValue("VendorSpecific");

                        /* Create SMART data from 'vendor specific' array */
                        var d = new SmartData(arrVendorSpecific, arrThreshold);
                        var smartRows = (from b in d.Attributes
                            where !Regex.IsMatch(b.AttributeType.ToString(), @"^\d+$")
                            let rawData =
                                BitConverter.ToString(b.VendorData.Reverse().ToArray()).Replace("-", string.Empty)
                            select
                                new SmartModel(b.AttributeType.ToString(),
                                    b.Value.ToString(CultureInfo.InvariantCulture),
                                    b.Threshold.ToString(CultureInfo.InvariantCulture), rawData,
                                    long.Parse(rawData, NumberStyles.HexNumber).ToString(CultureInfo.InvariantCulture)))
                            .ToList();
                        driveList.ElementAt(index).SmartData = smartRows;
                        if (failureSearchEnumerator.MoveNext())
                        {
                            driveList.ElementAt(index).DriveHealth =
                                (bool) failureSearchEnumerator.Current.GetPropertyValue("PredictFailure")
                                    ? "WARNING"
                                    : "OK";
                        }
                        index++;
                    }
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
            for (var i = 0; i < procCount; i++)
            {
                tempTemps.Add(-1);
            }
            return tempTemps;
        }


        [DllImport("kernel32")]
        private static extern ulong GetTickCount64();
    }
}