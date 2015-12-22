using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.Devices;
using Newtonsoft.Json;
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
                      SystemInformation.CpuUsage = GetPerformanceCounters();
                    //GetPerformanceCounters();

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


        public static string GetEventLogs()
        {
            var dictionary = new Dictionary<string, List<EventLogEntry>>();
            var d = EventLog.GetEventLogs();
            foreach (EventLog l in d)
            {
                var categoryName = l.LogDisplayName;
                if (!dictionary.ContainsKey(categoryName))
                    dictionary.Add(categoryName, new List<EventLogEntry>());

                foreach (EventLogEntry entry in l.Entries)
                {
                    dictionary[categoryName].Add(entry);
                }
            }

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = Int32.MaxValue;
            var json =
                    serializer.Serialize(dictionary);
            return json;
        }  
        public static List<float> GetPerformanceCounters()
        {
            List<float> performanceCounters = new List<float>();
            int procCount = Environment.ProcessorCount;
            for (int i = 0; i < procCount; i++)
            {
                PerformanceCounter pc = new PerformanceCounter("Processor", "% Processor Time", i.ToString());
                dynamic firstValue = pc.NextValue();
                Thread.Sleep(1000);
                // now matches task manager reading
                dynamic secondValue = pc.NextValue();
                performanceCounters.Add(secondValue);
            }
            return performanceCounters;
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