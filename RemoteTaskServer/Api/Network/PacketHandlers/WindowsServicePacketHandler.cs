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
    class WindowsServicePacketHandler : PacketHandler
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

        /// <summary>
        /// To pass back information about all windows services
        /// </summary>
        public void RequestServiceInformation()
        {
            var serviceInformation = GetServiceInformation();
            _builder.WriteMessage(serviceInformation);
        }

        /// <summary>
        /// To start a service by service name
        /// </summary>
        public void StartService()
        {
            var serviceName = _packet.Args[0].ToString();
            bool serviceStarted = false;
            try
            {
                var sc = new ServiceController(serviceName);
                if (sc.Status == ServiceControllerStatus.Stopped)
                {
                    sc.Start();
                    while (sc.Status == ServiceControllerStatus.Stopped)
                    {
                        System.Threading.Thread.Sleep(1000);
                        sc.Refresh();
                    }
                    serviceStarted = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                serviceStarted = false;
            }
            var data = new
            {
                serviceStarted,
                serviceName
            };
            _builder.WriteMessage(data);
        }

        /// <summary>
        /// Stop a Windows Service
        /// </summary>
        public void StopService()
        {
            var serviceName = _packet.Args[0].ToString();
            bool serviceStopped = false;
            try
            {
                var sc = new ServiceController(serviceName);
                if (sc.CanStop)
                {
                    sc.Stop();
                    while (sc.Status != ServiceControllerStatus.Stopped)
                    {
                        System.Threading.Thread.Sleep(1000);
                        sc.Refresh();
                    }
                    serviceStopped = true;

                }
                else
                {
                    serviceStopped = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                serviceStopped = false;
            }

            var data = new
            {
                serviceStopped,
                serviceName
            };
            _builder.WriteMessage(data);
        }

        public void DisableService ()
        {
            var serviceName = _packet.Args[0].ToString();
            bool serviceDisabled;
            try
            {
                var objPath = string.Format("Win32_Service.Name='{0}'", serviceName );
                using (ManagementObject obService = new ManagementObject(new ManagementPath(objPath)))
                {
                    var result = obService.InvokeMethod("ChangeStartMode", new object[] { "Disabled" });
                    if ((uint)result == 0)
                    {
                        serviceDisabled = true;
                    }
                    else
                    {
                        serviceDisabled = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                serviceDisabled = false;
            }

            var data = new
            {
                serviceDisabled ,
                serviceName
            };
            _builder.WriteMessage(data);
        }

        public override void HandlePacket(Packet packet)
        {
            _client = packet.Client;
            _authClient = packet.AuthClient;
            _packet = packet;
            _builder = new MessageBuilder(_authClient, _client, _packet.EndPointName, _packet.SyncKey);
            switch (_packet.EndPoint)
            {
                case PacketManager.EndPoints.RequestServiceInformation:
                    RequestServiceInformation();
                    break;
                case PacketManager.EndPoints.StartService:

                    StartService();
                    break;
                case PacketManager.EndPoints.StopService:
                    StopService();
                    break;
                case PacketManager.EndPoints.DisableService:
                    DisableService();
                    break;
            }
        }
    }
}
