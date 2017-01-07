#region

using System;
using System.Collections;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using AgentInterface.Api.Models;
using AgentInterface.Api.ScreenShare;
using AgentInterface.Api.Win32;
using InputManager;
using Newtonsoft.Json;
using UlteriusServer.Api.Network.Messages;
using UlteriusServer.Api.Win32;
using UlteriusServer.Utilities.Settings;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;
using static UlteriusServer.Api.UlteriusApiServer;
using ScreenShareService = UlteriusServer.Api.Services.LocalSystem.ScreenShareService;

#endregion

namespace UlteriusServer.Api.Network.PacketHandlers
{
    internal class ScreenSharePacketHandler : PacketHandler
    {
        private AuthClient _authClient;
        private MessageBuilder _builder;
        private WebSocket _client;
        private long _lastLoopTime = Environment.TickCount;
        private Packet _packet;
        private int fps;
        private long lastFpsTime;
        private int lastLoopTime;

        [DllImport("Sas.dll", SetLastError = true)]
        public static extern void SendSAS(bool asUser);

        public void StopScreenShare()
        {
            try
            {
                _authClient.ShutDownScreenShare = true;
                Thread outtemp;
                if (!ScreenShareService.Streams.TryRemove(_authClient, out outtemp)) return;
                if (!RunningAsService)
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
            var keyCodes = Enum.GetValues(typeof(Keys));
            //release all keys
            foreach (var keyCode in keyCodes)
            {
                var code = (Keys) keyCode;
                if (WinApi.IsKeyDown(code))
                {
                    Keyboard.KeyUp(code);
                }
            }
            Mouse.ButtonUp(Mouse.MouseKeys.Left);
        }

        public void CheckServer()
        {
        }

        public void GetAvailableMonitors()
        {
            var activeDisplays = RunningAsService ? AgentClient.GetDisplayInformation() : Display.DisplayInformation();
            var selectedDisplay = ScreenData.ActiveDisplay;
            var data = new
            {
                activeDisplays,
                selectedDisplay
            };
            _builder.WriteMessage(data);
        }

        public void SetActiveMonitor()
        {
            if (_packet.Args.ElementAt(0) == null)
            {
                ScreenData.ActiveDisplay = 0;
            }
            ScreenData.ActiveDisplay = Convert.ToInt32(_packet.Args[0]);
            var activeDisplays = RunningAsService ? AgentClient.GetDisplayInformation() : Display.DisplayInformation();
            if (RunningAsService)
            {
                AgentClient.SetActiveMonitor(ScreenData.ActiveDisplay);
            }
            var data = new
            {
                selectedDisplay = ScreenData.ActiveDisplay,
                resolutionInformation = activeDisplays[ScreenData.ActiveDisplay].CurrentResolution
            };
            _builder.WriteMessage(data);
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
                var stream = RunningAsService
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


        private void GetScreenAgentFrame()
        {
            try
            {
                var targetFps = Config.Load().ScreenShareService.ScreenShareFps;

                var optimalTime = 1000000000/targetFps;

                while (_client != null && _client.IsConnected && _authClient != null &&
                       !_authClient.ShutDownScreenShare)
                {
                    var now = Environment.TickCount;
                    long updateLength = now - lastLoopTime;
                    lastLoopTime = now;
                    var delta = updateLength/(double) optimalTime;
                    lastFpsTime += updateLength;
                    fps++;
                    try
                    {
                        var cleanFrame = AgentClient.GetCleanFrame();
                        if (cleanFrame?.ScreenImage != null)
                        {
                            using (var screenData = ScreenData.LocalAgentScreen(cleanFrame.ScreenImage))
                            {
                                if (screenData.ScreenBitmap != null && screenData.Rectangle != Rectangle.Empty)
                                {
                                    var data = ScreenData.PackScreenCaptureData(screenData.ScreenBitmap,
                                        screenData.Rectangle);
                                    if (data != null && data.Length > 0)
                                    {
                                        _builder.Endpoint = "screensharedata";
                                        _builder.WriteScreenFrame(data);
                                    }
                                }
                            }
                        }
                        var time = (lastLoopTime - Environment.TickCount + optimalTime)/1000000;
                        Thread.Sleep(TimeSpan.FromMilliseconds(time));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Thread.Sleep(250);
                    }
                }
                Console.WriteLine("Screen Share Died");
            }
            catch (Exception)
            {
            }
        }

        private void GetScreenFrame()
        {
            var targetFps = Config.Load().ScreenShareService.ScreenShareFps;

            var optimalTime = 1000000000/targetFps;
            while (_client != null && _client.IsConnected && _authClient != null &&
                   !_authClient.ShutDownScreenShare)
            {
                var now = Environment.TickCount;
                long updateLength = now - lastLoopTime;
                lastLoopTime = now;
                var delta = updateLength/(double) optimalTime;
                lastFpsTime += updateLength;
                fps++;
                try
                {
                    using (var image = ScreenData.LocalAgentScreen(ScreenData.CaptureScreen()))
                    {
                        if (image != null && image.Rectangle != Rectangle.Empty)
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
                    var time = (lastLoopTime - Environment.TickCount + optimalTime)/1000000;
                    Thread.Sleep(TimeSpan.FromMilliseconds(time));
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
            _builder = new MessageBuilder(_authClient, _client, _packet.EndPointName, _packet.SyncKey);
            switch (_packet.EndPoint)
            {
                case PacketManager.EndPoints.MouseDown:
                    HandleMouseDown();
                    break;
                case PacketManager.EndPoints.MouseUp:
                    HandleMouseUp();
                    break;
                case PacketManager.EndPoints.CtrlAltDel:
                    HandleCtrlAltDel();
                    break;
                case PacketManager.EndPoints.MouseScroll:
                    HandleScroll();
                    break;
                case PacketManager.EndPoints.LeftDblClick:
                    break;
                case PacketManager.EndPoints.KeyDown:
                    HandleKeyDown();
                    break;
                case PacketManager.EndPoints.RightDown:
                    RightDown();
                    break;
                case PacketManager.EndPoints.RightUp:
                    RightUp();
                    break;
                case PacketManager.EndPoints.KeyUp:
                    HandleKeyUp();
                    break;
                case PacketManager.EndPoints.FullFrame:
                    if (RunningAsService)
                    {
                        HandleAgentFullFrame();
                    }
                    else
                    {
                        HandleFullFrame();
                    }
                    break;
                case PacketManager.EndPoints.RightClick:
                    HandleRightClick();
                    break;
                case PacketManager.EndPoints.SetActiveMonitor:
                    SetActiveMonitor();
                    break;
                case PacketManager.EndPoints.MouseMove:
                    HandleMoveMouse();
                    break;
                case PacketManager.EndPoints.CheckScreenShare:
                    CheckServer();
                    break;
                case PacketManager.EndPoints.StartScreenShare:
                    StartScreenShare();
                    break;
                case PacketManager.EndPoints.GetAvailableMonitors:
                    GetAvailableMonitors();
                    break;
                case PacketManager.EndPoints.StopScreenShare:
                    StopScreenShare();
                    break;
            }
        }

        private void HandleCtrlAltDel()
        {
            SendSAS(false);
        }

        private void RightUp()
        {
            if (!ScreenShareService.Streams.ContainsKey(_authClient)) return;
            if (RunningAsService)
            {
                AgentClient.HandleRightMouseUp();
            }
            else
            {
                Mouse.ButtonUp(Mouse.MouseKeys.Right);
            }
        }

        private void RightDown()
        {
            if (!ScreenShareService.Streams.ContainsKey(_authClient)) return;
            if (RunningAsService)
            {
                AgentClient.HandleRightMouseDown();
            }
            else
            {
                Mouse.ButtonDown(Mouse.MouseKeys.Right);
            }
        }

        private void HandleAgentFullFrame()
        {
            try
            {
                var fullFrameData = AgentClient.GetFullFrame();
                if (fullFrameData?.ScreenImage == null) throw new InvalidOperationException("Frame was null");
                var bounds = fullFrameData.Bounds;
                var image = ScreenData.ImageToByteArray(fullFrameData.ScreenImage);
                var frameData = new
                {
                    screenBounds = new
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
                    },
                    frameData = image.Select(b => (int) b).ToArray()
                };
                _builder.WriteMessage(frameData);
            }
            catch (Exception ex)
            {
                var data = new
                {
                    frameFailed = true,
                    message = ex.Message
                };
                Console.WriteLine(ex.Message + "Fuck");
                _builder.WriteMessage(data);
            }
        }

        private void HandleFullFrame()
        {
            using (var grab = ScreenData.CaptureScreen())
            {
                var imgData = ScreenData.ImageToByteArray(grab);
                var monitors = Display.DisplayInformation();
                Rectangle bounds;
                if (monitors.Count > 0 && monitors.ElementAt(ScreenData.ActiveDisplay) != null)
                {
                    var activeDisplay = monitors[ScreenData.ActiveDisplay];
                    bounds = new Rectangle
                    {
                        X = activeDisplay.CurrentResolution.X,
                        Y = activeDisplay.CurrentResolution.Y,
                        Width = activeDisplay.CurrentResolution.Width,
                        Height = activeDisplay.CurrentResolution.Height
                    };
                }
                else
                {
                    bounds = Display.GetWindowRectangle();
                }
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

            if (RunningAsService)
            {
                AgentClient.HandleKeyUp(codes);
            }
            else
            {
                foreach (var code in codes)
                {
                    var virtualKey = (Keys) code;
                    Keyboard.KeyUp(virtualKey);
                }
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
            if (RunningAsService)
            {
                AgentClient.HandleKeyDown(codes);
            }
            else
            {
                foreach (var code in codes)
                {
                    var virtualKey = (Keys) code;
                    Keyboard.KeyDown(virtualKey);
                }
            }
        }

        private void HandleScroll()
        {
            if (!ScreenShareService.Streams.ContainsKey(_authClient)) return;
            var delta = Convert.ToInt32(_packet.Args[0], CultureInfo.InvariantCulture);
            delta = ~delta;
            var positive = delta > 0;
            var direction = positive ? Mouse.ScrollDirection.Up : Mouse.ScrollDirection.Down;
            if (RunningAsService)
            {
                AgentClient.ScrollMouse(positive);
            }
            else
            {
                Mouse.Scroll(direction);
            }
        }
        private static Point Translate(Point point, Size from, Size to)
        {
            return new Point((point.X * to.Width) / from.Width, (point.Y * to.Height) / from.Height);
        }

        private void HandleMoveMouse()
        {
            if (!ScreenShareService.Streams.ContainsKey(_authClient)) return;
            try
            {
                int y = Convert.ToInt16(_packet.Args[0], CultureInfo.InvariantCulture);
                int x = Convert.ToInt16(_packet.Args[1], CultureInfo.InvariantCulture);
                if (RunningAsService)
                {
                    AgentClient.MoveMouse(x, y);
                }
                else
                {
                    Cursor.Position = new Point(x, y);
                }
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
                if (RunningAsService)
                {
                    AgentClient.HandleRightClick();
                }
                else
                {
                    Mouse.PressButton(Mouse.MouseKeys.Right);
                }
            }
        }

        private void HandleMouseUp()
        {
            if (ScreenShareService.Streams.ContainsKey(_authClient))
            {
                if (RunningAsService)
                {
                    AgentClient.HandleLeftMouseUp();
                }
                else
                {
                    Mouse.ButtonUp(Mouse.MouseKeys.Left);
                }
            }
        }

        private void HandleMouseDown()
        {
            if (ScreenShareService.Streams.ContainsKey(_authClient))
            {
                if (RunningAsService)
                {
                    AgentClient.HandleLeftMouseDown();
                }
                else
                {
                    Mouse.ButtonDown(Mouse.MouseKeys.Left);
                }
            }
        }
    }

  
}