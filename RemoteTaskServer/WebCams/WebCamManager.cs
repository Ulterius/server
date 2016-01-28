#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using M1.Video;

#endregion

namespace UlteriusServer.WebCams
{
    public class WebCamManager
    {
        //TODO TURN INTO AN MJPEG STREAMING SERVER
        public static Dictionary<string, Camera> _Cameras;
        public static ConcurrentDictionary<string, byte[]> _Frames { get; set; }


        public static bool StartCamera(string cameraId)
        {
            var camera = _Cameras[cameraId.GetHashCode().ToString()];
            if (camera == null) return false;
            if (camera.CameraState == CameraState.Started) return false;
            camera.Start();
            return true;
        }

        public static List<Cameras> GetCameras()
        {
            var cameras = CameraInfo.GetCameraInfos();
            var cameraInfo = cameras.Select(currentCamera => new Cameras
            {
                Id = currentCamera.Id,
                Name = currentCamera.FriendlyName,
                DisplayName = currentCamera.DisplayName,
                DevicePath = currentCamera.DevicePath
            }).ToList();
            return cameraInfo;
        }

        public static bool StopCamera(string cameraId)
        {
            var camera = _Cameras[cameraId.GetHashCode().ToString()];
            if (camera == null) return false;
            if (camera.CameraState == CameraState.None) return false;
            camera.Stop();
            return true;
        }

        public static bool PauseCamera(string cameraId)
        {
            var camera = _Cameras[cameraId.GetHashCode().ToString()];
            if (camera == null) return false;
            if (camera.CameraState == CameraState.None) return false;
            camera.Pause();
            return true;
        }


        public static void LoadWebcams()
        {
            _Cameras = new Dictionary<string, Camera>();
            _Frames = new ConcurrentDictionary<string, byte[]>();
            foreach (var hardwareCamera in CameraInfo.GetCameraInfos())
            {
                var camera = new Camera(CameraInfo.GetCameraInfo(hardwareCamera.Id), hardwareCamera.VideoFormats[0]);
                camera.Capture +=
                    (sender, e) =>
                        HandleFrame(sender, e, hardwareCamera.Id.GetHashCode().ToString());
                _Cameras.Add(hardwareCamera.Id.GetHashCode().ToString(), camera);
            }
            Console.WriteLine(_Cameras.Count + " cameras loaded");
        }

        private static void HandleFrame(object sender, NewFrameEventArgs camera, string webcamIdHash)
        {
            if (camera?.Frame != null)
            {
                using (var ms = new MemoryStream())
                {
                    camera.Frame.Save(ms, ImageFormat.Jpeg);
                    var imageBytes = ms.ToArray();
                    _Frames[webcamIdHash] = imageBytes;
                }
                        
            }
        }

        public class Cameras
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public string DevicePath { get; set; }
        }
    }
}