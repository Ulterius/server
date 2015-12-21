using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using RemoteTaskServer.Utilities.Network;

namespace RemoteTaskServer.Api.Models

{
    public static class NetworkInformation
    {
        public static string MacAddress { get; set; }
        public static string JSON { get; set; }
        public static string PublicIp { get; set; }
        public static List<NetworkDevices> NetworkComputers { get; set; }
        public static string InternalIp { get; set; }


        public static string ToJson()
        {
            if (string.IsNullOrEmpty(JSON))
            {
                var json =
                    new JavaScriptSerializer().Serialize(
                        new
                        {
                            publicIp = PublicIp.Trim(),
                            networkDevices = NetworkComputers,
                            macAddress = MacAddress,
                            internalIp = InternalIp
                        });
                JSON = json;
            }
            return JSON;
        }
    }
}