using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace RemoteTaskServer.Utilities
{
    internal class Tools
    {

        public static string GenerateAPIKey()
        {
            string res = "";
            Random rnd = new Random();
            while (res.Length < 35) res += (new Func<Random, string>((r) => {
                char c = (char)((r.Next(123) * DateTime.Now.Millisecond % 123));
                return (Char.IsLetterOrDigit(c)) ? c.ToString() : "";
            }))(rnd);
            return res;
        }
        public static string GetQueryString(string url, string key)
        {
            string query_string = string.Empty;

            var uri = new Uri(url);
            var newQueryString = HttpUtility.ParseQueryString(uri.Query);
            if (newQueryString[key] != null)
            {
                query_string = newQueryString[key].ToString();
            }


            return query_string;
        }

        /// <summary>
        ///     Gets the icon for a process by its path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetIconForProcess(string path)
        {
            var appIcon = Icon.ExtractAssociatedIcon(path);
            var ms = new MemoryStream();
            appIcon.ToBitmap().Save(ms, ImageFormat.Png);
            var byteImage = ms.ToArray();
            var SigBase64 = Convert.ToBase64String(byteImage); //Get Base64
            return SigBase64;
        }
    }
}