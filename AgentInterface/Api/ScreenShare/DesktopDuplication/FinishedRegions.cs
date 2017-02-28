using System;
using System.Drawing;
using System.Runtime.Serialization;

namespace AgentInterface.Api.ScreenShare.DesktopDuplication
{
    [DataContract]
    public class FinishedRegions 
    {
        [DataMember]
        public Bitmap Frame { get; internal set; }

        /// <summary>
        /// Gets the target region to where the operating system moved the image region.
        /// </summary>
        [DataMember]
        public Rectangle Destination { get; internal set; }

        public void Dispose()
        {
            Frame?.Dispose();
        }
    }
}
