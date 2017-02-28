#region

using System;
using System.Drawing;
using System.ServiceModel;
using AgentInterface;
using AgentInterface.Api.Models;
using AgentInterface.Api.ScreenShare;
using AgentInterface.Api.Win32;


#endregion

namespace UlteriusAgent.Networking
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class FrameAgent : IFrameContract
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
            var tempBounds = Display.GetWindowRectangle();
            var frameInfo = new FrameInformation
            {
                Bounds = tempBounds,
                ScreenImage = ScreenData.CaptureDesktop()
            };
            if (frameInfo.ScreenImage != null) return frameInfo;
            var bmp = new Bitmap(frameInfo.Bounds.Width, frameInfo.Bounds.Height);
            using (var gfx = Graphics.FromImage(bmp))
            using (var brush = new SolidBrush(Color.FromArgb(67, 75, 99)))
            {
                gfx.FillRectangle(brush, 0, 0, frameInfo.Bounds.Width, frameInfo.Bounds.Height);
            }
            frameInfo.ScreenImage = bmp;
            return frameInfo;
        }

        public bool KeepAlive()
        {
            return true;
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