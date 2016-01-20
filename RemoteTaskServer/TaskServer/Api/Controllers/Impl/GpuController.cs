#region

using System.Linq;
using System.Management;
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
                where mo["CurrentBitsPerPixel"] != null
                select new GpuInformation
                {
                    Name = mo["Name"]?.ToString(),
                    ScreenInfo = mo["VideoModeDescription"]?.ToString(),
                    DriverVersion = mo["DriverVersion"]?.ToString(),
                    RefreshRate = int.Parse(mo["CurrentRefreshRate"]?.ToString()),
                    AdapterRam = mo["AdapterRAM"]?.ToString(),
                    VideoArchitecture = int.Parse(mo["VideoArchitecture"]?.ToString()),
                    VideoMemoryType = int.Parse(mo["VideoMemoryType"]?.ToString()),
                    InstalledDisplayDrivers = mo["InstalledDisplayDrivers"]?.ToString()?.Split(','),
                    AdapterCompatibility = mo["AdapterCompatibility"]?.ToString()
                }).ToList();
            var data = new
            {
                gpus
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
        }
    }
}