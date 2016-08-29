#region

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Ionic.Zip;
using Microsoft.Win32;
using NetFwTypeLib;
using Open.Nat;
using UlteriusServer.WebServer;
using static System.Security.Principal.WindowsIdentity;

#endregion

namespace UlteriusServer.Utilities
{
    internal class Tools
    {
        private const string NetFwPolicy2ProgId = "HNetCfg.FwPolicy2";
        private const string NetFwRuleProgId = "HNetCfg.FWRule";

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


        private static T GetComObject<T>(string progId)
        {
            var t = Type.GetTypeFromProgID(progId, true);
            return (T) Activator.CreateInstance(t);
        }

        private static void OpenFirewall(ushort port, string name)
        {
            try
            {
                var firewallPolicy = GetComObject<INetFwPolicy2>(NetFwPolicy2ProgId);
                var firewallRule = GetComObject<INetFwRule2>(NetFwRuleProgId);
                var existingRule = firewallPolicy.Rules.OfType<INetFwRule>().FirstOrDefault(x => x.Name == name);
                if (existingRule == null)
                {
                    firewallRule.Description = name;
                    firewallRule.Name = name;
                    firewallRule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
                    firewallRule.Enabled = true;
                    firewallRule.InterfaceTypes = "All";
                    firewallRule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
                    firewallRule.LocalPorts = port.ToString();
                    firewallPolicy.Rules.Add(firewallRule);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }


        public static void GenerateSettings()
        {
            //web server settings
            Settings.Get()["General"] = new Settings.Header
            {
                {
                    "Version", Assembly.GetExecutingAssembly().GetName().Version
                },
                {
                    "RunStartup", true
                },
                {
                    "UploadLogs", false
                },
                {
                    "Github", "https://github.com/Ulterius"
                },
                {
                    "ServerIssues", "https://github.com/Ulterius/server/issues"
                },
                {
                    "ClientIssues", "https://github.com/Ulterius/client/issues"
                },
                {
                    //this is kind of nasty 
                    "Maintainers", new[]
                    {
                        new
                        {
                            Name = "Andrew Sampson",
                            Twitter = "https://twitter.com/Andrewmd5",
                            Github = "https://github.com/codeusa",
                            Website = "https://andrew.im/"
                        },
                        new
                        {
                            Name = "Evan Banyash",
                            Twitter = "https://twitter.com/frobthebuilder",
                            Github = "https://github.com/FrobtheBuilder",
                            Website = "http://banyash.com/"
                        }
                    }
                }
            };
            Settings.Get()["WebServer"] = new Settings.Header
            {
                {
                    "WebFilePath", HttpServer.DefaultPath
                },
                {
                    "WebServerPort", 22006
                },
                {
                    "ToggleWebServer", true
                }
            };
            Settings.Get()["TaskServer"] = new Settings.Header
            {
                {
                    "TaskServerPort", 22007
                },
                {
                    "Encryption", true
                }
            };
            Settings.Get()["Network"] = new Settings.Header
            {
                {
                    "SkipHostNameResolve", false
                },
                {
                    "UPnpEnabled", true
                },
                {
                    "BindLocal", false
                }
            };
            Settings.Get()["Plugins"] = new Settings.Header
            {
                {
                    "LoadPlugins", true
                }
            };
            Settings.Get()["ScreenShareService"] = new Settings.Header
            {
                {
                    "ScreenSharePass", string.Empty
                },
                {
                    "ScreenSharePort", 22009
                }
            };
            Settings.Get()["Terminal"] = new Settings.Header
            {
                {
                    "AllowTerminal", true
                }
            };

            Settings.Get()["Debug"] = new Settings.Header
            {
                {
                    "TraceDebug", true
                }
            };

            Settings.Save();
        }

        public static void OpenFirewall()
        {
            var webServerPort = (int) Settings.Get("WebServer").WebServerPort;
            var apiPort = (int) Settings.Get("TaskServer").TaskServerPort;
            var terminalPort = 22008;
            var screenSharePort = (int) Settings.Get("ScreenShareService").ScreenSharePort;
            var nat = new NatDiscoverer();
            var cts = new CancellationTokenSource();
            cts.CancelAfter(5000);

            NatDevice device = null;
            var t = nat.DiscoverDeviceAsync(PortMapper.Upnp, cts);
            t.ContinueWith(tt =>
            {
                device = tt.Result;
                device.GetExternalIPAsync()
                    .ContinueWith(task =>
                    {
                        ;
                        return device.CreatePortMapAsync(
                            new Mapping(Protocol.Tcp, webServerPort, webServerPort, 0, "Ulterius Web Server"));
                    })
                    .Unwrap()
                    .ContinueWith(task =>
                    {
                        return device.CreatePortMapAsync(
                            new Mapping(Protocol.Tcp, screenSharePort, screenSharePort, 0, "Ulterius Screen Share"));
                    })
                    .Unwrap()
                    .ContinueWith(task =>
                    {
                        return device.CreatePortMapAsync(
                            new Mapping(Protocol.Tcp, apiPort, apiPort, 0, "Ulterius Api"));
                    })
                    .Unwrap()
                    .ContinueWith(task =>
                    {
                        return device.CreatePortMapAsync(
                            new Mapping(Protocol.Tcp, terminalPort, terminalPort, 0, "Ulterius Terminal"));
                    })
                    .Unwrap()
                    .ContinueWith(task => { Console.WriteLine("Ports forwarded!"); });
            }, TaskContinuationOptions.OnlyOnRanToCompletion);

            try
            {
                t.Wait();
            }
            catch (AggregateException e)
            {
                if (e.InnerException is NatDeviceNotFoundException)
                {
                    Console.WriteLine("No NAT Device Found");
                }
            }
        }

        private static bool SetLogging()
        {
            try
            {
                var filestream = new FileStream(Path.Combine(AppEnvironment.DataPath, "server.log"),
                     FileMode.Create);
                var streamwriter = new StreamWriter(filestream) { AutoFlush = true };
                Console.SetOut(streamwriter);
                Console.SetError(streamwriter);
                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }
        public static void ConfigureServer()
        {
            if (SetLogging())
            {
                Console.WriteLine("Logs Ready");
            }
            if (Settings.Empty)
            {
                //setup listen sh
                GenerateSettings();

                var webServerPort = (ushort) Settings.Get("WebServer").WebServerPort;
                var apiPort = (ushort) Settings.Get("TaskServer").TaskServerPort;
                var terminalPort = (ushort) 22008;
                var screenSharePort = (ushort) Settings.Get("ScreenShareService").ScreenSharePort;
                var prefix = $"http://*:{webServerPort}/";
                var username = Environment.GetEnvironmentVariable("USERNAME");
                var userdomain = Environment.GetEnvironmentVariable("USERDOMAIN");
                var command = $@"/C netsh http add urlacl url={prefix} user={userdomain}\{username} listen=yes";
                Process.Start("CMD.exe", command);
                OpenFirewall(webServerPort, "Ulterius Web Server");
                OpenFirewall(apiPort, "Ulterius Task Server");
                OpenFirewall(terminalPort, "Ulterius Terminal Server");
                OpenFirewall(screenSharePort, "Ulterius ScreenShareService");
            }
            SetStartup();
            if (File.Exists("client.zip"))
            {
                InstallClient();
            }
        }

        public static bool IsWindows()
        {
            var os = Environment.OSVersion;
            var pid = os.Platform;
            switch (pid)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    return true;

                case PlatformID.Unix:
                    return false;
                default:
                    return false;
            }
        }


        private static void SetStartup()
        {
            Console.WriteLine("Set Startup");
            try
            {
                var rk = Registry.CurrentUser.OpenSubKey
                       ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                var runStartup = Convert.ToBoolean(Settings.Get("General").RunStartup);
                var fileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "Bootstrapper.exe");
                if (runStartup)
                    rk?.SetValue("Ulterius", $"\"{fileName}\"");
                else
                    rk?.DeleteValue("Ulterius", false);
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        public static bool InstallClient()
        {
            try
            {
                var clientPath = Path.Combine(AppEnvironment.DataPath, "client/");
                Console.WriteLine("Extracting client archive");
                using (var zip = ZipFile.Read("client.zip"))
                {
                    zip.ExtractAll(clientPath
                        , ExtractExistingFileAction.OverwriteSilently);
                }
                File.Delete("client.zip");
                Console.WriteLine("Client deleted");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return false;
            }
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