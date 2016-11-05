#region

using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using WindowsInput.Native;
using Ionic.Zlib;
using UlteriusServer.Api.Network.Messages;
using UlteriusServer.Api.Services.LocalSystem;
using UlteriusServer.Api.Services.ScreenShare;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;
using Message = UlteriusServer.Api.Network.Messages.Message;

#endregion

namespace UlteriusServer.Api.Network.PacketHandlers
{
    internal class ScreenSharePacketHandler : PacketHandler
    {
        private readonly Screen[] _screens = Screen.AllScreens;
        private readonly ScreenShareService _shareService = UlteriusApiServer.ScreenShareService;
        private AuthClient _authClient;
        private MessageBuilder _builder;
        private WebSocket _client;
        private Packet _packet;
        [DllImport("Sas.dll", SetLastError = true)]
        public static extern void SendSAS(bool asUser);

        public void StopScreenShare()
        {
            try
            {
                _authClient.ShutDownScreenShare = true;
                Thread outtemp;
                if (!ScreenShareService.Streams.TryRemove(_authClient, out outtemp)) return;
                if (!UlteriusApiServer.RunningAsService)
                {
                    CleanUp();
                }
                if (!_client.IsConnected) return;
                var data = new
                {
                    streamStopped = true
                };
                _builder.WriteMessage(data);
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

        private void CleanUp()
        {
            var keyCodes = Enum.GetValues(typeof(VirtualKeyCode));
            //release all keys
            foreach (var keyCode in keyCodes)
            {
                if (_shareService.Simulator.InputDeviceState.IsKeyDown((VirtualKeyCode) keyCode))
                {
                    _shareService.Simulator.Keyboard.KeyUp((VirtualKeyCode) keyCode);
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
                if (ScreenShareService.Streams.ContainsKey(_authClient))
                {
                    var failData = new
                    {
                        cameraStreamStarted = false,
                        message = "Stream already created"
                    };
                    _builder.WriteMessage(failData);
                    return;
                }
                _authClient.ShutDownScreenShare = false;
                var stream = UlteriusApiServer.RunningAsService
                    ? new Thread(GetScreenAgentFrame) {IsBackground = true}
                    : new Thread(GetScreenFrame) {IsBackground = true};
                ScreenShareService.Streams[_authClient] = stream;
                var data = new
                {
                    screenStreamStarted = true
                };
                _builder.WriteMessage(data);
                ScreenShareService.Streams[_authClient].Start();
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


        public Bitmap GetImageFromByteArray(byte[] byteArray)
        {
            Bitmap newBitmap;
            using (var memoryStream = new MemoryStream(byteArray))
            using (var newImage = Image.FromStream(memoryStream))
                newBitmap = new Bitmap(newImage);
            return newBitmap;
        }

        private async void GetScreenAgentFrame()
        {
            var client = new TcpClient();
            StreamReader streamReader = null;
            StreamWriter streamWriter = null;
            await client.ConnectAsync(IPAddress.Loopback, 22005);
            var stream = client.GetStream();
            streamReader = new StreamReader(stream, Encoding.UTF8);
            streamWriter = new StreamWriter(stream, Encoding.UTF8);
            while (_client != null && _client.IsConnected && _authClient != null &&
                   !_authClient.ShutDownScreenShare)
            {
                try
                {
                    if (!client.Connected)
                    {
                        client = new TcpClient();
                        await client.ConnectAsync(IPAddress.Loopback, 22005);
                        stream = client.GetStream();
                        streamReader = new StreamReader(stream, Encoding.UTF8);
                        streamWriter = new StreamWriter(stream, Encoding.UTF8);
                    }
                    await streamWriter.WriteLineAsync("cleanframe");
                    await streamWriter.FlushAsync();
                    var base64 = await streamReader.ReadLineAsync();
                    if (base64 == null || base64.Length <= 1) continue;
                    var image = GetImageFromByteArray(Convert.FromBase64String(base64));
                    using (var screenData = ScreenData.LocalAgentScreen(image))
                    {
                        if (screenData.ScreenBitmap == null)

                        {
                            Thread.Sleep(250);
                            continue;
                        }
                        if (screenData.Rectangle == Rectangle.Empty) continue;
                        var data = ScreenData.PackScreenCaptureData(screenData.ScreenBitmap,
                            screenData.Rectangle);
                        if (data == null || data.Length <= 0) continue;
                        _builder.Endpoint = "screensharedata";
                        _builder.WriteScreenFrame(data);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Thread.Sleep(250);
                }
            }
            client?.Dispose();
            streamWriter?.Dispose();
            streamReader?.Dispose();
            Console.WriteLine("Screen Share Died");
        }

        private void GetScreenFrame()
        {
            while (_client != null && _client.IsConnected && _authClient != null &&
                   !_authClient.ShutDownScreenShare)
            {
                try
                {
                    using (var image = ScreenData.LocalScreen())
                    {
                        if (image == null)
                        {
                            Thread.Sleep(250);
                            continue;
                        }
                        if (image.Rectangle != Rectangle.Empty)
                        {
                            var data = ScreenData.PackScreenCaptureData(image.ScreenBitmap, image.Rectangle);
                            if (data != null && data.Length > 0)
                            {
                                _builder.Endpoint = "screensharedata";
                                _builder.WriteScreenFrame(data);

                                data = null;
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                            }
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
                    if (UlteriusApiServer.RunningAsService)
                    {
                        HandleAgentMouseDown();
                    }
                    else
                    {
                        HandleMouseDown();
                    }
                    break;
                case PacketManager.PacketTypes.MouseUp:
                    if (UlteriusApiServer.RunningAsService)
                    {
                        HandleAgentMouseUp();
                    }
                    else
                    {
                        HandleMouseUp();
                    }
                    break;
                case PacketManager.PacketTypes.CtrlAltDel:
                   
                        HandleCtrlAltDel();
         
                    break;
                case PacketManager.PacketTypes.MouseScroll:
                    if (UlteriusApiServer.RunningAsService)
                    {
                        HandleAgentMouseScroll();
                    }
                    else
                    {
                        HandleScroll();
                    }

                    break;
                case PacketManager.PacketTypes.LeftDblClick:
                    break;
                case PacketManager.PacketTypes.KeyDown:
                    if (UlteriusApiServer.RunningAsService)
                    {
                        HandleAgentKeyDown();
                    }
                    else
                    {
                        HandleKeyDown();
                    }

                    break;
                case PacketManager.PacketTypes.KeyUp:
                    if (UlteriusApiServer.RunningAsService)
                    {
                        HandleAgentKeyUp();
                    }
                    else
                    {
                        HandleKeyUp();
                    }
                    break;
                case PacketManager.PacketTypes.FullFrame:
                    if (UlteriusApiServer.RunningAsService)
                    {
                        HandleAgentFullFrame();
                    }
                    else
                    {
                        HandleFullFrame();
                    }
                    break;
                case PacketManager.PacketTypes.RightClick:
                    if (UlteriusApiServer.RunningAsService)
                    {
                        HandleAgentRightClick();
                    }
                    else
                    {
                        HandleRightClick();
                    }

                    break;
                case PacketManager.PacketTypes.MouseMove:
                    if (UlteriusApiServer.RunningAsService)
                    {
                        HandleAgentMouseMove();
                    }
                    else
                    {
                        HandleMoveMouse();
                    }
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

        private void HandleCtrlAltDel()
        {
           
            SendSAS(false);
        }

        private void HandleAgentRightClick()
        {
            if (!ScreenShareService.Streams.ContainsKey(_authClient)) return;
            var command = "rightclick";
            var message = new Message(command, Message.MessageType.Service);
            _authClient?.MessageQueueManagers[22005]?.SendQueue.Add(message);
        }

        private  void HandleAgentMouseScroll()
        {
            if (!ScreenShareService.Streams.ContainsKey(_authClient)) return;
            var delta = Convert.ToInt32(_packet.Args[0], CultureInfo.InvariantCulture);
            var arguments = delta + "," + "0";
            var command = "mousescroll|" + arguments;
            var message = new Message(command, Message.MessageType.Service);
            _authClient?.MessageQueueManagers[22005]?.SendQueue.Add(message);
        }

        private void HandleAgentMouseDown()
        {
            if (!ScreenShareService.Streams.ContainsKey(_authClient)) return;
            var command = "mousedown";
            var message = new Message(command, Message.MessageType.Service);
            _authClient?.MessageQueueManagers[22005]?.SendQueue.Add(message);
        }

        private void HandleAgentMouseUp()
        {
            if (!ScreenShareService.Streams.ContainsKey(_authClient)) return;
            var command = "mouseup";
            var message = new Message(command, Message.MessageType.Service);
            _authClient?.MessageQueueManagers[22005]?.SendQueue.Add(message);
        }

        private void HandleAgentMouseMove()
        {
            if (!ScreenShareService.Streams.ContainsKey(_authClient)) return;
            int y = Convert.ToInt16(_packet.Args[0], CultureInfo.InvariantCulture);
            int x = Convert.ToInt16(_packet.Args[1], CultureInfo.InvariantCulture);
            var command = "mousemove|" + $"{x},{y}";
            var message = new Message(command, Message.MessageType.Service);
            _authClient?.MessageQueueManagers[22005]?.SendQueue.Add(message);
        }

        private void HandleAgentKeyDown()
        {
            if (!ScreenShareService.Streams.ContainsKey(_authClient)) return;
            var keyCodes = ((IEnumerable) _packet.Args[0]).Cast<object>()
                .Select(x => x.ToString())
                .ToList();
            var codes =
                keyCodes.Select(code => ToHex(int.Parse(code.ToString())))
                    .Select(hexString => Convert.ToInt32(hexString, 16))
                    .ToList();
            var result = string.Join(",", codes);
            var command = "keydown|" + result;
            var message = new Message(command, Message.MessageType.Service);
            _authClient?.MessageQueueManagers[22005]?.SendQueue.Add(message);
        }

        private void HandleAgentKeyUp()
        {
            if (!ScreenShareService.Streams.ContainsKey(_authClient)) return;
            var keyCodes = ((IEnumerable) _packet.Args[0]).Cast<object>()
                .Select(x => x.ToString())
                .ToList();
            var codes =
                keyCodes.Select(code => ToHex(int.Parse(code.ToString())))
                    .Select(hexString => Convert.ToInt32(hexString, 16))
                    .ToList();
            var result = string.Join(",", codes);
            var command = "keyup|" + result;
            var message = new Message(command, Message.MessageType.Service);
            _authClient?.MessageQueueManagers[22005]?.SendQueue.Add(message);
        }


        private async void HandleAgentFullFrame()
        {
            try
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(IPAddress.Loopback, 22005);
                    using (var stream = client.GetStream())
                    using (var sr = new StreamReader(stream, Encoding.UTF8))
                    using (var sw = new StreamWriter(stream, Encoding.UTF8))
                    {
                        if (!client.Connected) throw new InvalidOperationException("Screen share agent is not connected");
                        await sw.WriteLineAsync("fullframe");
                        await sw.FlushAsync();
                        var base64 = await sr.ReadLineAsync();

                        if (base64 == null || base64.Length <= 1) throw new InvalidOperationException("Frame was null");
                        var data = Convert.FromBase64String(base64);
                        using (var ms = new MemoryStream(data))
                        using (var memoryReader = new BinaryReader(ms))
                        {
                            var bottom = memoryReader.ReadInt32();
                            var right = memoryReader.ReadInt32();
                            var imageLength = memoryReader.ReadInt32();
                            var image = memoryReader.ReadBytes(imageLength);
                            var screenBounds = new
                            {
                                bottom,
                                right
                            };
                            var frameData = new
                            {
                                screenBounds,
                                frameData = image.Select(b => (int) b).ToArray()
                            };
                            _builder.WriteMessage(frameData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var data = new
                {
                    frameFailed = true,
                    message = ex.Message
                };
                _builder.WriteMessage(data);
            }
        }

        private void HandleFullFrame()
        {
            using (var ms = new MemoryStream())
            {
                using (var grab = ScreenData.CaptureDesktop())
                {
                    grab.Save(ms, ImageFormat.Jpeg);
                    var imgData = ms.ToArray();
         
                    var bounds = Screen.PrimaryScreen.Bounds;
                    var screenBounds = new
                    {
                        top = bounds.Top,
                        bottom = bounds.Bottom,
                        left = bounds.Left,
                        right = bounds.Right,
                        height = bounds.Height,
                        width = bounds.Width,
                        x = bounds.X,
                        y = bounds.Y,
                        empty = bounds.IsEmpty,
                        location = bounds.Location,
                        size = bounds.Size
                    };
                    var frameData = new
                    {
                        screenBounds,
                        frameData = imgData.Select(b => (int) b).ToArray()
                    };
                    _builder.WriteMessage(frameData);
                }
            }
        }

        private void HandleKeyUp()
        {
            if (!ScreenShareService.Streams.ContainsKey(_authClient)) return;
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
            if (!ScreenShareService.Streams.ContainsKey(_authClient)) return;
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
            if (!ScreenShareService.Streams.ContainsKey(_authClient)) return;
            var delta = Convert.ToInt32(_packet.Args[0], CultureInfo.InvariantCulture);
            delta = ~delta;
            _shareService.Simulator.Mouse.VerticalScroll(delta);
        }

        private void HandleMoveMouse()
        {
            if (!ScreenShareService.Streams.ContainsKey(_authClient)) return;
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
            if (ScreenShareService.Streams.ContainsKey(_authClient))
            {
                _shareService.Simulator.Mouse.RightButtonClick();
            }
        }

        private void HandleMouseUp()
        {
            if (ScreenShareService.Streams.ContainsKey(_authClient))
            {
                _shareService.Simulator.Mouse.LeftButtonUp();
            }
        }

        private void HandleMouseDown()
        {
            if (ScreenShareService.Streams.ContainsKey(_authClient))
            {
                _shareService.Simulator.Mouse.LeftButtonDown();
            }
        }
    }
}