#region

using System;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.ServiceModel;
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
                var inputAddress = "net.pipe://localhost/ulterius/agent/input/";
                var frameAddress = "net.pipe://localhost/ulterius/agent/frames/";
                var frameService = new ServiceHost(typeof(ServerAgent));
                var inputService = new ServiceHost(typeof(ServerAgent));
                var inputBinding = new NetNamedPipeBinding
                {
                    Security = new NetNamedPipeSecurity
                    {
                        Transport = {ProtectionLevel = ProtectionLevel.None},
                        Mode = NetNamedPipeSecurityMode.None
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
                inputService.AddServiceEndpoint(typeof(ITUlteriusContract), inputBinding, inputAddress);
                frameService.AddServiceEndpoint(typeof(ITUlteriusContract), frameBinding, frameAddress);
                frameService.Open();
                inputService.Open();
                Console.Read();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " \n " + ex.StackTrace);
            }
            Console.Read();
        }
    }
}