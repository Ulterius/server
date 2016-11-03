#region

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;
using UlteriusAgent.Api;

#endregion

namespace UlteriusAgent.Networking
{
    public class AgentServer
    {
        private static string LastDesktop;
        private static Desktop lastDesktopInput;

        public static void Start()
        {
            try
            {
                var cancellationTokenSource = new CancellationTokenSource();
                var cancel = cancellationTokenSource.Token;
                var listener = new TcpListener(IPAddress.Loopback, 22005);
                listener.Start();
                Console.WriteLine("Service listening at " + listener.LocalEndpoint);
                var task = AcceptClientsAsync(listener, cancel);
                Console.ReadLine();
                cancellationTokenSource.Cancel();
                task.Wait();
            }
            catch (SocketException ex)
            {
                Process.GetCurrentProcess().Kill();
                Process.GetCurrentProcess().WaitForExit();
            }
        }

        public static async Task AcceptClientsAsync(TcpListener listener, CancellationToken cancel)
        {
            await Task.Yield();
            while (!cancel.IsCancellationRequested)
            {
                try
                {
                    var timeoutTask = Task.Delay(2000);
                    var acceptTask = listener.AcceptTcpClientAsync();

                    await Task.WhenAny(timeoutTask, acceptTask);
                    if (!acceptTask.IsCompleted)
                        continue;

                    var client = await acceptTask;
                    HandleClientAsync(client, cancel);
                }
                catch (Exception aex)
                {
                    var ex = aex.GetBaseException();
                    Console.WriteLine("Accepting error: " + ex.Message);
                }
            }
        }

