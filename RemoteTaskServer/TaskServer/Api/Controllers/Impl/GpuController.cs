#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using OpenHardwareMonitor.Hardware;
using UlteriusServer.TaskServer.Api.Models;
using UlteriusServer.TaskServer.Api.Serialization;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers.Impl
{
    internal class GpuController : ApiController
    {
        private readonly WebSocket client;
        private readonly Packets packet;
        private readonly ApiSerializator serializator = new ApiSerializator();

        public GpuController(WebSocket client, Packets packet)
        {
            this.client = client;
            this.packet = packet;
        }

        public void GetGpuInformation()
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
                    Availability = int.Parse(mo["Availability"]?.ToString() ?? "0"),
                    Temperature = GetGpuTemp(mo["Name"]?.ToString())
                }).ToList();
            serializator.Serialize(client, packet.endpoint, packet.syncKey, new
            {
                gpus
            });
        }

        private float? GetGpuTemp(string gpuName)
        {
            var myComputer = new Computer();

            myComputer.Open();
            myComputer.GPUEnabled = true;
            var gpuTemps = new List<Object>();
            foreach (var hardwareItem in myComputer.Hardware)
            {
                switch (hardwareItem.HardwareType)
                {
                    case HardwareType.GpuNvidia:
                        foreach (
                            var sensor in
                                hardwareItem.Sensors.Where(
                                    sensor =>
                                        sensor.SensorType == SensorType.Temperature &&
                                        hardwareItem.Name.Contains(gpuName)))
                        {
                            return sensor.Value;
                        }
                        break;
                    case HardwareType.GpuAti:
                        foreach (
                            var sensor in
                                hardwareItem.Sensors.Where(
                                    sensor =>
                                        sensor.SensorType == SensorType.Temperature &&
                                        hardwareItem.Name.Contains(gpuName)))
                        {
                            return sensor.Value;
                        }
                        break;
                }
            }
            return -1;
        }
    }
}