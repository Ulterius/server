#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.ServiceModel;
using System.Windows.Forms;
using AgentInterface;
using AgentInterface.Api.Models;
using AgentInterface.Api.ScreenShare;
using AgentInterface.Api.System;
using AgentInterface.Api.Win32;
using InputManager;

#endregion

namespace UlteriusAgent.Networking
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class ServerAgent : ITUlteriusContract
    {
        private string _lastDesktop;
        private Desktop _lastDesktopInput;


        public FrameInformation GetCleanFrame()
        {
            HandleDesktop();
            var setCurrent = Desktop.SetCurrent(_lastDesktopInput);
            return !setCurrent ? null : ScreenData.DesktopCapture();
        }

        public FrameInformation GetFullFrame()
        {
            HandleDesktop();
            var monitors = Display.DisplayInformation();
            Rectangle tempBounds;
            if (monitors.Count > 0 && monitors.ElementAt(ScreenData.ActiveDisplay) != null)
            {
                var activeDisplay = monitors[ScreenData.ActiveDisplay];
                tempBounds = new Rectangle
                {
                    X = activeDisplay.CurrentResolution.X,
                    Y = activeDisplay.CurrentResolution.Y,
                    Width = activeDisplay.CurrentResolution.Width,
                    Height = activeDisplay.CurrentResolution.Height
                };
            }
            else
            {
                tempBounds = Display.GetWindowRectangle();
            }
            var frameInfo = new FrameInformation
            {
                Bounds = tempBounds
            };
            var setCurrent = Desktop.SetCurrent(_lastDesktopInput);
            if (!setCurrent) return null;
            frameInfo.ScreenImage = ScreenData.CaptureActiveScreen(ScreenData.ActiveDisplay);
            if (frameInfo.ScreenImage == null)
            {
                var bmp = new Bitmap(frameInfo.Bounds.Width, frameInfo.Bounds.Height);
                using (var gfx = Graphics.FromImage(bmp))
                using (var brush = new SolidBrush(Color.FromArgb(67, 75, 99)))
                {
                    gfx.FillRectangle(brush, 0, 0, frameInfo.Bounds.Width, frameInfo.Bounds.Height);
                }
                frameInfo.ScreenImage = bmp;
            }
            return frameInfo;
        }

        public bool KeepAlive()
        {
            return true;
        }


        public void HandleRightMouseDown()
        {
            Mouse.ButtonDown(Mouse.MouseKeys.Right);
        }

        public void HandleRightMouseUp()
        {
            Mouse.ButtonUp(Mouse.MouseKeys.Right);
        }


        public void MoveMouse(int x, int y)
        {
            Cursor.Position = new Point(x, y);
        }

        public void MouseScroll(bool positive)
        {
            var direction = positive ? Mouse.ScrollDirection.Up : Mouse.ScrollDirection.Down;
            Mouse.Scroll(direction);
        }


        public void HandleLeftMouseDown()
        {
            Mouse.ButtonDown(Mouse.MouseKeys.Left);
        }

        public void HandleLeftMouseUp()
        {
            Mouse.ButtonUp(Mouse.MouseKeys.Left);
        }

        public void HandleKeyDown(List<int> keyCodes)
        {
            foreach (var code in keyCodes)
            {
                var virtualKey = (Keys)code;
                Keyboard.KeyDown(virtualKey);
            }
        }

        public void HandleKeyUp(List<int> keyCodes)
        {
            foreach (var code in keyCodes)
            {
                var virtualKey = (Keys)code;
                Keyboard.KeyUp(virtualKey);
            }
        }

        public void SetActiveMonitor(int index)
        {
        }

        public void HandleRightClick()
        {
            Mouse.PressButton(Mouse.MouseKeys.Right);
        }

        [HandleProcessCorruptedStateExceptions]
        public float GetGpuTemp(string gpuName)
        {
            return SystemData.GetGpuTemp(gpuName);
        }

        public List<DisplayInformation> GetDisplayInformation()
        {
            return Display.DisplayInformation();
        }


        public List<float> GetCpuTemps()
        {
            return SystemData.GetCpuTemps();
        }

        private void HandleDesktop()
        {
            var inputDesktop = new Desktop();
            inputDesktop.OpenInput();
            if (!inputDesktop.DesktopName.Equals(_lastDesktop))
            {
                var switched = inputDesktop.Show();

                if (switched)
                {
                    var setCurrent = Desktop.SetCurrent(inputDesktop);
                    if (setCurrent)
                    {
                        Console.WriteLine($"Desktop switched from {_lastDesktop} to {inputDesktop.DesktopName}");
                        _lastDesktop = inputDesktop.DesktopName;
                        _lastDesktopInput = inputDesktop;
                    }
                    else
                    {
                        _lastDesktopInput.Close();
                    }
                }
            }
            else
            {
                inputDesktop.Close();
            }
        }
    }
}