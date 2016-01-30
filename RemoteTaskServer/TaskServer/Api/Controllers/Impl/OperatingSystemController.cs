#region

using System.Linq;
using System.Management;
using UlteriusServer.TaskServer.Api.Models;
using UlteriusServer.TaskServer.Api.Serialization;
using UlteriusServer.TaskServer.Services.System;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers.Impl
{
    public class OperatingSystemController : ApiController
    {
        private readonly WebSocket client;
        private readonly Packets packet;
        private readonly ApiSerializator serializator = new ApiSerializator();

        public OperatingSystemController(WebSocket client, Packets packet)
        {
            this.client = client;
            this.packet = packet;
        }

        public void GetEventLogs()
        {
            serializator.Serialize(client, packet.endpoint, packet.syncKey, SystemUtilities.GetEventLogs());
            System.GC.Collect();
        }

        public void GetOperatingSystemInformation()
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
            serializator.Serialize(client, packet.endpoint, packet.syncKey, ServerOperatingSystem.ToObject());
        }
    }
}