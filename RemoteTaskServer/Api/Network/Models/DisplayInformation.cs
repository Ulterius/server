namespace UlteriusServer.Api.Network.Models
{
    public class DisplayInformation
    {
        public string FriendlyName { get; set; }
        public int MonitorHeight { get; set; }
        public int MonitorWidth { get; set; }
        public int BitsPerPixel { get; set; }
        /// <summary>The device is part of the desktop.</summary>
        public bool Primary { get; set; }

        public int Frequency { get; set; }
        /// <summary>The device is part of the desktop.</summary> 
        public bool MultiDriver { get; set; }
        public bool Attached { get; set; }
        /// <summary>The device is removable; it cannot be the primary display.</summary>
        public bool Removable { get; set; }
        /// <summary>The device is VGA compatible.</summary>
        public bool VgaCompatible { get; set; }
        /// <summary>Represents a pseudo device used to mirror application drawing for remoting or other purposes.</summary>
        public bool MirroringDriver { get; set; }
        /// <summary>The device has more display modes than its output devices support.</summary>
        public bool ModesPruned { get; set; }
        public bool Remote { get; set; }
        public bool Disconnect { get; set; }




    }
}
