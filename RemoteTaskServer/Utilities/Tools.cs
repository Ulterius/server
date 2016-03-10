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
using NetFwTypeLib;
using RemoteTaskServer.WebServer;
using UlteriusServer.Plugins;
using UlteriusServer.TaskServer.Api.Controllers.Impl;
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
                            var pingReply = p.Send("8.8.8.8", 15000);
                            if (pingReply != null)
                                result =
                                    (pingReply.Status == IPStatus.Success) ||
                                    (p.Send("8.8.4.4", 15000)?.Status == IPStatus.Success) ||
                                    (p.Send("4.2.2.1", 15000)?.Status == IPStatus.Success);
                        }
                    }
                }
                catch
                {
                    // ignored
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
        const string INetFwPolicy2ProgID = "HNetCfg.FwPolicy2";
        const string INetFwRuleProgID = "HNetCfg.FWRule";


        private void ClosePort(string name)
        {
            INetFwPolicy2 firewallPolicy = getComObject<INetFwPolicy2>(INetFwPolicy2ProgID);
            firewallPolicy.Rules.Remove(name);
        }
        private static T getComObject<T>(string progID)
        {
            Type t = Type.GetTypeFromProgID(progID, true);
            return (T)Activator.CreateInstance(t);
        }
        private static void OpenPort(ushort port, string name)
        {
            INetFwRule2 firewallRule = getComObject<INetFwRule2>(INetFwRuleProgID);
            firewallRule.Description = name;
            firewallRule.Name = name;
            firewallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
            firewallRule.Enabled = true;
            firewallRule.InterfaceTypes = "All";
            firewallRule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
            firewallRule.LocalPorts = port.ToString();

            INetFwPolicy2 firewallPolicy = getComObject<INetFwPolicy2>(INetFwPolicy2ProgID);
            firewallPolicy.Rules.Add(firewallRule);
        }


        public static void GenerateSettings()
        {
            if (!File.Exists("UlteriusServer.ini"))
            {
                //setup listen sh
                var prefix = "http://*:22006/";
                var username = Environment.GetEnvironmentVariable("USERNAME");
                var userdomain = Environment.GetEnvironmentVariable("USERDOMAIN");
                var command = $@"/C netsh http add urlacl url={prefix} user={userdomain}\{username} listen=yes";
                System.Diagnostics.Process.Start("CMD.exe", command);
                OpenPort(22006, "Ulterius Web Server");
                OpenPort(22007, "Ulterius Task Server");
                OpenPort(22008, "Ulterius Terminal Server");
                OpenPort(5900, "VNC Server");
                OpenPort(5901, "Ulterius VNC Proxy Server");


                var settings = new Settings();
                //web server settings
                settings.Write("WebServer", "UseWebServer", true);
                settings.Write("WebServer", "WebServerPort", 22006);
                settings.Write("WebServer", "WebFilePath", HttpServer.defaultPath);
                //task server settings
                settings.Write("TaskServer", "TaskServerPort", 22007);
                //network settings
                settings.Write("Network", "SkipHostNameResolve", false);
                //plugin settings
                settings.Write("Plugins", "LoadPlugins", true);
                //vnc settings
                settings.Write("Vnc", "VncPass", "");
                settings.Write("Vnc", "VncPort", 5900);
                settings.Write("Vnc", "VncProxyPort", 5901);
                //terminal settings
                settings.Write("Terminal", "AllowTerminal", true);
            }
        }

        public static string GenerateAPIKey()
        {
            var res = "";
            var rnd = new Random();
            while (res.Length < 35)
                res += new Func<Random, string>(r =>
                {
                    var c = (char)(r.Next(123) * DateTime.Now.Millisecond % 123);
                    return char.IsLetterOrDigit(c) ? c.ToString() : "";
                })(rnd);
            return res;
        }

        public static bool IsAdmin()
        {
            return new WindowsPrincipal(GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static string GetQueryString(string url, string key)
        {
            var queryString = string.Empty;

            var uri = new Uri(url);
            var newQueryString = HttpUtility.ParseQueryString(uri.Query);
            if (newQueryString[key] != null)
            {
                queryString = newQueryString[key];
            }


            return queryString;
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