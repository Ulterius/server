#region

using UlteriusServer.Utilities.Drive;

#endregion

namespace UlteriusServer.Api.Network.Models

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
        public string Model { get; set; }

       public Disk SmartInfo { get; set; }
    }
}