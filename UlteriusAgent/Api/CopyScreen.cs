#region

using System.Drawing;
using System.Windows.Forms;

#endregion

namespace UlteriusAgent.Api
{
    internal class CopyScreen
    {
        public static Bitmap CaptureDesktop()
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

        public struct SIZE
        {
            public int Cx;
            public int Cy;
        }
    }
}