#region

using System.Web.Script.Serialization;

#endregion

namespace UlteriusServer.Api.Models

{
    public static class CpuInformation
    {
        public static string Name { get; set; }
        public static string Id { get; set; }
        public static string Socket { get; set; }
        public static string Description { get; set; }
        public static ushort AddressWidth { get; set; }
        public static ushort DataWidth { get; set; }
        public static uint SpeedMHz { get; set; }
        public static uint BusSpeedMHz { get; set; }
        public static ulong L2Cache { get; set; }
        public static ulong L3Cache { get; set; }
        public static uint Cores { get; set; }
        public static uint Threads { get; set; }
        public static string Architecture { get; set; }
        public static string JSON { get; set; }

        public static string ToJson()
        {
            if (string.IsNullOrEmpty(JSON))
            {
                var json =
                    new JavaScriptSerializer().Serialize(
                        new
                        {
                            cpuName = Name,
                            id = Id,
                            socket = Socket,
                            description = Description,
                            addressWidth = AddressWidth,
                            dataWidth = DataWidth,
                            speedMhz = SpeedMHz,
                            busSpeedMhz = BusSpeedMHz,
                            l2Cache = L2Cache,
                            l3Cache = L3Cache,
                            cores = Cores,
                            threads = Threads,
                            architecture = Architecture
                        });
                JSON = json;
            }
            return JSON;
        }
    }
}