namespace UlteriusServer.TaskServer.Api.Models

{
    public class DriveInformation
    {
        public string Name { get; set; }
        public long TotalSize { get; set; }
        public long FreeSpace { get; set; }
        public bool IsReady { get; set; }
        public string VolumeLabel { get; set; }
        public string DriveFormat { get; set; }
        //Convert to strings or we get recurrsion errors 
        public string DriveType { get; set; }
        public string RootDirectory { get; set; }
    }
}