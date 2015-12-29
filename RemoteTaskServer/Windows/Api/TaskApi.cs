#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Web.Script.Serialization;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Network;
using UlteriusServer.Utilities.System;
using UlteriusServer.Windows.Api.Models;

#endregion

namespace UlteriusServer.Windows.Api
{
    internal class TaskApi
    {
        public string format = "JSON";

        public static bool KillProcessById(int id, bool waitForExit = false)
        {
            using (var p = Process.GetProcessById(id))
            {
                if (p == null || p.HasExited) return false;

                p.Kill();
                if (waitForExit)
                {
                    p.WaitForExit();
                }
                return true;
            }
        }


        public static bool StartProcess(string processName)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo(processName);

                var process = new Process {StartInfo = processStartInfo};
                if (!process.Start())
                {
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void RestartServer()
        {
            var info = Console.ReadKey();
            var fileName = Assembly.GetExecutingAssembly().Location;
            Process.Start(fileName);
            Environment.Exit(0);
        }

        public static string GetNetworkInformation()


        {
            if (string.IsNullOrEmpty(NetworkInformation.PublicIp))
            {
                NetworkInformation.PublicIp = NetworkUtilities.GetPublicIp();
                NetworkInformation.NetworkComputers = NetworkUtilities.ConnectedDevices();
                NetworkInformation.MacAddress = NetworkUtilities.GetMacAddress().ToString();
                NetworkInformation.InternalIp = NetworkUtilities.GetIPAddress().ToString();
            }
            return NetworkInformation.ToJson();
        }

        public static string GetEventLogs()
        {
            return SystemUtilities.GetEventLogs();
        }

        public static string GetCpuInformation()
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

            return CpuInformation.ToJson();
        }

        public static string GetOperatingSystemInformation()
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
                ServerOperatingSystem.MaxProcessRAM = (ulong) wmi["MaxProcessMemorySize"];
                ServerOperatingSystem.Architecture = (string) wmi["OSArchitecture"];
                ServerOperatingSystem.SerialNumber = (string) wmi["SerialNumber"];
                ServerOperatingSystem.Build = (string) wmi["BuildNumber"];
            }
            return ServerOperatingSystem.ToJson();
        }

        public static string GetSystemInformation()
        {
            return SystemInformation.ToJson();
        }

        /// <summary>
        ///     Builds all of the system information and sends it off as JSON
        /// </summary>
        /// <returns></returns>
        public static string GetProcessInformation()
        {
            var results = new List<SystemProcesses>();

            try
            {
                Console.WriteLine("Select Now");
                var searcher =
                    new ManagementObjectSearcher("root\\CIMV2",
                        "SELECT * FROM Win32_PerfFormattedData_PerfProc_Process");


                foreach (var queryObj in searcher.Get())
                {
                    
                    //process can be overwritten after select
                    if (queryObj == null) continue;
                    var name = (string) queryObj["Name"];
                    var processId = int.Parse(queryObj["IDProcess"].ToString());
                    var handles = int.Parse(queryObj["HandleCount"].ToString());
                    var threads = int.Parse(queryObj["ThreadCount"].ToString());
                    var memory = long.Parse(queryObj["WorkingSetPrivate"].ToString());
                    var cpuUsage = int.Parse(queryObj["PercentProcessorTime"].ToString());
                    var ioReadOperationsPerSec = int.Parse(queryObj["IOReadOperationsPerSec"].ToString());
                    var ioWriteOperationsPerSec = int.Parse(queryObj["IOWriteOperationsPerSec"].ToString());
                    // var ioReadBytesPerSec = int.Parse(queryObj["IOReadBytesPerSec "].ToString());
                    // var ioWriteBytesPerSec = int.Parse(queryObj["IOWriteBytesPerSec  "].ToString());
                    var fullPath = "";
                    var icon = "";
                    var process = Process.GetProcessById(processId);
                    try
                    {
                        fullPath = process.Modules[0].FileName;
                        icon = Tools.GetIconForProcess(fullPath);
                    }
                    catch (Win32Exception)
                    {
                        fullPath = "null";
                        icon = "null";
                    }

                    results.Add(new SystemProcesses
                    {
                        id = processId,
                        path = fullPath,
                        name = name,
                        icon = icon,
                        ramUsage = memory,
                        cpuUsage = cpuUsage,
                        threads = threads,
                        handles = handles,
                        ioWriteOperationsPerSec = ioWriteOperationsPerSec,
                        ioReadOperationsPerSec = ioReadOperationsPerSec
                    });
                }
            }
            catch (ManagementException e)
            {
               Console.WriteLine("13 giga wat");
            }
            var json = new JavaScriptSerializer().Serialize(results);
            return json;
        }
    }
}