#region

using System;
using System.Linq;
using System.Management;
using UlteriusServer.TaskServer.Network.Messages;
using UlteriusServer.TaskServer.Network.Models;
using UlteriusServer.WebSocketAPI.Authentication;

#endregion

namespace UlteriusServer.TaskServer.Network.PacketHandlers
{
    public class CpuPacketHandler : PacketHandler
    {
        private PacketBuilder _builder;
        private AuthClient _client;
        private Packet _packet;


        public void GetCpuInformation()
        {
            if (string.IsNullOrEmpty(CpuInformation.Name))
            {
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
            _builder.WriteMessage(CpuInformation.ToObject());
        }

        public override void HandlePacket(Packet packet)
        {
            _client = packet.AuthClient;
            _packet = packet;
            _builder = new PacketBuilder(_client, _packet.EndPoint, _packet.SyncKey);
            switch (_packet.PacketType)
            {
                case PacketManager.PacketTypes.RequestCpuInformation:
                    GetCpuInformation();
                    break;
            }
        }
    }
}