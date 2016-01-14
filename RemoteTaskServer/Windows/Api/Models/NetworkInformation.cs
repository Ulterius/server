#region

using System.Collections.Generic;
using UlteriusServer.Utilities.Network;

#endregion

namespace UlteriusServer.Windows.Api.Models

{
    public static class NetworkInformation
    {
        public static string MacAddress { get; set; }
        public static string JSON { get; set; }
        public static string PublicIp { get; set; }
        public static List<NetworkDevices> NetworkComputers { get; set; }
        public static string InternalIp { get; set; }

        public static object ToObject()
        {
            var data = new
            {
                publicIp = PublicIp.Trim(),
                networkDevices = NetworkComputers,
                macAddress = MacAddress,
                internalIp = InternalIp
            };
            return data;
        }
    }
}