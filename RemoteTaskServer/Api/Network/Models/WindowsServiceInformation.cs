namespace UlteriusServer.Api.Network.Models
{
    public class WindowsServiceInformation
    {
        public enum ServiceStatus
        {
            Running,
            Stopped,
            paused,
            Disabled
        }
        public string Name { get; set; }

        public string Description { get; set; }

        public string Status { get; set; }

        public string StartupType { get; set; }

        
    }
}
