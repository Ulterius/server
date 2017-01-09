#region

using System;
using System.IO;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Text;
using AgentInterface;
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
                string address = "net.pipe://localhost/ulterius/Agent";
                ServiceHost serviceHost = new ServiceHost(typeof(ServerAgent));
                NetNamedPipeBinding binding = new NetNamedPipeBinding
                {
                    Security = new NetNamedPipeSecurity
                    {
                        Transport = {ProtectionLevel = ProtectionLevel.EncryptAndSign},
                        Mode = NetNamedPipeSecurityMode.Transport
                    },
                    MaxReceivedMessageSize = int.MaxValue
                };
                serviceHost.AddServiceEndpoint(typeof(ITUlteriusContract), binding, address);
                serviceHost.Open();
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