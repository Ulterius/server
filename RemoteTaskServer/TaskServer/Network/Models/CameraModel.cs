#region

using AForge.Video.DirectShow;

#endregion

namespace UlteriusServer.TaskServer.Network.Models
{
    public static class CameraModel
    {
        public static VideoCaptureDevice Camera { get; set; }
        public static bool Active { get; set; }
    }
}