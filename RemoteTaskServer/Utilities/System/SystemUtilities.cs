using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic.Devices;
using RemoteTaskServer.Api.Models;

namespace RemoteTaskServer.Utilities.System
{
    internal class SystemUtilities
    {
        public void Start()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    SystemInformation.AvailableMemory = GetAvailablePhysicalMemory();
                    SystemInformation.UsedMemory = GetUsedMemory();
                    SystemInformation.TotalMemory = GetTotalPhysicalMemory();
                    SystemInformation.RunningProcesses = GetTotalProcesses();
                    SystemInformation.UpTime = GetUpTime().TotalMilliseconds;
                    SystemInformation.RunningAsAdmin = IsRunningAsAdministrator();
                    SystemInformation.CpuUsage = GetCurrentCpuUsage();

                }
            });
        }

        private ulong GetAvailablePhysicalMemory()
        {
            return new ComputerInfo().AvailablePhysicalMemory;
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
        private float GetCurrentCpuUsage()
        {
            var cpuCounter = new PerformanceCounter();
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";
            // will always start at 0
            dynamic firstValue = cpuCounter.NextValue();
            Thread.Sleep(1000);
            // now matches task manager reading
            dynamic secondValue = cpuCounter.NextValue();
            return secondValue;
        }

        [DllImport("kernel32")]
        private static extern ulong GetTickCount64();
    }
}