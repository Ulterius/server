#region

using System.Collections.Generic;
using System.Web.Script.Serialization;

#endregion

namespace UlteriusServer.Windows.Api.Models

{
    public static class SystemInformation
    {
        public static List<float> CpuUsage { get; set; }
        public static ulong TotalMemory { get; set; }
        public static ulong AvailableMemory { get; set; }
        public static ulong UsedMemory { get; set; }
        public static int RunningProcesses { get; set; }
        public static double UpTime { get; set; }
        public static bool RunningAsAdmin { get; set; }
        public static List<DriveInformation> Drives { get; set; }

        public static object ToObject()
        {
            var data = new
            {
                cpuUsage = CpuUsage,
                totalMemory = TotalMemory,
                availableMemory = AvailableMemory,
                usedMemory = UsedMemory,
                runningProcesses = RunningProcesses,
                upTime = UpTime,
                runningAsAdmin = RunningAsAdmin,
                drives = Drives
            };
            return data;
        }
   
    }
}