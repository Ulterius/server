#region

using System;
using System.Linq;
using System.Management;
using UlteriusServer.TaskServer.Network.Messages;
using UlteriusServer.TaskServer.Network.Models;
using UlteriusServer.TaskServer.Services.System;
using UlteriusServer.WebSocketAPI.Authentication;

#endregion

namespace UlteriusServer.TaskServer.Network.PacketHandlers
{
    public class OperatingSystemPacketHandler : PacketHandler
    {
        private PacketBuilder _builder;
        private AuthClient _client;
        private Packet _packet;


        public void GetEventLogs()
        {
            _builder.WriteMessage(SystemUtilities.GetEventLogs());
            GC.Collect();
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
                ServerOperatingSystem.MaxProcessRam = (ulong) wmi["MaxProcessMemorySize"];
                ServerOperatingSystem.Architecture = (string) wmi["OSArchitecture"];
                ServerOperatingSystem.SerialNumber = (string) wmi["SerialNumber"];
                ServerOperatingSystem.Build = (string) wmi["BuildNumber"];
            }
            _builder.WriteMessage(ServerOperatingSystem.ToObject());
        }

        public override void HandlePacket(Packet packet)
        {
            _client = packet.AuthClient;
            _packet = packet;
            _builder = new PacketBuilder(_client, _packet.EndPoint, _packet.SyncKey);
            switch (_packet.PacketType)
            {
                case PacketManager.PacketTypes.RequestOsInformation:
                    GetOperatingSystemInformation();
                    break;
                case PacketManager.PacketTypes.GetEventLogs:
                    // GetEventLogs();
                    break;
            }
        }
    }
}