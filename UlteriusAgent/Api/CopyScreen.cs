using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlteriusAgent.Api
{
    class CopyScreen
    {
        public static Bitmap CaptureDesktop()
        {
            var hDc = IntPtr.Zero;
            try
            {
                Bitmap bmp = null;
                {
                    try
                    {
                        SIZE size;
                        hDc = WinApi.GetDC(WinApi.GetDesktopWindow());
                        var hMemDc = Gdi.CreateCompatibleDC(hDc);

                        size.Cx = WinApi.GetSystemMetrics
                            (WinApi.SmCxscreen);

                        size.Cy = WinApi.GetSystemMetrics
                            (WinApi.SmCyscreen);

                        var hBitmap = Gdi.CreateCompatibleBitmap(hDc, size.Cx, size.Cy);

                        if (hBitmap != IntPtr.Zero)
                        {
                            var hOld = Gdi.SelectObject
                                (hMemDc, hBitmap);

                            Gdi.BitBlt(hMemDc, 0, 0, size.Cx, size.Cy, hDc,
                                0, 0, Gdi.Srccopy);

                            Gdi.SelectObject(hMemDc, hOld);
                            Gdi.DeleteDC(hMemDc);
                            bmp = Image.FromHbitmap(hBitmap);
                            Gdi.DeleteObject(hBitmap);
                            // GC.Collect();
                        }
                    }
                    finally
                    {
                        if (hDc != IntPtr.Zero)
                        {
                            WinApi.ReleaseDC(WinApi.GetDesktopWindow(), hDc);
                        }
                    }
                }
                return bmp;
            }
            catch (Exception)
            {
                if (hDc != IntPtr.Zero)
                {
                    WinApi.ReleaseDC(WinApi.GetDesktopWindow(), hDc);
                }
                return null;
            }
        }

        public struct SIZE
        {
            public int Cx;
            public int Cy;
        }

    }
}
