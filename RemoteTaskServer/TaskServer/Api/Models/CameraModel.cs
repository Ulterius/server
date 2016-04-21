using AForge.Video.DirectShow;

namespace UlteriusServer.TaskServer.Api.Models
{
    public static class CameraModel
    {
        public static VideoCaptureDevice Camera { get; set; }
        public static bool Active { get; set; }
    }
}