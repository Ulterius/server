using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.ServiceModel;
using System.Windows.Forms;
using AgentInterface;
using AgentInterface.Api.Models;
using AgentInterface.Api.Win32;
using InputManager;
using OpenHardwareMonitor.Hardware;
using UlteriusAgent.Api;

namespace UlteriusAgent.Networking
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class ServerAgent : ITUlteriusContract
    {


        private  byte[] ImageToByte(Image img, bool convertToJpeg = false)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, convertToJpeg ? ImageFormat.Jpeg : ImageFormat.Bmp);
                return stream.ToArray();
            }
        }


        private  Bitmap CaptureDesktop()
        {
            var desktopBmp = new Bitmap(
                Screen.PrimaryScreen.Bounds.Width,
                Screen.PrimaryScreen.Bounds.Height);

            var g = Graphics.FromImage(desktopBmp);

            g.CopyFromScreen(0, 0, 0, 0,
                new Size(
                    Screen.PrimaryScreen.Bounds.Width,
                    Screen.PrimaryScreen.Bounds.Height));
            g.Dispose();
            return desktopBmp;
        }

        public ServerAgent()
        {
            var inputDesktop = new Desktop();
            inputDesktop.OpenInput();
            if (inputDesktop.DesktopName.Equals(_lastDesktop)) return;
            var switched = inputDesktop.Show();
            if (!switched) return;
            var setCurrent = Desktop.SetCurrent(inputDesktop);
            if (setCurrent)
            {
                Console.WriteLine(
                    $"Desktop switched from {_lastDesktop} to {inputDesktop.DesktopName}");
                _lastDesktop = inputDesktop.DesktopName;
                _lastDesktopInput = inputDesktop;
            }
            else
            {
                _lastDesktopInput.Close();
            }
        }
        private string _lastDesktop;
        private Desktop _lastDesktopInput;

        public byte[] GetCleanFrame()
        {
            HandleDesktop();
            var setCurrent = Desktop.SetCurrent(_lastDesktopInput);
            if (!setCurrent) return null;
            var image = CaptureDesktop();
            return image == null ? new byte[0] : ImageToByte(image, true);
        }

        public byte[] GetFullFrame()
        {
            HandleDesktop();
            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memoryStream))
                {
                    var bounds = Screen.PrimaryScreen.Bounds;
                    writer.Write(bounds.Bottom);
                    writer.Write(bounds.Right);
                    var setCurrent = Desktop.SetCurrent(_lastDesktopInput);
                    if (!setCurrent) return null;
                    var image = CaptureDesktop();
                    if (image == null)
                    {
                        var bmp = new Bitmap(bounds.Width, bounds.Height);
                        using (var gfx = Graphics.FromImage(bmp))
                        using (var brush = new SolidBrush(Color.FromArgb(67, 75, 99)))
                        {
                            gfx.FillRectangle(brush, 0, 0, bounds.Width, bounds.Height);
                        }
                        image = bmp;

                    }
                    var imageBytes = ImageToByte(image, true);
                    writer.Write(imageBytes.Length);
                    writer.Write(imageBytes);
                    return memoryStream.ToArray();
                }
            }
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
                        Console.WriteLine(
                            $"Desktop switched from {_lastDesktop} to {inputDesktop.DesktopName}");
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

        public bool KeepAlive()
        {
            return true;
        }

      

        public void HandleRightMouseDown()
        {
            var setCurrent = Desktop.SetCurrent(_lastDesktopInput);
            if (setCurrent)
            {
                Mouse.ButtonDown(Mouse.MouseKeys.Right);
            }
        }

        public void HandleRightMouseUp()
        {
            var setCurrent = Desktop.SetCurrent(_lastDesktopInput);
            if (setCurrent)
            {
                Mouse.ButtonUp(Mouse.MouseKeys.Right);
            }
        }

        public void MoveMouse(int x, int y)
        {
            var setCurrent = Desktop.SetCurrent(_lastDesktopInput);
            if (setCurrent)
            {
                Cursor.Position = new Point(x, y);
            }
        }

        public void MouseScroll(bool positive)
        {
            var direction = positive ? Mouse.ScrollDirection.Up : Mouse.ScrollDirection.Down;
            Mouse.Scroll(direction);
        }

      

        public void HandleLeftMouseDown()
        {
            var setCurrent = Desktop.SetCurrent(_lastDesktopInput);
            if (setCurrent)
            {
                Mouse.ButtonDown(Mouse.MouseKeys.Left);
            }
        }

        public void HandleLeftMouseUp()
        {
            var setCurrent = Desktop.SetCurrent(_lastDesktopInput);
            if (setCurrent)
            {
                Mouse.ButtonUp(Mouse.MouseKeys.Left);
            }
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

        public void HandleRightClick()
        {
            var setCurrent = Desktop.SetCurrent(_lastDesktopInput);
            if (setCurrent)
            {
                Mouse.PressButton(Mouse.MouseKeys.Right);
            }
        }
        [HandleProcessCorruptedStateExceptions]
        public float GetGpuTemp(string gpuName)
        {
            try
            {
                var myComputer = new Computer();
                myComputer.Open();
                //possible fix for gpu temps on laptops
                myComputer.GPUEnabled = true;
                float temp = -1;
                foreach (var hardwareItem in myComputer.Hardware)
                {
                    hardwareItem.Update();
                    switch (hardwareItem.HardwareType)
                    {
                        case HardwareType.GpuNvidia:
                            foreach (
                                var sensor in
                                    hardwareItem.Sensors.Where(
                                        sensor =>
                                            sensor.SensorType == SensorType.Temperature &&
                                            hardwareItem.Name.Contains(gpuName)))
                            {
                                if (sensor.Value != null)
                                {
                                    temp = (float)sensor.Value;
                                }
                            }
                            break;
                        case HardwareType.GpuAti:
                            foreach (
                                var sensor in
                                    hardwareItem.Sensors.Where(
                                        sensor =>
                                            sensor.SensorType == SensorType.Temperature &&
                                            hardwareItem.Name.Contains(gpuName)))
                            {
                                if (sensor.Value != null)
                                {
                                    temp = (float)sensor.Value;
                                }
                            }
                            break;
                    }
                }
                myComputer.Close();
                return temp;
            }
            catch (System.AccessViolationException)
            {
                return -1;
            }
        }

        public List<DisplayInformation> GetDisplayInformation()
        {
            return Display.DisplayInformation();
        }
    }
}
