#region

using System.Drawing;
using UlteriusServer.Api.Win32.ScreenShare.DesktopDuplication;

#endregion

namespace UlteriusServer.Api.Win32.ScreenShare.Models
{
    
    public class FrameInformation
    {
      
        public Rectangle Bounds { get; set; }
      
        public Bitmap ScreenImage { get; set; }
    
        public bool UsingGpu { get; set; }

        public FinishedRegions[] FinishedRegions { get; set; }
    }
}