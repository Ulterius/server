#region

using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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
            var cameraStatus = WebCamManager.StartCamera(cameraId);
            var data = new
            {
                cameraStatus,
                cameraStarted = cameraStatus
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
        }

        public void StopCamera()
        {
            var cameraId = packet.args.First().ToString();
            var cameraStatus = WebCamManager.StopCamera(cameraId);
            var data = new
            {
                cameraStatus,
                cameraStopped = cameraStatus
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
        }

        public void PauseCamera()
        {
            var cameraId = packet.args.First().ToString();
            var cameraStatus = WebCamManager.PauseCamera(cameraId);
            var data = new
            {
                cameraStatus,
                cameraPaused = cameraStatus
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
        }


        public void GetWebCamFrame()
        {
            var cameraId = packet.args.First().ToString();
            try
            {
                var cameraHash = cameraId.GetHashCode().ToString();
                var imageBytes = WebCamManager._Frames[cameraHash];
                    var data = new
                    {
                        cameraId,
                        cameraFrame = imageBytes
                    };
                    serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
                
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
                serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
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