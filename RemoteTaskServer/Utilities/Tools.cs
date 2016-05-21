#region

using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Threading;
using System.Web;
using NetFwTypeLib;
using RemoteTaskServer.WebServer;
using static System.Security.Principal.WindowsIdentity;

#endregion

namespace UlteriusServer.Utilities
{
    internal class Tools
    {
        private const string INetFwPolicy2ProgID = "HNetCfg.FwPolicy2";
        private const string INetFwRuleProgID = "HNetCfg.FWRule";

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


        private void ClosePort(string name)
        {
            var firewallPolicy = getComObject<INetFwPolicy2>(INetFwPolicy2ProgID);
            firewallPolicy.Rules.Remove(name);
        }

        private static T getComObject<T>(string progID)
        {
            var t = Type.GetTypeFromProgID(progID, true);
            return (T) Activator.CreateInstance(t);
        }

        private static void OpenPort(ushort port, string name)
        {
            var firewallRule = getComObject<INetFwRule2>(INetFwRuleProgID);
            firewallRule.Description = name;
            firewallRule.Name = name;
            firewallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
            firewallRule.Enabled = true;
            firewallRule.InterfaceTypes = "All";
            firewallRule.Protocol = (int) NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
            firewallRule.LocalPorts = port.ToString();

            var firewallPolicy = getComObject<INetFwPolicy2>(INetFwPolicy2ProgID);
            firewallPolicy.Rules.Add(firewallRule);
        }


        public static void ConfigureServer()
        {
            if (Settings.Empty)
            {
                //setup listen sh
                var prefix = "http://*:22006/";
                var username = Environment.GetEnvironmentVariable("USERNAME");
                var userdomain = Environment.GetEnvironmentVariable("USERDOMAIN");
                var command = $@"/C netsh http add urlacl url={prefix} user={userdomain}\{username} listen=yes";
                Process.Start("CMD.exe", command);
                OpenPort(22006, "Ulterius Web Server");
                OpenPort(22007, "Ulterius Task Server");
                OpenPort(22008, "Ulterius Terminal Server");
                OpenPort(5900, "VNC Server");
                OpenPort(5901, "Ulterius VNC Proxy Server");
                //web server settings
                Settings.Get()["WebServer"] = new Settings.Header
                {
                    {
                        "WebFilePath", HttpServer.DefaultPath
                    },
                    {
                        "WebServerPort", 22006
                    },
                    {
                        "UseWebServer", true
                    }
                };
                Settings.Get()["TaskServer"] = new Settings.Header
                {
                    {
                        "TaskServerPort", 22007
                    }
                };
                Settings.Get()["Network"] = new Settings.Header
                {
                    {
                        "SkipHostNameResolve", false
                    }
                };
                Settings.Get()["Plugins"] = new Settings.Header
                {
                    {
                        "LoadPlugins", true
                    }
                };
                Settings.Get()["Vnc"] = new Settings.Header
                {
                    {
                        "VncPass", string.Empty
                    },
                    {
                        "VncPort", 5900
                    },
                    {
                        "VncProxyPort", 5901
                    }
                };
                Settings.Get()["Terminal"] = new Settings.Header
                {
                    {
                        "AllowTerminal", true
                    }
                     };

                Settings.Save();
            }
        }

        public static string GenerateAPIKey()
        {
            var res = "";
            var rnd = new Random();
            while (res.Length < 35)
                res += new Func<Random, string>(r =>
                {
                    var c = (char) (r.Next(123)*DateTime.Now.Millisecond%123);
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
    }
}