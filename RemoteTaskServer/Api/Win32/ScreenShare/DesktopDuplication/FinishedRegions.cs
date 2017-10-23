using System.Drawing;

namespace UlteriusServer.Api.Win32.ScreenShare.DesktopDuplication
{
    public class FinishedRegions 
    {
        public Bitmap Frame { get; internal set; }

        /// <summary>
        /// Gets the target region to where the operating system moved the image region.
        /// </summary>
        public Rectangle Destination { get; internal set; }

        public void Dispose()
        {
            Frame?.Dispose();
        }
    }
}
