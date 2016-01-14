#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.Sockets;
using System.Reflection;
using System.Web.Script.Serialization;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Network;
using UlteriusServer.Utilities.System;
using UlteriusServer.Windows.Api.Models;
using static System.String;

#endregion

namespace UlteriusServer.Windows.Api
{
    internal class TaskApi
    {
        private static ManagementObjectSearcher searcher;
        public string format = "JSON";

        private static bool KillProcessById(int id, bool waitForExit = false)
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

        public static object KillProcessById(int id)
        {
            var isKilled = KillProcessById(id, true);
            var processName = Process.GetProcessById(id).ProcessName;
            var data = new
            {
                processKilled = isKilled,
                processId = id, processName
            };
            return data;
        }

      
        private static int StartProcess(string processName,  bool waitForExit = false)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo(processName);

                var process = new Process {StartInfo = processStartInfo};
                if (!process.Start())
                {
                    return -1;
                }
                return process.Id;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public static object StartProcess(string processName)
        {
            var id = StartProcess(processName, true);
            var isStarted = id >= 0;
            
            var data = new
            {
                processStarted = isStarted,
                processId = id,
                processName
            };
            return data;
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
            if (IsNullOrEmpty(NetworkInformation.PublicIp))
            {
                NetworkInformation.PublicIp = NetworkUtilities.GetPublicIp();
                NetworkInformation.NetworkComputers = NetworkUtilities.ConnectedDevices();
                NetworkInformation.MacAddress = NetworkUtilities.GetMacAddress().ToString();
                NetworkInformation.InternalIp = NetworkUtilities.GetIPAddress().ToString();
            }
            return new JavaScriptSerializer().Serialize(new
            {
                endpoint = "requestNetworkInformation",
                results = NetworkInformation.ToObject()
            });
        }

        public static string GetEventLogs()
        {
            return SystemUtilities.GetEventLogs();
        }

        public static string GetCpuInformation()
        {
            if (IsNullOrEmpty(CpuInformation.Name))
            {
                var opt = new EnumerationOptions();
                opt.BlockSize = 2000;
                opt.EnumerateDeep = true;
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
            return new JavaScriptSerializer().Serialize(new
            {
                endpoint = "requestCpuInformation",
                results = CpuInformation.ToObject()
            });
        }

        public static string GetOperatingSystemInformation()
        {
            if (IsNullOrEmpty(ServerOperatingSystem.Name))
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
            return new JavaScriptSerializer().Serialize(new
            {
                endpoint = "requestOsInformation",
                results = ServerOperatingSystem.ToObject()
            });
        }

        public static string GetSystemInformation()
        {
            return new JavaScriptSerializer().Serialize(new
            {
                endpoint = "requestSystemInformation",
                results = SystemInformation.ToObject()
            });
        }

        /// <summary>
        ///     Builds all of the system information and sends it off as JSON
        ///     This function is literally a cluster fuck of retardation
        /// </summary>
        /// <returns></returns>
        public static string GetProcessInformation()
        {
            var results = new List<SystemProcesses>();
            var simpleProcesses = new List<SimpleProcessInfo>();

            try
            {
                using (
                    searcher =
                        new ManagementObjectSearcher("root\\CIMV2",
                            "SELECT ExecutablePath, ProcessId FROM Win32_Process"))
                {
                    simpleProcesses.AddRange(from ManagementBaseObject info in searcher.Get()
                        let id = int.Parse(info["ProcessId"].ToString())
                        let fullPath = (string) info["ExecutablePath"]
                        select new SimpleProcessInfo
                        {
                            path = fullPath,
                            id = id
                        });
                }

                var options = new EnumerationOptions {ReturnImmediately = false};
                using (
                    searcher =
                        new ManagementObjectSearcher("root\\CIMV2",
                            "SELECT * FROM Win32_PerfFormattedData_PerfProc_Process", options))
                {
                    results.AddRange(from ManagementBaseObject queryObj in searcher.Get()
                        where queryObj != null
                        let name = (string) queryObj["Name"]
                        let processId = int.Parse(queryObj["IDProcess"].ToString())
                        let handles = int.Parse(queryObj["HandleCount"].ToString())
                        let threads = int.Parse(queryObj["ThreadCount"].ToString())
                        let memory = long.Parse(queryObj["WorkingSetPrivate"].ToString())
                        let cpuUsage = int.Parse(queryObj["PercentProcessorTime"].ToString())
                        let ioReadOperationsPerSec = int.Parse(queryObj["IOReadOperationsPerSec"].ToString())
                        let ioWriteOperationsPerSec = int.Parse(queryObj["IOWriteOperationsPerSec"].ToString())
                        let fullPath = ""
                        let icon = ""
                        select new SystemProcesses
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
                    foreach (var result in results)
                    {
                        foreach (var process in simpleProcesses.Where(process => process.id == result.id))
                        {
                            result.path = process.path;
                            if (!IsNullOrEmpty(result.path))
                            {
                                result.icon = Tools.GetIconForProcess(result.path);
                            }
                            else
                            {
                                result.path = "null";
                                result.icon = "null";
                            }
                        }
                    }
                }
            }
            catch (ManagementException)
            {
                Console.WriteLine("Selection failed");
            }

            return new JavaScriptSerializer().Serialize(new
            {
                endpoint = "requestProcessInformation",
                results
            });
        }

        public class SimpleProcessInfo
        {
            public int id;
            public string path;
        }
    }
}