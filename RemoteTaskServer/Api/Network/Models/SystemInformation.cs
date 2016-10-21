#region

using System.Collections.Generic;
using OpenHardwareMonitor.Hardware;

#endregion

namespace UlteriusServer.Api.Network.Models

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
        public static string MotherBoard { get; set; }
        public static string CdRom { get; set; }
        public static object Bios { get; set; }
        public static object NetworkInfo { get; set; }
        public static List<float> CpuTemps { get; set; }
        public static List<FanInformation> FanSpeeds { get; set; }

        public static object ToObject()
        {
            var data = new
            {
                cpuUsage = CpuUsage,
                cpuTemps = CpuTemps,
                totalMemory = TotalMemory,
                availableMemory = AvailableMemory,
                usedMemory = UsedMemory,
                runningProcesses = RunningProcesses,
                upTime = UpTime,
                runningAsAdmin = RunningAsAdmin,
                drives = Drives,
                cdRom = CdRom,
                networkInfo = NetworkInfo,
                motherBoard = MotherBoard,
                biosInfo = Bios,
                fanSpeeds = FanSpeeds
            };
            return data;
        }
    }
}