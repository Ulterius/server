namespace UlteriusServer.Api.Network.Models

{
    public class ProcessInformation
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public string Icon { get; set; }
        public string Name { get; set; }
        public double CpuUsage { get; set; }
        public long RamUsage { get; set; }
        public int Threads { get; set; }
        public int Handles { get; set; }
    }
}