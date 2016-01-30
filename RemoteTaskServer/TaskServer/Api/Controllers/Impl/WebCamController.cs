#region

using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using M1.Video;
using UlteriusServer.TaskServer.Api.Serialization;
using UlteriusServer.WebCams;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers.Impl
{
    public class WebCamController : ApiController
    {
        private readonly WebSocket client;
        private readonly Packets packet;
        private readonly ApiSerializator serializator = new ApiSerializator();


        public WebCamController(WebSocket client, Packets packet)
        {
            this.client = client;
            this.packet = packet;
        }

        public void RefreshCameras()
        {
            WebCamManager.LoadWebcams();
            var data = new
            {
                cameraFresh = true,
                message = "Cameras have been refreshed!"
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
        }
        public void GetCameras()
        {
            var cameras = WebCamManager.GetCameras();
            var data = new
            {
                cameraInfo = cameras
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
        }

      

        public void StartCamera()
        {
            var cameraId = packet.args.First().ToString();
            var cameraStarted = WebCamManager.StartCamera(cameraId);
            var camera = WebCamManager._Cameras[cameraId.GetHashCode().ToString()];
            var data = new
            {
                cameraStatus = camera.CameraState.ToString(),
                cameraStarted
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
        }

        public void StopCamera()
        {
            var cameraId = packet.args.First().ToString();
            var cameraStopped = WebCamManager.StopCamera(cameraId);
            var camera = WebCamManager._Cameras[cameraId.GetHashCode().ToString()];
            var data = new
            {
                cameraStatus = camera.CameraState.ToString(),
                cameraStopped
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
        }

        public void PauseCamera()
        {
            var cameraId = packet.args.First().ToString();
            var cameraPaused = WebCamManager.PauseCamera(cameraId);
            var camera = WebCamManager._Cameras[cameraId.GetHashCode().ToString()];
            var data = new
            {
                cameraStatus = camera.CameraState.ToString(),
                cameraPaused
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
        }

        public void StartStream()
        {
           
            var cameraId = packet.args.First().ToString();
            var streamThread = new Thread(() => GetWebCamFrame(cameraId));
            WebCamManager._Streams[cameraId.GetHashCode().ToString()] = streamThread;
            WebCamManager._Streams[cameraId.GetHashCode().ToString()].IsBackground = true;
            WebCamManager._Streams[cameraId.GetHashCode().ToString()].Start();

            var data = new
            {
                cameraId,
                cameraStreamStarted = true
            };
           
            serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
        }


        public void StopStream()
        {
            Console.WriteLine("Stream stopped");
            var cameraId = packet.args.First().ToString();
            var streamThread = WebCamManager._Streams[cameraId.GetHashCode().ToString()];
            if (streamThread != null)
            {
                WebCamManager._Streams[cameraId.GetHashCode().ToString()].Abort();
                if (client.IsConnected)
                {
                    var data = new
                    {
                        cameraId,
                        cameraStreamStopped = true
                    };
                    serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
                }
               
            }
        }

            
        
        public void GetWebCamFrame(string cameraId)
        {
            while (client.IsConnected)
            {
                try
                {
                    var cameraHash = cameraId.GetHashCode().ToString();
                    var imageBytes = WebCamManager._Frames[cameraHash];
                    var data = new
                    {
                        cameraId,
                        cameraFrame = imageBytes
                    };

                    serializator.Serialize(client, "getcameraframe", packet.syncKey, data);

                }
                catch (Exception e)
                {
                    var data = new
                    {
                        cameraFrameFailed = true,
                        cameraId,
                        message = "Something went wrong and we were unable to get a feed from this camera!",
                        exceptionMessage = e.StackTrace
                    };
                    serializator.Serialize(client, "getcameraframe", packet.syncKey, data);
                }
            }
           StopStream();
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