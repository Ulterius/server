#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
            var bindLocal = (bool) Settings.Get("Network").BindLocal;
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
        
            var all = GetAllDevicesOnLan();
            foreach (var device in all)
            {
                var ip = device.Key.ToString();
                string name;
                var currentStatus = Convert.ToBoolean(Settings.Get("Network").SkipHostNameResolve);
                if (!currentStatus)
                {
                    try
                    {
                        var hostEntry = GetReverseDns(ip, 200);
                        name = hostEntry.Equals(ip) ? "Unknown-U" : hostEntry;
                    }
                    catch (SocketException)
                    {
                        name = "Unknown-E";
                    }
                }
                else
                {
                    name = "Unknown-O";
                }
                Devices.Add(new NetworkDevices
                {
                    Name = name,
                    Ip = device.Key.ToString(),
                    MacAddress = device.Value.ToString()
                });
            }
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
        ///     Get the IP and MAC addresses of all known devices on the LAN
        /// </summary>
        /// <remarks>
        ///     1) This table is not updated often - it can take some human-scale time
        ///     to notice that a device has dropped off the network, or a new device
        ///     has connected.
        ///     2) This discards non-local devices if they are found - these are multicast
        ///     and can be discarded by IP address range.
        /// </remarks>
        /// <returns></returns>
        private static Dictionary<IPAddress, PhysicalAddress> GetAllDevicesOnLan()
        {
            var all = new Dictionary<IPAddress, PhysicalAddress> {{GetIpAddress(), GetMacAddress()}};
            // Add this PC to the list...
            var spaceForNetTable = 0;
            // Get the space needed
            // We do that by requesting the table, but not giving any space at all.
            // The return value will tell us how much we actually need.
            GetIpNetTable(IntPtr.Zero, ref spaceForNetTable, false);
            // Allocate the space
            // We use a try-finally block to ensure release.
            var rawTable = IntPtr.Zero;
            try
            {
                rawTable = Marshal.AllocCoTaskMem(spaceForNetTable);
                // Get the actual data
                var errorCode = GetIpNetTable(rawTable, ref spaceForNetTable, false);
                if (errorCode != 0)
                {
                    // Failed for some reason - can do no more here.
                    throw new Exception($"Unable to retrieve network table. Error code {errorCode}");
                }
                // Get the rows count
                var rowsCount = Marshal.ReadInt32(rawTable);
                var currentBuffer = new IntPtr(rawTable.ToInt64() + Marshal.SizeOf(typeof(int)));
                // Convert the raw table to individual entries
                var rows = new MibIpnetrow[rowsCount];
                for (var index = 0; index < rowsCount; index++)
                {
                    rows[index] = (MibIpnetrow) Marshal.PtrToStructure(new IntPtr(currentBuffer.ToInt64() +
                                                                                  index*
                                                                                  Marshal.SizeOf(typeof(MibIpnetrow))
                        ),
                        typeof(MibIpnetrow));
                }
                // Define the dummy entries list (we can discard these)
                var virtualMac = new PhysicalAddress(new byte[] {0, 0, 0, 0, 0, 0});
                var broadcastMac = new PhysicalAddress(new byte[] {255, 255, 255, 255, 255, 255});
                foreach (var row in rows)
                {
                    var ip = new IPAddress(BitConverter.GetBytes(row.dwAddr));
                    byte[] rawMac = {row.mac0, row.mac1, row.mac2, row.mac3, row.mac4, row.mac5};
                    var pa = new PhysicalAddress(rawMac);
                    if (!pa.Equals(virtualMac) && !pa.Equals(broadcastMac) && !IsMulticast(ip))
                    {
                        //Console.WriteLine("IP: {0}\t\tMAC: {1}", ip.ToString(), pa.ToString());
                        if (!all.ContainsKey(ip))
                        {
                            all.Add(ip, pa);
                        }
                    }
                }
            }
            finally
            {
                // Release the memory.
                Marshal.FreeCoTaskMem(rawTable);
            }
            return all;
        }

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
        public static PhysicalAddress GetMacAddress()
        {
            return (from nic in NetworkInterface.GetAllNetworkInterfaces()
                where
                    nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                    nic.OperationalStatus == OperationalStatus.Up
                select nic.GetPhysicalAddress()).FirstOrDefault();
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