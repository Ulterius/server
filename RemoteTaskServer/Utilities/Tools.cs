using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace RemoteTaskServer.Utilities
{
    internal class Tools
    {
        /// <summary>
        ///     Gets the icon for a process by its path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetIconForProcess(string path)
        {
            var appIcon = Icon.ExtractAssociatedIcon(path);
            var ms = new MemoryStream();
            appIcon.ToBitmap().Save(ms, ImageFormat.Jpeg);
            var byteImage = ms.ToArray();
            var SigBase64 = Convert.ToBase64String(byteImage); //Get Base64
            return SigBase64;
        }
    }
}