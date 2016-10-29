#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;
using Newtonsoft.Json;
using UlteriusServer.Api.Network.Models;
using UlteriusServer.Properties;
using SystemInformation = UlteriusServer.Api.Network.Models.SystemInformation;

#endregion

namespace UlteriusServer.Utilities.Usage
{
    public class HardwareSurvey
    {
        private static readonly string results = "surveryresults.json";

        private List<GpuInformation> GetGpuInformation()
        {
            var searcher =
                new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");

            var gpus = (from ManagementBaseObject mo in searcher.Get()
                select new GpuInformation
                {
                    Name = mo["Name"]?.ToString(),
                    ScreenInfo = mo["VideoModeDescription"]?.ToString(),
                    DriverVersion = mo["DriverVersion"]?.ToString(),
                    RefreshRate = int.Parse(mo["CurrentRefreshRate"]?.ToString() ?? "0"),
                    AdapterRam = mo["AdapterRAM"]?.ToString(),
                    VideoArchitecture = int.Parse(mo["VideoArchitecture"]?.ToString() ?? "0"),
                    VideoMemoryType = int.Parse(mo["VideoMemoryType"]?.ToString() ?? "0"),
                    InstalledDisplayDrivers = mo["InstalledDisplayDrivers"]?.ToString()?.Split(','),
                    AdapterCompatibility = mo["AdapterCompatibility"]?.ToString(),
                    Status = mo["Status"]?.ToString(),
                    Availability = int.Parse(mo["Availability"]?.ToString() ?? "0")
                }).ToList();
            return gpus;
        }

        private void Prompt()
        {
            var dialogResult = MessageBox.Show(Resources.HardwareSurvey_Setup_,
                Resources.HardwareSurvey_Setup_Usage_statistics_reporting, MessageBoxButtons.YesNo);
            switch (dialogResult)
            {
                case DialogResult.Yes:
                    SendData();
                    break;
                case DialogResult.No:
                    File.WriteAllText(results, "false");
                    break;
            }
        }

        public void Setup(bool service = false)
        {
            if (File.Exists(results))
            {
                var days = (DateTime.Now - File.GetCreationTime(results)).TotalDays;
                if (days > 30)
                {
                    SendData();
                }
            }
            else
            {
                if (service)
                {
                    SendData();
                }
                else
                {
                    Prompt();
                }
            }
        }

        public string GetMachineGuid()
        {
            try
            {
                var location = @"SOFTWARE\Microsoft\Cryptography";
                var name = "MachineGuid";
                using (var localMachineX64View =
                    RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                {
                    using (var rk = localMachineX64View.OpenSubKey(location))
                    {
                        if (rk == null)
                            throw new KeyNotFoundException(
                                $"Key Not Found: {location}");

                        var machineGuid = rk.GetValue(name);
                        if (machineGuid == null)
                            throw new IndexOutOfRangeException(
                                $"Index Not Found: {name}");
                        return machineGuid.ToString().Replace("-", "").ToUpper();
                    }
                }
            }
            catch (Exception)
            {
                return Guid.NewGuid().ToString("N").ToUpper();
            }
        }

        private async void SendData()
        {
            try
            {
                var data = new
                {
                    ServerInfo = ServerOperatingSystem.ToObject(),
                    SystemInfo = SystemInformation.ToObject(),
                    UlteriusVersion = Assembly.GetExecutingAssembly().GetName().Version,
                    GpuInfo = GetGpuInformation(),
                    NetworkInformation = NetworkInformation.ToObject(),
                    CpuInfo = CpuInformation.ToObject()
                };
                var json = JsonConvert.SerializeObject(data);
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("guid", GetMachineGuid()),
                    new KeyValuePair<string, string>("results", json)
                });
                using (var client = new HttpClient())
                {
                    client.Timeout = new TimeSpan(0, 0, 0, 5);
                    var result = await client.PostAsync("https://api.ulterius.io/hardware/", content);
                    if (result.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Hardware Survery Completed");
                        File.WriteAllText(results, "true");
                    }
                }
            }
            catch (Exception)
            {
                //just fail
            }
        }
    }
}