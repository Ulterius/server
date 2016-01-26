#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Threading;
using System.Web;
using UlteriusPlugins;
using UlteriusServer.Plugins;
using static System.Security.Principal.WindowsIdentity;

#endregion

namespace UlteriusServer.Utilities
{
    internal class Tools
    {
        public static bool HasInternetConnection
        {
            // There is no way you can reliably check if there is an internet connection, but we can come close
            get
            {
                var result = false;

                try
                {
                    if (NetworkInterface.GetIsNetworkAvailable())
                    {
                        using (var p = new Ping())
                        {
                            result =
                                (p.Send("8.8.8.8", 15000).Status == IPStatus.Success) ||
                                (p.Send("8.8.4.4", 15000).Status == IPStatus.Success) ||
                                (p.Send("4.2.2.1", 15000).Status == IPStatus.Success);
                        }
                    }
                }
                catch
                {
                }

                return result;
            }
        }

        public static void ShowNetworkTraffic()
        {
            var performanceCounterCategory = new PerformanceCounterCategory("Network Interface");
            var instance = performanceCounterCategory.GetInstanceNames()[0]; // 1st NIC !
            var performanceCounterSent = new PerformanceCounter("Network Interface", "Bytes Sent/sec", instance);
            var performanceCounterReceived = new PerformanceCounter("Network Interface", "Bytes Received/sec", instance);

            for (var i = 0; i < 10; i++)
            {
                Console.WriteLine("bytes sent: {0}k\tbytes received: {1}k", performanceCounterSent.NextValue()/1024,
                    performanceCounterReceived.NextValue()/1024);
                Thread.Sleep(500);
            }
        }

        public static void GenerateSettings()
        {
            if (!File.Exists("UlteriusServer.ini"))
            {
                var settings = new Settings();

                settings.Write("WebServer", "UseWebServer", false);
                settings.Write("WebServer", "WebServerPort", 9999);
                settings.Write("WebServer", "WebFilePath", "");
                settings.Write("TaskServer", "TaskServerPort", 8387);
                settings.Write("Network", "SkipHostNameResolve", false);
            }
        }

        public static bool IsAdmin()
        {
            return new WindowsPrincipal(GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static string GetQueryString(string url, string key)
        {
            var query_string = string.Empty;

            var uri = new Uri(url);
            var newQueryString = HttpUtility.ParseQueryString(uri.Query);
            if (newQueryString[key] != null)
            {
                query_string = newQueryString[key];
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