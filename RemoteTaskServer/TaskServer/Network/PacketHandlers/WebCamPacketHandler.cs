#region

using System;
using System.Threading.Tasks;
using UlteriusServer.TaskServer.Network.Messages;
using UlteriusServer.WebCams;
using UlteriusServer.WebSocketAPI.Authentication;

#endregion

namespace UlteriusServer.TaskServer.Network.PacketHandlers
{
    public class WebCamPacketHandler : PacketHandler
    {
        private MessageBuilder _builder;
        private AuthClient _client;
        private Packet _packet;


        public void RefreshCameras()
        {
            WebCamManager.LoadWebcams();
            var data = new
            {
                cameraFresh = true,
                message = "Camera have been refreshed!"
            };
            _builder.WriteMessage(data);
        }

        public void GetCameras()
        {
            var cameras = WebCamManager.GetCameras();
            var data = new
            {
                cameraInfo = cameras
            };
            _builder.WriteMessage(data);
        }


        public void StartCamera()
        {
            var cameraId = _packet.Args[0].ToString();
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
                _builder.WriteMessage(data);
            }
            catch (Exception)
            {
                var data = new
                {
                    cameraId,
                    cameraRunning = false,
                    cameraStarted = false
                };
                _builder.WriteMessage(data);
            }
        }

        public void StopCamera()
        {
            var cameraId = _packet.Args[0].ToString();
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
                _builder.WriteMessage(data);
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
                _builder.WriteMessage(data);
            }
        }

        public void PauseCamera()
        {
            var cameraId = _packet.Args[0].ToString();
            var cameraPaused = WebCamManager.PauseCamera(cameraId);
            var camera = WebCamManager.Cameras[cameraId];
            var data = new
            {
                cameraRunning = camera.IsRunning,
                cameraPaused
            };
            _builder.WriteMessage(data);
        }


        public void StartStream()
        {
            var cameraId = _packet.Args[0].ToString();
            try
            {
                var cameraStream = new Task(() => GetWebCamFrame(cameraId));
                WebCamManager.Streams[cameraId] = cameraStream;
                WebCamManager.Streams[cameraId].Start();
                var data = new
                {
                    cameraId,
                    cameraStreamStarted = true
                };
                _builder.WriteMessage(data);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                var data = new
                {
                    cameraId,
                    cameraStreamStarted = false
                };

                _builder.WriteMessage(data);
            }
        }


        public void StopStream()
        {
            var cameraId = _packet.Args[0].ToString();

            try
            {
                var streamThread = WebCamManager.Streams[cameraId];
                if (streamThread != null && !streamThread.IsCanceled && !streamThread.IsCompleted &&
                    streamThread.Status == TaskStatus.Running)
                {
                    streamThread.Dispose();
                    if (_client.Client.IsConnected)
                    {
                        var data = new
                        {
                            cameraId,
                            cameraStreamStopped = true
                        };
                        _builder.WriteMessage(data);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                if (_client.Client.IsConnected)
                {
                    var data = new
                    {
                        cameraId,
                        cameraStreamStopped = false
                    };
                    _builder.WriteMessage(data);
                }
            }
        }


        public void GetWebCamFrame(string cameraId)
        {
            var camera = WebCamManager.Cameras[cameraId];
            while (_client.Client.IsConnected && camera.IsRunning)
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
                        _builder.WriteMessage(data);
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
                    _builder.WriteMessage(data);
                }
            }
        }

        public override void HandlePacket(Packet packet)
        {
            _client = packet.AuthClient;
            _packet = packet;
            _builder = new MessageBuilder(_client, _packet.EndPoint, _packet.SyncKey);
            switch (_packet.PacketType)
            {
                case PacketManager.PacketTypes.StartCamera:
                    StartCamera();
                    break;
                case PacketManager.PacketTypes.StopCamera:
                    StopCamera();
                    break;
                case PacketManager.PacketTypes.PauseCamera:
                    PauseCamera();
                    break;
                case PacketManager.PacketTypes.StopCameraStream:
                    StopStream();
                    break;
                case PacketManager.PacketTypes.StartCameraStream:
                    StartStream();
                    break;
                case PacketManager.PacketTypes.GetCameras:
                    GetCameras();
                    break;
                case PacketManager.PacketTypes.GetCameraFrame:
                 
                    break;
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