        public static byte[] ImageToByte(Image img, bool convertToJpeg = false)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, convertToJpeg ? ImageFormat.Jpeg : ImageFormat.Bmp);
                return stream.ToArray();
            }
        }

        private static byte[] SendCleanFrame()
        {
            var image = CopyScreen.CaptureDesktop();
            return image == null ? new byte[0] : ImageToByte(image, true);
        }

        
        private static byte[] SendFullFrame()
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memoryStream))
                {
                    var bounds = Screen.PrimaryScreen.Bounds;
                    writer.Write(bounds.Bottom);
                    writer.Write(bounds.Right);
                    var image = CopyScreen.CaptureDesktop();
                    //Okay the image is null
                    if (image == null)
                    {
                        //Lets try resetting the thread
                        var setCurrent = Desktop.SetCurrent(lastDesktopInput);
                        if (setCurrent)
                        {
                            //if the image is still null, send a frame so we don't break the whole screen share instance. 
                            image = CopyScreen.CaptureDesktop();
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
                        }
                    }
                    var imageBytes = ImageToByte(image, true);
                    writer.Write(imageBytes.Length);
                    writer.Write(imageBytes);
                    return memoryStream.ToArray();
                }
            }
        }

        public static async Task HandleClientAsync(TcpClient client, CancellationToken cancel)
        {
            await Task.Yield();
            StreamReader sr = null;
            StreamWriter sw = null;
            try
            {
                var stream = client.GetStream();
                sr = new StreamReader(stream, Encoding.UTF8);
                sw = new StreamWriter(stream, Encoding.UTF8);
                while (!cancel.IsCancellationRequested && client.Connected)
                {
                    var inputDesktop = new Desktop();
                    inputDesktop.OpenInput();
                    if (!inputDesktop.DesktopName.Equals(LastDesktop))
                    {
                        var switched = inputDesktop.Show();

                        if (switched)
                        {
                            var setCurrent = Desktop.SetCurrent(inputDesktop);
                            if (setCurrent)
                            {
                                Console.WriteLine(
                                    $"Desktop switched from {LastDesktop} to {inputDesktop.DesktopName}");
                                LastDesktop = inputDesktop.DesktopName;
                                lastDesktopInput = inputDesktop;
                            }
                            else
                            {
                                lastDesktopInput.Close();
                            }
                        }
                    }
                    else
                    {
                        inputDesktop.Close();
                    }
                    var endpoint = await sr.ReadLineAsync();
                    if (string.IsNullOrEmpty(endpoint))
                    {
                        break;
                    }
                    string[] endpointArgs = {};
                    if (endpoint.Contains("|"))
                    {
                        var splitPoint = endpoint.Split('|');
                        endpointArgs = splitPoint[1].Split(',');
                        endpoint = splitPoint[0];
                    }
                    string response = null;
                    switch (endpoint)
                    {
                        case "fullframe":
                            var frameData = SendFullFrame();
                            response = frameData.Length == 0 ? "n" : Convert.ToBase64String(frameData);
                            break;
                        case "cleanframe":
                            var cleanFrameData = SendCleanFrame();
                            response = cleanFrameData.Length == 0 ? "n" : Convert.ToBase64String(cleanFrameData);
                            break;
                        case "ctrlaltdel":
                            HandleCtrlAltDel();
                            break;
                        case "mousemove":
                            int x = Convert.ToInt16(endpointArgs[0], CultureInfo.InvariantCulture);
                            int y = Convert.ToInt16(endpointArgs[1], CultureInfo.InvariantCulture);
                            MoveMouse(x, y);
                            break;
                        case "mousedown":
                            HandleMouseDown();
                            break;
                        case "mousescroll":
                            HandleMouseScroll(endpointArgs[0]);
                            break;
                        case "mouseup":
                            HandleMouseUp();
                            break;
                        case "leftclick":
                            break;
                        case "rightclick":
                            HandleRightClick();
                            break;
                        case "keydown":
                            KeyDown(endpointArgs);
                            break;
                        case "keyup":
                            Keyup(endpointArgs);
                            break;
                    }
                    if (string.IsNullOrEmpty(response))
                    {
                        break;
                    }
                    await sw.WriteLineAsync(response);
                    await sw.FlushAsync();
                    await Task.Yield();
                }
            }
            catch (Exception aex)
            {
                var ex = aex.GetBaseException();
                Console.WriteLine("Client error: " + ex.Message);
            }
            finally
            {
                sr?.Dispose();

                sw?.Dispose();
            }
        }

        private static void HandleCtrlAltDel()
        {
            Desktop.SimulateCtrlAltDel();
        }

        private static void HandleMouseScroll(string deltaString)
        {
            var delta = Convert.ToInt32(deltaString, CultureInfo.InvariantCulture);
            delta = ~delta;
            var setCurrent = Desktop.SetCurrent(lastDesktopInput);
            if (setCurrent)
            {
                var inputSimualtor = new InputSimulator();
                inputSimualtor.Mouse.VerticalScroll(delta);
            }
        }

        private static void HandleRightClick()
        {
            var setCurrent = Desktop.SetCurrent(lastDesktopInput);
            if (setCurrent)
            {
                var inputSimualtor = new InputSimulator();
                inputSimualtor.Mouse.RightButtonClick();
            }
        }

        private static void HandleMouseUp()
        {
            var setCurrent = Desktop.SetCurrent(lastDesktopInput);
            if (setCurrent)
            {
                var inputSimualtor = new InputSimulator();
                inputSimualtor.Mouse.LeftButtonUp();
            }
        }

        private static void HandleMouseDown()
        {
            var setCurrent = Desktop.SetCurrent(lastDesktopInput);
            if (setCurrent)
            {
                var inputSimualtor = new InputSimulator();
                inputSimualtor.Mouse.LeftButtonDown();
            }
        }

        private static void MoveMouse(int x, int y)
        {
            var setCurrent = Desktop.SetCurrent(lastDesktopInput);
            if (setCurrent)
            {
                Cursor.Position = new Point(x, y);
            }
        }

        private static void Keyup(string[] endpointArgs)
        {
            try
            {
                var keycodes = Array.ConvertAll(endpointArgs, int.Parse);
                var inputSimualtor = new InputSimulator();
                foreach (var code in keycodes)
                {
                    var virtualKey = (VirtualKeyCode) code;
                    inputSimualtor.Keyboard.KeyUp(virtualKey);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void KeyDown(string[] endpointArgs)
        {
            try
            {
                var keycodes = Array.ConvertAll(endpointArgs, int.Parse);


                var inputSimualtor = new InputSimulator();
                foreach (var code in keycodes)
                {
                    var virtualKey = (VirtualKeyCode) code;
                    inputSimualtor.Keyboard.KeyDown(virtualKey);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


       
    }
}