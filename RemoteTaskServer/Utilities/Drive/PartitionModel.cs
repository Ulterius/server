namespace UlteriusServer.Utilities.Drive
{
    public class PartitionModel
    {
        public string Name { get; set; }
        public string Size { get; set; }
        public string BlockSize { get; set; }
        public string StartingOffset { get; set; }
        public string Index { get; set; }
        public string DiskIndex { get; set; }
        public string BootPartition { get; set; }
        public string PrimaryPartition { get; set; }
        public string Bootable { get; set; }
    }
}
