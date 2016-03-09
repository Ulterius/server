#region

using System;
using System.Linq;
using System.Threading;
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
                message = "Camera have been refreshed!"
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
            try
            {
                var cameraStarted = WebCamManager.StartCamera(cameraId);
                var camera = WebCamManager.Cameras[cameraId];
                var data = new
                {
                    cameraId = cameraId,
                    cameraRunning = camera.IsRunning,
                    cameraStarted
                };
                serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
            }
            catch (Exception)
            {

                var data = new
                {
                    cameraId = cameraId,
                    cameraRunning = false,
                    cameraStarted = false
                };
                serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
            }
        }

        public void StopCamera()
        {
            var cameraId = packet.args.First().ToString();
            try
            {
               
                var cameraStopped = WebCamManager.StopCamera(cameraId);
                var camera = WebCamManager.Cameras[cameraId];
                var data = new
                {
                    cameraId,
                    cameraRunning = camera.IsRunning,
                    cameraStopped
                };
                serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
            }
            catch (Exception)
            {

                var data = new
                {
                    cameraId,
                    cameraRunning = false,
                    cameraStarted = false
                };
                serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
            }
        }

        public void PauseCamera()
        {
            var cameraId = packet.args.First().ToString();
            var cameraPaused = WebCamManager.PauseCamera(cameraId);
            var camera = WebCamManager.Cameras[cameraId];
            var data = new
            {
                cameraRunning = camera.IsRunning,
                cameraPaused
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
        }

        public void StartStream()
        {
            var cameraId = packet.args.First().ToString();
            try
            {
                
                var streamThread = new Thread(() => GetWebCamFrame(cameraId));
                WebCamManager.Streams[cameraId] = streamThread;
                WebCamManager.Streams[cameraId].IsBackground = true;
                WebCamManager.Streams[cameraId].Start();

                var data = new
                {
                    cameraId,
                    cameraStreamStarted = true
                };

                serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
            }
            catch (Exception)
            {

                var data = new
                {
                    cameraId,
                    cameraStreamStarted = false
                };

                serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
            }
        }


        public void StopStream()
        {
            var cameraId = packet.args.First().ToString();
            try
            {
                
                var streamThread = WebCamManager.Streams[cameraId];
                if (streamThread != null)
                {
                    WebCamManager.Streams[cameraId].Abort();
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
            catch (Exception)
            {
                if (client.IsConnected)
                {

                    var data = new
                    {
                        cameraId,
                        cameraStreamStopped = false
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
                    var cameraHash = cameraId;
                    var imageBytes = WebCamManager.Frames[cameraHash];
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
                        exceptionMessage = e.Message
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