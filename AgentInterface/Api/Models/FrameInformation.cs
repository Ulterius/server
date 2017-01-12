#region

using System.Drawing;
using System.Runtime.Serialization;
using AgentInterface.Api.ScreenShare.DesktopDuplication;

#endregion

namespace AgentInterface.Api.Models
{
    [DataContract]
    public class FrameInformation
    {
        [DataMember]
        public Rectangle Bounds { get; set; }
        [DataMember]
        public Bitmap ScreenImage { get; set; }
        [DataMember]
        public bool UsingGpu { get; set; }
        [DataMember]
        public FinishedRegions[] FinishedRegions { get; set; }
    }
}