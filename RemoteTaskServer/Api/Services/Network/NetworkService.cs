#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UlteriusServer.Utilities;

#endregion

namespace UlteriusServer.Api.Services.Network
{
    public class NetworkDevices
    {
        public string Name { get; set; }
        public string Ip { get; set; }
        public string MacAddress { get; set; }
    }

    internal class NetworkService
    {
        private static readonly List<NetworkDevices> Devices = new List<NetworkDevices>();


        public static IPAddress GetAddress()
        {
            //if VMware or VMPlayer installed, we get the wrong address, so try getting the physical first.
            var address = GetPhysicalIpAdress();
            //Default since we couldn't.
            if (string.IsNullOrEmpty(address))
            {
                address = GetIPv4Address();
            }
            var config = Config.Load();
            var bindLocal = config.Network.BindLocal;
            return bindLocal ? IPAddress.Parse(address) : IPAddress.Any;
        }

        public static string GetDisplayAddress()
        {
            //if VMware or VMPlayer installed, we get the wrong address, so try getting the physical first.
            var address = GetPhysicalIpAdress();
            //Default since we couldn't.
            if (string.IsNullOrEmpty(address))
            {
                address = GetIPv4Address();
            }
            return address;
        }

        private static string GetReverseDns(string ip, int timeout)
        {
            try
            {
                GetHostEntryHandler callback = Dns.GetHostEntry;
                var result = callback.BeginInvoke(ip, null, null);
                return result.AsyncWaitHandle.WaitOne(timeout, false) ? callback.EndInvoke(result).HostName : ip;
            }
            catch (Exception)
            {
                return ip;
            }
        }

        public static string GetPhysicalIpAdress()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                var addr = ni.GetIPProperties().GatewayAddresses.FirstOrDefault();
                if (addr == null || addr.Address.ToString().Equals("0.0.0.0")) continue;
                if (ni.NetworkInterfaceType != NetworkInterfaceType.Wireless80211 &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Ethernet) continue;
                foreach (var ip in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.Address.ToString();
                    }
                }
            }
            return string.Empty;
        }

        public static List<NetworkDevices> ConnectedDevices()
        {
           
            return Devices;
        }


        /// <summary>
        ///     GetIpNetTable external method
        /// </summary>
        /// <param name="pIpNetTable"></param>
        /// <param name="pdwSize"></param>
        /// <param name="bOrder"></param>
        /// <returns></returns>
        [DllImport("IpHlpApi.dll")]
        [return: MarshalAs(UnmanagedType.U4)]
        private static extern int GetIpNetTable(IntPtr pIpNetTable,
            [MarshalAs(UnmanagedType.U4)] ref int pdwSize, bool bOrder);

       
        /// <summary>
        ///     Gets the IP address of the current PC
        /// </summary>
        /// <returns></returns>
        public static IPAddress GetIpAddress()
        {
            var strHostName = Dns.GetHostName();
            var ipEntry = Dns.GetHostEntry(strHostName);
            var addr = ipEntry.AddressList;
            foreach (var ip in addr.Where(ip => !ip.IsIPv6LinkLocal))
            {
                return ip;
            }
            return addr.Length > 0 ? addr[0] : null;
        }

        /// <summary>
        ///     Gets the MAC address of the current PC.
        /// </summary>
        /// <returns></returns>
        public static string GetMacAddress()
        {
            const int minMacAddrLength = 12;
            var macAddress = string.Empty;
            long maxSpeed = -1;

            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                var tempMac = nic.GetPhysicalAddress().ToString();
                if (nic.Speed <= maxSpeed || string.IsNullOrEmpty(tempMac) || tempMac.Length < minMacAddrLength)
                    continue;
                maxSpeed = nic.Speed;
                macAddress = tempMac;
            }
            return macAddress;
        }

        /// <summary>
        ///     Returns true if the specified IP address is a multicast address
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        private static bool IsMulticast(IPAddress ip)
        {
            var result = true;
            if (!ip.IsIPv6Multicast)
            {
                var highIp = ip.GetAddressBytes()[0];
                if (highIp < 224 || highIp > 239)
                {
                    result = false;
                }
            }
            return result;
        }

        public static string GetPublicIp(string serviceUrl = "https://api.ulterius.io/network/ip/")
        {
            try
            {
                using (var webClient = new WebClient())
                {
                    var response = webClient.DownloadString(serviceUrl);
                    if (!string.IsNullOrEmpty(response)) return response;
                    //try again
                    response = webClient.DownloadString("https://icanhazip.com/");
                    if (string.IsNullOrEmpty(response))
                    {
                        response = "Unknown";
                    }
                    return response;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting IP");
                Console.WriteLine(ex.Message);
                return "null";
            }
        }

        private static string GetIPv4Address()
        {
            var ips = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (var i in ips.Where(i => i.AddressFamily == AddressFamily.InterNetwork))
            {
                return i.ToString();
            }
            return "127.0.0.1";
        }

        private delegate IPHostEntry GetHostEntryHandler(string ip);

        /// <summary>
        ///     MIB_IPNETROW structure returned by GetIpNetTable
        ///     DO NOT MODIFY THIS STRUCTURE.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct MibIpnetrow
        {
            [MarshalAs(UnmanagedType.U4)] private readonly int dwIndex;
            [MarshalAs(UnmanagedType.U4)] private readonly int dwPhysAddrLen;
            [MarshalAs(UnmanagedType.U1)] public readonly byte mac0;
            [MarshalAs(UnmanagedType.U1)] public readonly byte mac1;
            [MarshalAs(UnmanagedType.U1)] public readonly byte mac2;
            [MarshalAs(UnmanagedType.U1)] public readonly byte mac3;
            [MarshalAs(UnmanagedType.U1)] public readonly byte mac4;
            [MarshalAs(UnmanagedType.U1)] public readonly byte mac5;
            [MarshalAs(UnmanagedType.U1)] private readonly byte mac6;
            [MarshalAs(UnmanagedType.U1)] private readonly byte mac7;
            [MarshalAs(UnmanagedType.U4)] public readonly int dwAddr;
            [MarshalAs(UnmanagedType.U4)] private readonly int dwType;
        }
    }
}