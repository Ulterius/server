#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AgentInterface.Api.Win32;
using UlteriusServer.Api.Network.Messages;
using UlteriusServer.Api.Network.Models;
using UlteriusServer.Utilities;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;
using System.ServiceProcess;
using System.Management;

#endregion

namespace UlteriusServer.Api.Network.PacketHandlers
{
    class WindowsServicePacketHandler
    {
        private AuthClient _authClient;
        private MessageBuilder _builder;
        private WebSocket _client;
        private Packet _packet;

        private List<WindowsServiceInformation> GetServiceInformation ()
        {
            var serviceinformation = new List<WindowsServiceInformation>();
            var services = ServiceController.GetServices();
            foreach (var service in services)
            {
                try
                {
                    var name = service.ServiceName;
                    string desc;
                    var objPath = string.Format("Win32_Service.Name='{0}'", name);
                    using (ManagementObject obService = new ManagementObject(new ManagementPath(objPath)))
                    {
                        desc = obService["Description"].ToString();
                    }
                    var status = service.Status.ToString();
                    var startup = service.StartType.ToString();
                    var serP = new WindowsServiceInformation
                    {
                        Name = name,
                        Description = desc,
                        Status = status,
                        StartupType = status
                    };
                    serviceinformation.Add(serP);

                }
                catch (Exception)
                {
                    Console.WriteLine("Error in windows service packet handler");
                }
            }
            return serviceinformation;
        }

        public void RequestServiceInformation()
        {
            var serviceInformation = GetServiceInformation();
            _builder.WriteMessage(serviceInformation);
        }
    }
}
