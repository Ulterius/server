#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using AForge.Video;
using AForge.Video.DirectShow;

#endregion

namespace UlteriusServer.WebCams
{
    public class WebCamManager
    {
        //TODO TURN INTO AN MJPEG STREAMING SERVER
        public static Dictionary<string, VideoCaptureDevice> _Cameras;
        public static ConcurrentDictionary<string, byte[]> _Frames { get; set; }
        public static ConcurrentDictionary<string, Thread> _Streams { get; set; }


        public static bool StartCamera(string cameraId)
        {
            try
            {
                var camera = _Cameras[cameraId];
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
            foreach (var camera in _Cameras)
            {
                StopCamera(camera.Key);
            }
        }

        public static void StartAllCameras()
        {
            foreach (var camera in _Cameras)
            {
                StartCamera(camera.Key);
            }
        }

        public static List<Cameras> GetCameras()
        {
            var cameras = _Cameras;
            var index = 0;
            var cameraInfo = new List<Cameras>();
            foreach (var currentCamera in cameras)
            {
                var camera = new Cameras
                {
                    Id = currentCamera.Key,
                    Name = new FilterInfoCollection(FilterCategory.VideoInputDevice)[index].Name,
                    CameraStatus = currentCamera.Value.IsRunning
                };
                cameraInfo.Add(camera);
                index++;
            }
            return cameraInfo;
        }

        public static bool StopCamera(string cameraId)
        {
            try
            {
                var camera = _Cameras[cameraId];
                if (camera == null) return false;
                if (camera.IsRunning == false) return false;
                camera.Stop();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);

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
                _Cameras = new Dictionary<string, VideoCaptureDevice>();
                _Streams = new ConcurrentDictionary<string, Thread>();
                _Frames = new ConcurrentDictionary<string, byte[]>();
                var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                for (var i = 0; i < videoDevices.Count; i++)
                {
                    var camera = new VideoCaptureDevice(videoDevices[i].MonikerString);
                    var cameraId = videoDevices[i].MonikerString.GetHashCode().ToString();
                    camera.NewFrame +=
                        (sender, e) =>
                            HandleFrame(sender, e, cameraId);
                    _Cameras.Add(cameraId, camera);
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
            public bool CameraStatus { get; set; }
        }
    }
}