#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Security;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using AgentInterface;
using AgentInterface.Api.Models;
using AgentInterface.Api.ScreenShare.DesktopDuplication;
using UlteriusServer.Utilities;

#endregion

namespace UlteriusServer.Api.Network
{
    public class UlteriusAgentClient
    {
        private ITUlteriusContract InputChannel { get; set; }

        private ITUlteriusContract FrameChannel { get; set; }

        public void Start(bool keepAlive = true)
        {
            var inputAddress = "net.pipe://localhost/ulterius/agent/input/";
            var frameAddress = "net.pipe://localhost/ulterius/agent/frames/";

            var inputBinding = new NetNamedPipeBinding
            {
                Security = new NetNamedPipeSecurity
                {
                    Transport = {ProtectionLevel = ProtectionLevel.None},
                    Mode = NetNamedPipeSecurityMode.None
                },
                MaxReceivedMessageSize = int.MaxValue
            };
            var ep = new EndpointAddress(inputAddress);
            InputChannel = ChannelFactory<ITUlteriusContract>.CreateChannel(inputBinding, ep);

            var frameBinding = new NetNamedPipeBinding
            {
                Security = new NetNamedPipeSecurity
                {
                    Transport = { ProtectionLevel = ProtectionLevel.None },
                    Mode = NetNamedPipeSecurityMode.None
                },
                MaxReceivedMessageSize = int.MaxValue
            };
            var epf = new EndpointAddress(frameAddress);
            FrameChannel = ChannelFactory<ITUlteriusContract>.CreateChannel(frameBinding, epf);


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
                InputChannel?.MouseScroll(positive);
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
                return FrameChannel != null && FrameChannel.KeepAlive();
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
                return FrameChannel.GetCleanFrame();
            }
            catch (EndpointNotFoundException)
            {
                return null;
            }
            catch (TimeoutException)
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
                return FrameChannel.GetFullFrame();
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
                InputChannel.HandleRightMouseDown();
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
                InputChannel.HandleRightMouseUp();
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
                InputChannel.MoveMouse(x, y);
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
                InputChannel.HandleLeftMouseDown();
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
                InputChannel.HandleLeftMouseUp();
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
                InputChannel.HandleKeyDown(keyCodes);
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
                InputChannel.HandleKeyUp(keyCodes);
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
                InputChannel.HandleRightClick();
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
                var temp = InputChannel.GetGpuTemp(gpuName);
                return temp;
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
                return InputChannel.GetDisplayInformation();
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
                InputChannel.SetActiveMonitor(index);
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
                return InputChannel.GetCpuTemps();
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