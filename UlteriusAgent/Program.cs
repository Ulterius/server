#region

using System;
using System.Collections.ObjectModel;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using AgentInterface;
using AgentInterface.Api.ScreenShare;
using UlteriusAgent.Networking;

#endregion

namespace UlteriusAgent
{
  

    internal class Program
    {
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);


        private static void Main(string[] args)
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                SetProcessDPIAware();
            }
            var handle = GetConsoleWindow();

            // Hide
            ShowWindow(handle, SW_HIDE);

            Tools.KillAllButMe();
            try
            {
                ScreenData.SetupDuplication();
                var inputAddress = "net.tcp://localhost/ulterius/agent/input/";
                var frameAddress = "net.pipe://localhost/ulterius/agent/frames/";
               
                var inputService = new ServiceHost(typeof(InputAgent));
                var frameService = new ServiceHost(typeof(FrameAgent));
                var inputBinding = new NetTcpBinding
                {
                    Security = new NetTcpSecurity
                    {
                        Transport = {ProtectionLevel = ProtectionLevel.None},
                        Mode = SecurityMode.None
                    },
                    MaxReceivedMessageSize = int.MaxValue
                };
                var frameBinding = new NetNamedPipeBinding
                {
                    Security = new NetNamedPipeSecurity
                    {
                        Transport = { ProtectionLevel = ProtectionLevel.None },
                        Mode = NetNamedPipeSecurityMode.None
                    },
                    MaxReceivedMessageSize = int.MaxValue
                };
                inputService.AddServiceEndpoint(typeof(IInputContract), inputBinding, inputAddress);
                frameService.AddServiceEndpoint(typeof(IFrameContract), frameBinding, frameAddress);
                inputService.Opened += delegate(object sender, EventArgs eventArgs)
                 {
                     Console.WriteLine("Input started");
                 };
                frameService.Opened += delegate (object sender, EventArgs eventArgs)
                {
                    Console.WriteLine("Frame started");
                };
                inputService.Open();
                frameService.Open();
                Console.WriteLine("Test");
                Console.Read();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " \n " + ex.StackTrace);
            }
            Console.Read();
        }

        private static void host_faulted(object sender, EventArgs e)
        {
            
        }
    }
}