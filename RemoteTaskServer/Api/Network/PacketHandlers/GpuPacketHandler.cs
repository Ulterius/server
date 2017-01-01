#region

using System;
using System.Linq;
using System.Management;
using System.Runtime.ExceptionServices;
using OpenHardwareMonitor.Hardware;
using UlteriusServer.Api.Network.Messages;
using UlteriusServer.Api.Network.Models;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;
using static UlteriusServer.Api.Network.PacketManager;

#endregion

namespace UlteriusServer.Api.Network.PacketHandlers
{
    internal class GpuPacketHandler : PacketHandler
    {
        private MessageBuilder _builder;
        private AuthClient _authClient;
        private Packet _packet;
        private WebSocket _client;


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
            _builder.WriteMessage(new
            {
                gpus
            });
        }

        //random crash happening in this DLL, will fork and fix.
        [HandleProcessCorruptedStateExceptions]
        private float? GetGpuTemp(string gpuName)
        {

            return UlteriusApiServer.RunningAsService ? GetServiceTemp(gpuName) : GetLocalTemp(gpuName);
        
        }
        [HandleProcessCorruptedStateExceptions]
        private float? GetLocalTemp(string gpuName)
        {
            try
            {
                var myComputer = new Computer();
                myComputer.Open();
                //possible fix for gpu temps on laptops
                myComputer.GPUEnabled = true;
                float temp = -1;
                foreach (var hardwareItem in myComputer.Hardware)
                {
                    hardwareItem.Update();
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
                                if (sensor.Value != null)
                                {
                                    temp = (float)sensor.Value;
                                }
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
                                if (sensor.Value != null)
                                {
                                    temp = (float)sensor.Value;
                                }
                            }
                            break;
                    }
                }
                myComputer.Close();
                return temp;
            }
            catch (System.AccessViolationException)
            {
                return -1;
            }
        }

        private float? GetServiceTemp(string gpuName)
        {
            return UlteriusApiServer.AgentClient.GetGpuTemp(gpuName);
        }

        public override void HandlePacket(Packet packet)
        {
            _client = packet.Client;
            _authClient = packet.AuthClient;
            _packet = packet;
            _builder = new MessageBuilder(_authClient, _client, _packet.EndPointName, _packet.SyncKey);
            switch (_packet.EndPoint)
            {
                case EndPoints.RequestGpuInformation:
                    GetGpuInformation();
                    break;
            }
        }
    }
}