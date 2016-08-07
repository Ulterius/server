namespace UlteriusServer.Api.Network.Models

{
    public static class ServerOperatingSystem
    {
        public static string Name { get; set; }
        public static string Version { get; set; }
        public static uint MaxProcessCount { get; set; }
        public static ulong MaxProcessRam { get; set; }
        public static string Architecture { get; set; }
        public static string SerialNumber { get; set; }
        public static string Build { get; set; }
        public static string Json { get; set; }

        public static object ToObject()
        {
            var data = new
            {
                name = Name,
                version = Version,
                maxProcessCount = MaxProcessCount,
                maxProcessRam = MaxProcessRam,
                architecture = Architecture,
                serialNumber = SerialNumber,
                build = Build
            };
            return data;
        }
    }
}