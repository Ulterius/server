namespace UlteriusServer.TaskServer.Api.Models

{
    public class GpuInformation
    {
        public string Name { get; set; }
        public int RefreshRate { get; set; }
        public string DriverVersion { get; set; }
        public string ScreenInfo { get; set; }
        public string AdapterRam { get; internal set; }
        public int VideoArchitecture { get; set; }
        public int VideoMemoryType { get; set; }
        public string[] InstalledDisplayDrivers { get; set; }
        public string AdapterCompatibility { get; set; }

        public object ToObject()
        {
            var data = new
            {
            };
            return data;
        }
    }
}