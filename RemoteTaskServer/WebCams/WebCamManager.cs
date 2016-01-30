#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
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

        public static void StopAllCameras()
        {
            foreach (var camera in _Cameras)
            {
                StopCamera(camera.Value.CameraInfo.Id);
            }
        }

        public static void StartAllCameras()
        {
            foreach (var camera in _Cameras)
            {
                StartCamera(camera.Value.CameraInfo.Id);
            }
        }

        public static List<Cameras> GetCameras()
        {
            var cameras = _Cameras;
            var cameraInfo = cameras.Select(currentCamera => new Cameras
            {
                Id = currentCamera.Value.CameraInfo.Id,
                Name = currentCamera.Value.CameraInfo.FriendlyName,
                DisplayName = currentCamera.Value.CameraInfo.DisplayName,
                DevicePath = currentCamera.Value.CameraInfo.DevicePath,
                CameraStatus = currentCamera.Value.CameraState.ToString()

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
            try
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
            catch (Exception)
            {

                // Eat it whole!
            }
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
            public string CameraStatus { get; set; }
        }
    }
}