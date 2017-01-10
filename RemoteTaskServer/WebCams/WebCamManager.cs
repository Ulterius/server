#region

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Vision.Motion;

#endregion

namespace UlteriusServer.WebCams
{
    public class WebCamManager
    {
        private static MotionDetector detector;
        

        public static ConcurrentDictionary<string, byte[]> CameraFrames { get; set; }

        public static ConcurrentDictionary<string, Camera> Cameras { get; set; }

        public static void LoadCameras()
        {
            BlobCountingObjectsProcessing motionProcessor = new BlobCountingObjectsProcessing();

             detector = new MotionDetector(
                new SimpleBackgroundModelingDetector(),
                motionProcessor);

            Cameras = new ConcurrentDictionary<string, Camera>();
            CameraFrames = new ConcurrentDictionary<string, byte[]>();
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (videoDevices.Count == 0)
            {
                return;
            }
            foreach (FilterInfo device in videoDevices)
            {
                var cameraId = Guid.NewGuid().ToString("N").ToUpper();
                var camera = new Camera
                {
                    Id = cameraId,
                    Name = device.Name,
                    Moniker = device.MonikerString,
                    Physical = new VideoCaptureDevice(device.MonikerString)
                };
                camera.Physical.NewFrame += (sender, e) => HandleFrame(sender, e, cameraId);
                Cameras.TryAdd(cameraId, camera);
            }
            Console.WriteLine($"{Cameras.Count} cameras loaded");
        }


        public static bool StartCamera(string cameraId)
        {
            try
            {
                var camera = Cameras[cameraId];
                if (camera == null) return false;
                if (camera.Physical.IsRunning) return true;
                camera.Physical.Start();
                Cameras[cameraId] = camera;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }


        public static List<Camera> GetCameras()
        {
            return Cameras.Values.ToList();
        }

        public static bool StopCamera(string cameraId)
        {
            try
            {

                var camera = Cameras[cameraId];
                if (camera == null) return false;
                if (!camera.Physical.IsRunning) return true;
                camera.Physical.SignalToStop();
                camera.Physical.WaitForStop();
                Cameras[cameraId] = camera;
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to stop camera");
                Console.WriteLine(e);
                return false;
            }
        }


    

        public static byte[] ImageToByte2(Image img)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, ImageFormat.Jpeg);
                return stream.ToArray();
            }
        }

        private static void HandleFrame(object sender, NewFrameEventArgs camera, string cameraId)
        {
            if (sender == null) throw new ArgumentNullException(nameof(sender));
            if (camera?.Frame == null) return;
            byte[] bytes;
            if (!CameraFrames.TryGetValue(cameraId, out bytes))
            {
                CameraFrames[cameraId] = ImageToByte2(camera.Frame);
            }
            else
            {
                detector.ProcessFrame(camera.Frame);
                CameraFrames[cameraId] = ImageToByte2(camera.Frame);
               
            }
            
        }


        public class Camera
        {
            public object Name { get; set; }
            public VideoCaptureDevice Physical { get; set; }
            public string Id { get; set; }
            public bool CameraStatus { get; set; }
            public bool StreamActive { get; set; }
            public string Moniker { get; set; }
        }
    }
}