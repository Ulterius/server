#region

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UlteriusServer.TaskServer.Api.Serialization;
using UlteriusServer.WebCams;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers.Impl
{
    public class WebCamController : ApiController
    {
        private readonly WebSocket _client;
        private readonly Packets _packet;
        private readonly ApiSerializator _serializator = new ApiSerializator();


        public WebCamController(WebSocket client, Packets packet)
        {
            this._client = client;
            this._packet = packet;
        }

        public void RefreshCameras()
        {
            WebCamManager.LoadWebcams();
            var data = new
            {
                cameraFresh = true,
                message = "Camera have been refreshed!"
            };
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
        }

        public void GetCameras()
        {
            var cameras = WebCamManager.GetCameras();
            var data = new
            {
                cameraInfo = cameras
            };
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
        }


        public void StartCamera()
        {
            var cameraId = _packet.Args.First().ToString();
            try
            {
                var cameraStarted = WebCamManager.StartCamera(cameraId);
                var camera = WebCamManager.Cameras[cameraId];
                var data = new
                {
                    cameraId,
                    cameraRunning = camera.IsRunning,
                    cameraStarted
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
            }
            catch (Exception)
            {
                var data = new
                {
                    cameraId,
                    cameraRunning = false,
                    cameraStarted = false
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
            }
        }

        public void StopCamera()
        {
            var cameraId = _packet.Args.First().ToString();
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
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                var data = new
                {
                    cameraId,
                    cameraRunning = false,
                    cameraStarted = false
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
            }
        }

        public void PauseCamera()
        {
            var cameraId = _packet.Args.First().ToString();  
            var cameraPaused = WebCamManager.PauseCamera(cameraId);
            var camera = WebCamManager.Cameras[cameraId];
            var data = new
            {
                cameraRunning = camera.IsRunning,
                cameraPaused
            };
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
        }

        
        public void StartStream()
        {
            var cameraId = _packet.Args.First().ToString();
            try
            {
                Task cameraStream = new Task(() => GetWebCamFrame(cameraId));
                WebCamManager.Streams[cameraId] = cameraStream;
                WebCamManager.Streams[cameraId].Start();
                var data = new
                {
                    cameraId,
                    cameraStreamStarted = true
                };
                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                var data = new
                {
                    cameraId,
                    cameraStreamStarted = false
                };

                _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
            }
        }


        public void StopStream()
        {
            var cameraId = _packet.Args.First().ToString();
            
            try
            {
                var streamThread = WebCamManager.Streams[cameraId];
                if (streamThread != null && !streamThread.IsCanceled && !streamThread.IsCompleted && streamThread.Status == TaskStatus.Running)
                {
                    streamThread.Dispose();
                    if (_client.IsConnected)
                    {
                        var data = new
                        {
                            cameraId,
                            cameraStreamStopped = true
                        };
                        _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                if (_client.IsConnected)
                {
                    var data = new
                    {
                        cameraId,
                        cameraStreamStopped = false
                    };
                    _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
                }
            }
        }


        public void GetWebCamFrame(string cameraId)
        {
            var camera = WebCamManager.Cameras[cameraId];
            while (_client.IsConnected && camera.IsRunning)
            {
                try
                {
                    var cameraHash = cameraId;
                    var imageBytes = WebCamManager.Frames[cameraHash];
                    if (imageBytes.Length > 0)
                    {
                        var data = new
                        {
                            cameraId,
                            cameraFrame = imageBytes
                        };
                        _serializator.Serialize(_client, "getcameraframe", _packet.SyncKey, data);
                    }
                    else
                    {
                        break;
                    }
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
                    _serializator.Serialize(_client, "getcameraframe", _packet.SyncKey, data);
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