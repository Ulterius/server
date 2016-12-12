#region

using System;
using System.Linq;
using System.Management;
using UlteriusServer.Api.Network.Messages;
using UlteriusServer.Api.Network.Models;
using UlteriusServer.Api.Services.LocalSystem;
using UlteriusServer.Api.Win32;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.Api.Network.PacketHandlers
{
    public class OperatingSystemPacketHandler : PacketHandler
    {
        private MessageBuilder _builder;
        private AuthClient _authClient;
        private Packet _packet;
        private WebSocket _client;


        public void GetEventLogs()
        {
            _builder.WriteMessage(SystemService.GetEventLogs());
            GC.Collect();
        }

        public void GetOperatingSystemInformation()
        {
            _builder.WriteMessage(OperatingSystemInformation.ToObject());
        }

        public override void HandlePacket(Packet packet)
        {
            _client = packet.Client;
            _authClient = packet.AuthClient;
            _packet = packet;
            _builder = new MessageBuilder(_authClient, _client, _packet.EndPoint, _packet.SyncKey);
            switch (_packet.PacketType)
            {
                case PacketManager.PacketTypes.RequestOsInformation:
                    GetOperatingSystemInformation();
                    break;
                case PacketManager.PacketTypes.ChangeScreenResolution:
                    ChangeScreenResolution();
                    break;
                case PacketManager.PacketTypes.GetEventLogs:
                    // GetEventLogs();
                    break;
            }
        }

        private void ChangeScreenResolution()
        {
         
            var width = int.Parse(_packet.Args[0].ToString());
            var height = int.Parse(_packet.Args[1].ToString());
            var bbp = int.Parse(_packet.Args[2].ToString());
            var freq = int.Parse(_packet.Args[3].ToString());
            var device = _packet.Args[4].ToString();
         
           
            Console.WriteLine(device);
            var code = Display.ChangeResolution(device, width, height, bbp, freq);
            string message = string.Empty;
            switch (code)
            {
                case Display.DISP_CHANGE.Successful:
                    message = "Resolution updated.";
                    break;
                case Display.DISP_CHANGE.Restart:
                    message = "A restart is required for this resolution to take effect.";
                    break;
                case Display.DISP_CHANGE.BadDualView:
                    break;
                case Display.DISP_CHANGE.BadFlags:
                    break;
                case Display.DISP_CHANGE.BadMode:
                    message = $"{width}x{height}x{bbp}x{freq} is not a supported resolution";
                    break;
                case Display.DISP_CHANGE.BadParam:
                    break;
                case Display.DISP_CHANGE.Failed:
                    message = "Resolution failed to update";
                    break;
                case Display.DISP_CHANGE.NotUpdated:
                    message = "Resolution not updated";
                    break;
            }
            var formThread = new
            {
                message,
                responseCode = code,
            };
            _builder.WriteMessage(formThread);
        }
    }
}