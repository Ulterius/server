namespace UlteriusServer.Api.Models

{
    public class SystemProcesses
    {
        public int id { get; set; }
        public string path { get; set; }
        public string icon { get; set; }
        public string name { get; set; }
        public int cpuUsage { get; set; }
        public long ramUsage { get; set; }
        public int threads { get; set; }
        public int handles { get; set; }
    }
}