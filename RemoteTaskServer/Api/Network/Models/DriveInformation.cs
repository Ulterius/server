#region


#endregion

using System.Collections.Generic;
using UlteriusServer.Utilities.Drive;

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
        public List<SmartModel> SmartData { get; set; }
        public List<PartitionModel> Partitions { get; set; }
        public string DriveHealth { get; set; }
        public string MediaType { get; set; }
        public string Serial { get; set; }
        public string Interface { get; set; }
        public string TotalPartitions { get; set; }
        public string Signature { get; set; }
        public string Firmware { get; set; }
        public string Cylinders { get; set; }
        public string Sectors { get; set; }
        public string Heads { get; set; }
        public string Tracks { get; set; }
        public string BytesPerSecond { get; set; }
        public string SectorsPerTrack { get; set; }
        public string TracksPerCylinder { get; set; }
    }
}