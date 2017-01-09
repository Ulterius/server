#region

using System;
using System.Linq;
using System.Threading;
using UlteriusServer.Api.Network.Messages;
using UlteriusServer.WebCams;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;
using static UlteriusServer.WebCams.WebCamManager;

#endregion

namespace UlteriusServer.Api.Network.PacketHandlers
{
    public class WebCamPacketHandler : PacketHandler
    {
        private AuthClient _authClient;
        private MessageBuilder _builder;
        private WebSocket _client;
        private Packet _packet;


        public override void HandlePacket(Packet packet)
        {
            _client = packet.Client;
            _authClient = packet.AuthClient;
            _packet = packet;
            _builder = new MessageBuilder(_authClient, _client, _packet.EndPointName, _packet.SyncKey);
            switch (_packet.EndPoint)
            {
                case PacketManager.EndPoints.StartCamera:
                    StartCamera();
                    break;
                case PacketManager.EndPoints.StopCamera:
                    StopCamera();
                    break;
                case PacketManager.EndPoints.PauseCamera:
                    PauseCamera();
                    break;
                case PacketManager.EndPoints.StopCameraStream:
                    StopStream();
                    break;
                case PacketManager.EndPoints.StartCameraStream:
                    StartStream();
                    break;
                case PacketManager.EndPoints.GetCameras:
                    GetCameras();
                    break;
                case PacketManager.EndPoints.GetCameraFrame:
                    break;
            }
        }

        private void GetCameras()
        {
            var cameras = WebCamManager.GetCameras();
            var data = new
            {
                cameraInfo = cameras
            };
            _builder.WriteMessage(data);
        }

        private void StartStream()
        {
            var cameraId = _packet.Args[0].ToString();
            Camera camera;
            if (Cameras.TryGetValue(cameraId, out camera) && camera != null && !camera.StreamActive)
            {
                camera.StreamActive = true;
                Cameras[cameraId] = camera;
                var cameraStream = new Thread(() => GetWebCamFrame(cameraId));
                cameraStream.Start();
                var data = new
                {
                    cameraId,
                    cameraStreamStarted = true
                };
                _builder.WriteMessage(data);
            }   
            else
            {
                if (camera != null)
                {
                    var data = new
                    {
                        cameraId,
                        cameraStreamStarted = true
                    };
                    _builder.WriteMessage(data);
                }
            }
        }

        private void GetWebCamFrame(string cameraId)
        {

            Camera camera;
            if (!Cameras.TryGetValue(cameraId, out camera)) return;
            while (_client != null && _client.IsConnected && camera.StreamActive && camera.Physical.IsRunning)
            {
                byte[] imageBytes;
                if (CameraFrames.TryGetValue(cameraId, out imageBytes))
                {
                    var cameraData = new
                    {
                        cameraId,
                        cameraData = imageBytes.Select(b => (int)b).ToArray()
                    };
                    _builder.Endpoint = "cameraframe";
                    _builder.WriteMessage(cameraData);
                }
                Thread.Sleep(150);
            }
            camera.StreamActive = false;
        }

        private void StopStream()
        {
            var cameraId = _packet.Args[0].ToString();
            Camera camera;
            if (Cameras.TryGetValue(cameraId, out camera))
            {
                camera.StreamActive = false;
                Cameras[cameraId] = camera;
                WebCamManager.StopCamera(cameraId);
                var data = new
                {
                    cameraId,
                    cameraStreamStopped = true
                };
                _builder.WriteMessage(data);

            }
        }

        private void PauseCamera()
        {
            throw new NotImplementedException();
        }

        private void StopCamera()
        {
            var cameraId = _packet.Args[0].ToString();
            Camera camera;
            if (Cameras.TryGetValue(cameraId, out camera))
            {
                var cameraStopped = WebCamManager.StopCamera(cameraId);
                var data = new
                {
                    cameraId,
                    cameraRunning = camera.Physical.IsRunning,
                    cameraStopped
                };
                _builder.WriteMessage(data);
            }
        }

        private void StartCamera()
        {

            var cameraId = _packet.Args[0].ToString();
            Camera camera;
            if (Cameras.TryGetValue(cameraId, out camera))
            {
                var cameraStarted = WebCamManager.StartCamera(cameraId);
                var data = new
                {
                    cameraId,
                    cameraRunning = camera.Physical.IsRunning,
                    cameraStarted
                };
                _builder.WriteMessage(data);
            }
        }
    }
}