#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AForge.Video;
using AForge.Video.DirectShow;

#endregion

namespace UlteriusServer.WebCams
{
    public class WebCamManager
    {
        //TODO TURN INTO AN MJPEG STREAMING SERVER
        public static Dictionary<string, VideoCaptureDevice> Cameras;
        public static ConcurrentDictionary<string, byte[]> Frames { get; set; }
        public static ConcurrentDictionary<string, Task> Streams { get; set; }
     

        public static bool StartCamera(string cameraId)
        {
            try
            {
                var camera = Cameras[cameraId];
                if (camera == null) return false;
                if (camera.IsRunning) return false;
                camera.Start();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void StopAllCameras()
        {
            foreach (var camera in Cameras)
            {
                StopCamera(camera.Key);
            }
        }

        public static void StartAllCameras()
        {
            foreach (var camera in Cameras)
            {
                StartCamera(camera.Key);
            }
        }

        public static List<Camera> GetCameras()
        {
            var cameras = Cameras;
            int[] index = {0};
            var cameraInfo = new List<Camera>();
            foreach (var camera in cameras.Select(currentCamera => new Camera
            {
                Id = currentCamera.Key,
                Name = new FilterInfoCollection(FilterCategory.VideoInputDevice)[index[0]].Name,
                CameraStatus = currentCamera.Value.IsRunning
            }))
            {
                cameraInfo.Add(camera);
                index[0]++;
            }
            return cameraInfo;
        }

        public static bool StopCamera(string cameraId)
        {
            try
            {
                var camera = Cameras[cameraId];
                if (camera == null) return false;
                if (camera.IsRunning == false) return false;
                camera.SignalToStop();
                camera.WaitForStop();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to stop camera");
                Console.WriteLine(e);
                return false;
            }
        }

        public static bool PauseCamera(string cameraId)
        {
            return true;
        }


        public static void LoadWebcams()
        {
            try
            {
                Cameras = new Dictionary<string, VideoCaptureDevice>();
                Streams = new ConcurrentDictionary<string, Task>();
                Frames = new ConcurrentDictionary<string, byte[]>();
                var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                for (var i = 0; i < videoDevices.Count; i++)
                {
                    var camera = new VideoCaptureDevice(videoDevices[i].MonikerString);
                    var cameraId = Guid.NewGuid().ToString("N").ToUpper();
                    camera.NewFrame +=
                        (sender, e) =>
                            HandleFrame(sender, e, cameraId);
                    Cameras.Add(cameraId, camera);
                }
                Console.WriteLine($"{Cameras.Count} cameras loaded");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void HandleFrame(object sender, NewFrameEventArgs camera, string webcamIdHash)
        {
 
            if (sender == null) throw new ArgumentNullException(nameof(sender));
            if (camera?.Frame == null) return;
            using (var ms = new MemoryStream())
            {
                camera.Frame.Save(ms, ImageFormat.Jpeg);
                var imageBytes = ms.ToArray();
                Frames[webcamIdHash] = imageBytes;
            }
        }


        public class Camera
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public string DevicePath { get; set; }
            public bool CameraStatus { get; set; }
        }
    }
}