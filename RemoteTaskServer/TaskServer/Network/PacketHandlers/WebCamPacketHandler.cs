#region

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Ionic.Zlib;
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
            catch (Exception e)
            {
                var data = new
                {
                    cameraId,
                    cameraRunning = false,
                    cameraStarted = false,
                    message = e.Message
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
                var data = new
                {
                    cameraId,
                    cameraRunning = false,
                    cameraStarted = false,
                    message = e.Message
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
                Console.WriteLine("stream started for " + cameraId);
            }
            catch (Exception exception)
            {
                var data = new
                {
                    cameraId,
                    cameraStreamStarted = false,
                    message = exception.Message
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
                if (_client.Client.IsConnected)
                {
                    var data = new
                    {
                        cameraId,
                        cameraStreamStopped = false,
                        message = e.Message
                    };
                    _builder.WriteMessage(data);
                }
            }
        }


        public void GetWebCamFrame(string cameraId)
        {
            while (_client.Client.IsConnected)
            {
                try
                {
                    var camera = WebCamManager.Cameras[cameraId];
                    if (camera != null && camera.IsRunning)
                    {
                        var imageBytes = WebCamManager.Frames[cameraId];
                        if (imageBytes.Length > 0)
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                using (var binaryWriter = new BinaryWriter(memoryStream))
                                {
                                    var compressed = ZlibStream.CompressBuffer(imageBytes);
                                    binaryWriter.Write(compressed);
                                }

                                var cameraData = new
                                {
                                    cameraId,
                                    cameraData = memoryStream.ToArray()
                                };
                                _builder.Endpoint = "cameraframe";
                                _builder.WriteMessage(cameraData);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    var data = new
                    {
                        cameraFrameFailed = true,
                        cameraId,
                        message = "Something went wrong and we were unable to get a frame from this camera!",
                        exceptionMessage = e.Message
                    };
                    _builder.WriteMessage(data);
                    Thread.Sleep(2500);
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