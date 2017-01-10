#region

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Security;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using AgentInterface;
using AgentInterface.Api.Models;
using UlteriusServer.Utilities;

#endregion

namespace UlteriusServer.Api.Network
{
    public class UlteriusAgentClient
    {
        private ITUlteriusContract Channel { get; set; }

        public void Start(bool keepAlive = true)
        {
            var address = "net.pipe://localhost/ulterius/Agent";
            var binding = new NetNamedPipeBinding
            {
                Security = new NetNamedPipeSecurity
                {
                    Transport = {ProtectionLevel = ProtectionLevel.EncryptAndSign},
                    Mode = NetNamedPipeSecurityMode.Transport
                },
                MaxReceivedMessageSize = int.MaxValue
            };

            var ep = new EndpointAddress(address);
            Channel = ChannelFactory<ITUlteriusContract>.CreateChannel(binding, ep);
            if (!keepAlive) return;
            var task = new Task(KeepAlive);
            task.Start();
        }

        private void KeepAlive()
        {
            //let this sleep after launch so we dont get spammed
            Thread.Sleep(10000);
            while (true)
            {
                Tools.RestartDaemon();
                var alive = ChannelActive();
                if (!alive)
                {
                    var agentList = Process.GetProcessesByName("UlteriusAgent");
                    if (agentList.Length == 0)
                    {
                        Tools.RestartAgent();
                    }
                    Start(false);
                }
                Thread.Sleep(1000);
            }
        }

        public void ScrollMouse(bool positive)
        {
            try
            {
                Channel?.MouseScroll(positive);
            }
            catch (EndpointNotFoundException)
            {
                //
            }
            catch (CommunicationException)
            {
                //
            }
        }

        public bool ChannelActive()
        {
            try
            {
                return Channel != null && Channel.KeepAlive();
            }
            catch (EndpointNotFoundException)
            {
                return false;
            }
            catch (CommunicationException)
            {
                return false;
            }
        }

        public FrameInformation GetCleanFrame()
        {
            try
            {
                return Channel?.GetCleanFrame();
            }
            catch (EndpointNotFoundException)
            {
                return null;
            }
            catch (CommunicationException)
            {
                return null;
            }
        }

        public FrameInformation GetFullFrame()
        {
            try
            {
                return Channel?.GetFullFrame();
            }
            catch (EndpointNotFoundException)
            {
                return null;
            }
            catch (CommunicationException)
            {
                return null;
            }
        }

        public void HandleRightMouseDown()
        {
            try
            {
                Channel?.HandleRightMouseDown();
            }
            catch (EndpointNotFoundException)
            {
            }
            catch (CommunicationException)
            {
            }
        }

        public void HandleRightMouseUp()
        {
            try
            {
                Channel?.HandleRightMouseUp();
            }
            catch (EndpointNotFoundException)
            {
            }
            catch (CommunicationException)
            {
            }
        }

        public void MoveMouse(int x, int y)
        {
            try
            {
                Channel?.MoveMouse(x, y);
            }
            catch (EndpointNotFoundException)
            {
            }
            catch (CommunicationException)
            {
            }
        }

        public void HandleLeftMouseDown()
        {
            try
            {
                Channel?.HandleLeftMouseDown();
            }
            catch (EndpointNotFoundException)
            {
            }
            catch (CommunicationException)
            {
            }
        }

        public void HandleLeftMouseUp()
        {
            try
            {
                Channel?.HandleLeftMouseUp();
            }
            catch (EndpointNotFoundException)
            {
            }
            catch (CommunicationException)
            {
            }
        }

        public void HandleKeyDown(List<int> keyCodes)
        {
            try
            {
                Channel?.HandleKeyDown(keyCodes);
            }
            catch (EndpointNotFoundException)
            {
            }
            catch (CommunicationException)
            {
            }
        }

        public void HandleKeyUp(List<int> keyCodes)
        {
            try
            {
                Channel?.HandleKeyUp(keyCodes);
            }
            catch (EndpointNotFoundException)
            {
            }
            catch (CommunicationException)
            {
            }
        }

        public void HandleRightClick()
        {
            try
            {
                Channel?.HandleRightClick();
            }
            catch (EndpointNotFoundException)
            {
            }
            catch (CommunicationException)
            {
            }
        }

        public float GetGpuTemp(string gpuName)
        {
            try
            {
                var temp = Channel?.GetGpuTemp(gpuName);
                return temp ?? -1;
            }
            catch (EndpointNotFoundException)
            {
                return -1;
            }
            catch (CommunicationException)
            {
                return -1;
            }
        }

        public List<DisplayInformation> GetDisplayInformation()
        {
            try
            {
                return Channel?.GetDisplayInformation();
            }
            catch (EndpointNotFoundException)
            {
                return null;
            }
            catch (CommunicationException)
            {
                return null;
            }
        }

        public void SetActiveMonitor(int index)
        {
            try
            {
                Channel?.SetActiveMonitor(index);
            }
            catch (EndpointNotFoundException)
            {
                
            }
            catch (CommunicationException)
            {
               
            }
        }
       
        public List<float> GetCpuTemps()
        {
            try
            {
                return Channel?.GetCpuTemps();
            }
            catch (EndpointNotFoundException)
            {
                return null;
            }
            catch (CommunicationException)
            {
                return null;
            }
        }

     
    }
}