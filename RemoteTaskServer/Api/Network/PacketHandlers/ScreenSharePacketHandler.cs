#region

using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ionic.Zlib;
using UlteriusServer.Api.Network.Messages;
using UlteriusServer.Api.Services.LocalSystem;
using UlteriusServer.Api.Services.ScreenShare;
using UlteriusServer.Api.Win32.WindowsInput.Native;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.Api.Network.PacketHandlers
{
    internal class ScreenSharePacketHandler : PacketHandler
    {
        private readonly ScreenData _screenData = new ScreenData();
        private readonly Screen[] _screens = Screen.AllScreens;
        private readonly ScreenShareService _shareService = UlteriusApiServer.ScreenShareService;
        private AuthClient _authClient;
        private MessageBuilder _builder;
        private WebSocket _client;
        private Packet _packet;


        public void StopScreenShare()
        {
            try
            {
                var streamThread = ScreenShareService.Streams[_authClient];
                if (streamThread != null && !streamThread.IsCanceled && !streamThread.IsCompleted &&
                    streamThread.Status == TaskStatus.Running)
                {
                    streamThread.Dispose();


                    if (_client.IsConnected)
                    {
                        var data = new
                        {
                            streamStopped = true
                        };
                        _builder.WriteMessage(data);
                    }
                }
            }
            catch (Exception e)
            {
                if (_client.IsConnected)
                {
                    var data = new
                    {
                        streamStopped = false,
                        message = e.Message
                    };
                    _builder.WriteMessage(data);
                }
            }
        }

        public void CheckServer()
        {
        }

        public void StartScreenShare()
        {
            try
            {
                
                var screenStream = new Task(GetScreenFrame);
                ScreenShareService.Streams[_authClient] = screenStream;
                ScreenShareService.Streams[_authClient].Start();

                var data = new
                {
                    screenStreamStarted = true
                };
                _builder.WriteMessage(data);
            }
            catch (Exception exception)
            {
                var data = new
                {
                    cameraStreamStarted = false,
                    message = exception.Message
                };

                _builder.WriteMessage(data);
            }
        }

        private void GetScreenFrame()
        {
            _builder.Endpoint = "screensharedata";
            var bounds = Rectangle.Empty;
            while (_client != null && _client.IsConnected)
            {
                try
                {
                    var image = _screenData.LocalScreen(ref bounds);
                 
                    if (_screenData.NumByteFullScreen == 1)
                    {
                        _screenData.NumByteFullScreen = bounds.Width*bounds.Height*4;
                    }
                    if (bounds != Rectangle.Empty && image != null)
                    {
                        var data = _screenData.PackScreenCaptureData(image, bounds);
                        if (data != null && data.Length > 0)
                        {
                            _builder.WriteScreenFrame(data);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message + " " + e.StackTrace);
                }
            }
            Console.WriteLine("Screen Share Died");
        }

        public override void HandlePacket(Packet packet)
        {
            _client = packet.Client;
            _authClient = packet.AuthClient;
            _packet = packet;
            _builder = new MessageBuilder(_authClient, _client, _packet.EndPoint, _packet.SyncKey);
            switch (_packet.PacketType)
            {
                case PacketManager.PacketTypes.MouseDown:
                    HandleMouseDown();
                    break;
                case PacketManager.PacketTypes.MouseUp:
                    HandleMouseUp();
                    break;
                case PacketManager.PacketTypes.MouseScroll:
                    HandleScroll();
                    break;
                case PacketManager.PacketTypes.LeftDblClick:
                    break;
                case PacketManager.PacketTypes.KeyDown:
                    HandleKeyDown();
                    break;
                case PacketManager.PacketTypes.KeyUp:
                    HandleKeyUp();
                    break;
                case PacketManager.PacketTypes.FullFrame:
                    HandleFullFrame();
                    break;
                case PacketManager.PacketTypes.RightClick:
                    HandleRightClick();
                    break;
                case PacketManager.PacketTypes.MouseMove:
                    HandleMoveMouse();
                    break;
                case PacketManager.PacketTypes.CheckScreenShare:
                    CheckServer();
                    break;
                case PacketManager.PacketTypes.StartScreenShare:
                    StartScreenShare();
                    break;
                case PacketManager.PacketTypes.StopScreenShare:
                    StopScreenShare();
                    break;
            }
        }

        private void HandleFullFrame()
        {
            using (var ms = new MemoryStream())
            {
                _screenData.CaptureDesktop().Save(ms, ImageFormat.Jpeg);
                var imgData = ms.ToArray();
                var compressed = ZlibStream.CompressBuffer(imgData);
                var frameData = new
                {
                    Screen.PrimaryScreen.Bounds,
                    frameData = compressed
                };
                _builder.WriteMessage(frameData);
            }
        }

        private void HandleKeyUp()
        {
            var keyCodes = ((IEnumerable) _packet.Args[0]).Cast<object>()
                .Select(x => x.ToString())
                .ToList();
            var codes =
                keyCodes.Select(code => ToHex(int.Parse(code.ToString())))
                    .Select(hexString => Convert.ToInt32(hexString, 16))
                    .ToList();


            foreach (var code in codes)
            {
                var virtualKey = (VirtualKeyCode) code;
                _shareService.Simulator.Keyboard.KeyUp(virtualKey);
            }
        }


        private string ToHex(int value)
        {
            return $"0x{value:X}";
        }

        private void HandleKeyDown()
        {
            var keyCodes = ((IEnumerable) _packet.Args[0]).Cast<object>()
                .Select(x => x.ToString())
                .ToList();
            var codes =
                keyCodes.Select(code => ToHex(int.Parse(code.ToString())))
                    .Select(hexString => Convert.ToInt32(hexString, 16))
                    .ToList();
            foreach (var code in codes)
            {
                var virtualKey = (VirtualKeyCode) code;
                _shareService.Simulator.Keyboard.KeyDown(virtualKey);
            }
        }

        private void HandleScroll()
        {
            var delta = (int) _packet.Args[0];
            delta = ~delta;

            _shareService.Simulator.Mouse.VerticalScroll(delta);
        }

        private void HandleMoveMouse()
        {
            try
            {
                int y = Convert.ToInt16(_packet.Args[0], CultureInfo.InvariantCulture);
                int x = Convert.ToInt16(_packet.Args[1], CultureInfo.InvariantCulture);
                var device = _screens[0];
                if (x < 0 || x >= device.Bounds.Width || y < 0 || y >= device.Bounds.Height)
                {
                    return;
                }
                Cursor.Position = new Point(x, y);
            }
            catch
            {
                Console.WriteLine("Error moving mouse");
            }
        }

        private void HandleRightClick()
        {
            _shareService.Simulator.Mouse.RightButtonClick();
        }

        private void HandleMouseUp()
        {
            _shareService.Simulator.Mouse.LeftButtonUp();
        }

        private void HandleMouseDown()
        {
            _shareService.Simulator.Mouse.LeftButtonDown();
        }
    }
}