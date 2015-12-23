#region

using System.Web.Script.Serialization;

#endregion

namespace RemoteTaskServer.Api.Models

{
    public static class ServerOperatingSystem
    {
        public static string Name { get; set; }
        public static string Version { get; set; }
        public static uint MaxProcessCount { get; set; }
        public static ulong MaxProcessRAM { get; set; }
        public static string Architecture { get; set; }
        public static string SerialNumber { get; set; }
        public static string Build { get; set; }
        public static string JSON { get; set; }

        public static string ToJson()
        {
            if (string.IsNullOrEmpty(JSON))
            {
                var json =
                    new JavaScriptSerializer().Serialize(
                        new
                        {
                            name = Name,
                            version = Version,
                            maxProcessCount = MaxProcessCount,
                            maxProcessRam = MaxProcessRAM,
                            architecture = Architecture,
                            serialNumber = SerialNumber,
                            build = Build
                        });
                JSON = json;
            }
            return JSON;
        }
    }
}