#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;
using Newtonsoft.Json.Linq;

#endregion

namespace UlteriusServer.Windows.Api
{
    internal class WindowsApi
    {
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

        [DllImport("shell32.dll", EntryPoint = "#261",
            CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void GetUserTilePath(
            string username,
            uint whatever, // 0x80000000
            StringBuilder picpath, int maxLength);

        private static string GetUserTilePath(string username)
        {
            // username: use null for current user
            var sb = new StringBuilder(1000);
            GetUserTilePath(username, 0x80000000, sb, sb.Capacity);
            return sb.ToString();
        }

        private static string GetUsername()
        {
            return Environment.UserName;
        }

        public static string GetWindowsInformation()
        {
            return JObject.FromObject(new
            {
                endpoint = "getWindowsData",
                avatar = GetUserAvatar(),
                username = GetUsername()
            }).ToString();
        }

        public static string VerifyPassword(string password)
        {
            var valid = false;
            using (var context = new PrincipalContext(ContextType.Machine))
            {
                valid = context.ValidateCredentials(GetUsername(), password);
            }
            var windowsLoginData = new
            {
                validLogin = valid,
                message = valid ? "Login was successfull" : "Login was unsuccessful"
            };
            return JObject.FromObject(new
            {
                endpoint = "verifyPassword",
               results = windowsLoginData
            }).ToString();
        }

        private static bool AllOneColor(Bitmap bmp)
        {
            var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly,
                PixelFormat.Format8bppIndexed);
            var rgbValues = new byte[bmpData.Stride*bmpData.Height];
            Marshal.Copy(bmpData.Scan0, rgbValues, 0, rgbValues.Length);
            bmp.UnlockBits(bmpData);
            return !rgbValues.Where((v, i) => i%bmpData.Stride < bmp.Width && v != rgbValues[0]).Any();
        }

        /// <summary>
        ///     Experimental function for monitoring active windows on your remote desktop (windows).
        /// </summary>
        /// <returns></returns>
        public static string GetActiveWindowsImages()
        {
            var activeWindows = new List<WindowsImages>();
            foreach (var process in Process.GetProcesses())
            {
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    RECT rc;
                    GetWindowRect(process.MainWindowHandle, out rc);
                    if (rc.Width > 0)
                    {
                        var bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
                        var gfxBmp = Graphics.FromImage(bmp);
                        var hdcBitmap = gfxBmp.GetHdc();

                        PrintWindow(process.MainWindowHandle, hdcBitmap, 0);

                        gfxBmp.ReleaseHdc(hdcBitmap);
                        gfxBmp.Dispose();
                        var ms = new MemoryStream();
                        bmp.Save(ms, ImageFormat.Png);
                        var byteImage = ms.ToArray();
                        var base64Window = Convert.ToBase64String(byteImage); //Get Base64
                        var image = new WindowsImages
                        {
                            imageData = base64Window,
                            windowName = process.ProcessName
                        };
                        if (!AllOneColor(bmp))
                        {
                            activeWindows.Add(image);
                        }
                    }
                }
            }
            return new JavaScriptSerializer().Serialize(new
            {
                endpoint = "getActiveWindowsSnapshots",
                results = activeWindows
            });
        }

        private static string GetUserAvatar()
        {
            var avatar = GetUserTile(null);
            var ms = new MemoryStream();
            avatar.Save(ms, ImageFormat.Png);
            var arr = new byte[ms.Length];

            ms.Position = 0;
            ms.Read(arr, 0, (int) ms.Length);
            ms.Close();

            var strBase64 = Convert.ToBase64String(arr);

            return strBase64;
        }

        private static Image GetUserTile(string username)
        {
            return Image.FromFile(GetUserTilePath(username));
        }
    }

    #region

    public class WindowsImages
    {
        public string imageData;
        public string windowName;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public RECT(RECT Rectangle) : this(Rectangle.Left, Rectangle.Top, Rectangle.Right, Rectangle.Bottom)
        {
        }

        public RECT(int Left, int Top, int Right, int Bottom)
        {
            X = Left;
            Y = Top;
            this.Right = Right;
            this.Bottom = Bottom;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int Left
        {
            get { return X; }
            set { X = value; }
        }

        public int Top
        {
            get { return Y; }
            set { Y = value; }
        }

        public int Right { get; set; }

        public int Bottom { get; set; }

        public int Height
        {
            get { return Bottom - Y; }
            set { Bottom = value + Y; }
        }

        public int Width
        {
            get { return Right - X; }
            set { Right = value + X; }
        }

        public Point Location
        {
            get { return new Point(Left, Top); }
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        public Size Size
        {
            get { return new Size(Width, Height); }
            set
            {
                Right = value.Width + X;
                Bottom = value.Height + Y;
            }
        }

        public static implicit operator Rectangle(RECT Rectangle)
        {
            return new Rectangle(Rectangle.Left, Rectangle.Top, Rectangle.Width, Rectangle.Height);
        }

        public static implicit operator RECT(Rectangle Rectangle)
        {
            return new RECT(Rectangle.Left, Rectangle.Top, Rectangle.Right, Rectangle.Bottom);
        }

        public static bool operator ==(RECT Rectangle1, RECT Rectangle2)
        {
            return Rectangle1.Equals(Rectangle2);
        }

        public static bool operator !=(RECT Rectangle1, RECT Rectangle2)
        {
            return !Rectangle1.Equals(Rectangle2);
        }

        public override string ToString()
        {
            return "{Left: " + X + "; " + "Top: " + Y + "; Right: " + Right + "; Bottom: " + Bottom + "}";
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public bool Equals(RECT Rectangle)
        {
            return Rectangle.Left == X && Rectangle.Top == Y && Rectangle.Right == Right && Rectangle.Bottom == Bottom;
        }

        public override bool Equals(object Object)
        {
            if (Object is RECT)
            {
                return Equals((RECT) Object);
            }
            if (Object is Rectangle)
            {
                return Equals(new RECT((Rectangle) Object));
            }

            return false;
        }
    }

    #endregion
}