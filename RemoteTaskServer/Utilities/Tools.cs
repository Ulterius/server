#region

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Ionic.Zip;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using NetFwTypeLib;
using Open.Nat;
using UlteriusServer.WebServer;
using static System.Security.Principal.WindowsIdentity;
using Task = System.Threading.Tasks.Task;

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

        private static void OpenFirewallPort(ushort port, string name)
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
                    firewallRule.Protocol = (int) NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
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

        public enum Platform
        {
            Windows,
            Linux,
            Mac
        }
        public static Platform RunningPlatform()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                    // Well, there are chances MacOSX is reported as Unix instead of MacOSX.
                    // Instead of platform check, we'll do a feature checks (Mac specific root folders)
                    if (Directory.Exists("/Applications")
                        & Directory.Exists("/System")
                        & Directory.Exists("/Users")
                        & Directory.Exists("/Volumes"))
                        return Platform.Mac;
                    else
                        return Platform.Linux;

                case PlatformID.MacOSX:
                    return Platform.Mac;

                default:
                    return Platform.Windows;
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
                    "UploadLogs", true
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
                    "UpnpEnabled", true
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
                    "ScreenSharePort", 22009
                }
            };
            Settings.Get()["Terminal"] = new Settings.Header
            {
                {
                    "AllowTerminal", true
                },
                {
                    "TerminalPort", 22008
                }
            };
            Settings.Get()["Webcams"] = new Settings.Header
            {
                {
                    "UseWebcams", true
                },
                {
                    "WebcamPort", 22010
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

        public static void ForwardPorts(PortMapper type = PortMapper.Upnp, bool retry = false)
        {
            var webServerPort = (int) Settings.Get("WebServer").WebServerPort;
            var apiPort = (int) Settings.Get("TaskServer").TaskServerPort;
            var webCamPort = (int) Settings.Get("Webcams").WebcamPort;
            var terminalPort = (int) Settings.Get("Terminal").TerminalPort;
            var screenSharePort = (int) Settings.Get("ScreenShareService").ScreenSharePort;
            var nat = new NatDiscoverer();
            var cts = new CancellationTokenSource();
            cts.CancelAfter(5000);
            NatDevice device;
            var t = nat.DiscoverDeviceAsync(type, cts);
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
                            new Mapping(Protocol.Tcp, webCamPort, webCamPort, 0, "Ulterius Webcams"));
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
                    if (retry)
                    {
                        return;
                    }
                    ForwardPorts(PortMapper.Pmp, true);
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
                var streamwriter = new StreamWriter(filestream) {AutoFlush = true};
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
                if (RunningPlatform() == Platform.Windows)
                {
                    var webServerPort = (ushort)Settings.Get("WebServer").WebServerPort;
                    var apiPort = (ushort)Settings.Get("TaskServer").TaskServerPort;
                    var webcamPort = (ushort)Settings.Get("Webcams").WebcamPort;
                    var terminalPort = (ushort)Settings.Get("Terminal").TerminalPort;
                    var screenSharePort = (ushort)Settings.Get("ScreenShareService").ScreenSharePort;
                    var prefix = $"http://*:{webServerPort}/";
                    var username = Environment.GetEnvironmentVariable("USERNAME");
                    var userdomain = Environment.GetEnvironmentVariable("USERDOMAIN");
                    var command = $@"/C netsh http add urlacl url={prefix} user={userdomain}\{username} listen=yes";
                    Process.Start("CMD.exe", command);
                    OpenFirewallPort(webcamPort, "Ulterius Web Cams");
                    OpenFirewallPort(webServerPort, "Ulterius Web Server");
                    OpenFirewallPort(apiPort, "Ulterius Task Server");
                    OpenFirewallPort(terminalPort, "Ulterius Terminal Server");
                    OpenFirewallPort(screenSharePort, "Ulterius ScreenShareService");
                }
            }
            if (RunningPlatform() == Platform.Windows)
            {
                SetStartup();
            }
            if (File.Exists("client.zip"))
            {
               Task.Run(() => InstallClient());
             
            }
        }

       
        


        private static void LegacyStartupRemove()
        {
            try
            {
                var rk = Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                rk?.DeleteValue("Ulterius", false);
            }
            catch (Exception)
            {
                //fail
            }
        }

        private static void SetStartup()
        {
            Console.WriteLine("Set Startup");
            LegacyStartupRemove();
            try
            {
                var runStartup = Convert.ToBoolean(Settings.Get("General").RunStartup);
                var fileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "Bootstrapper.exe");
                using (var sched = new TaskService())
                {
                    var username = Environment.UserDomainName + "\\" + Environment.UserName;
                    var t = sched.GetTask($"Ulterius {Environment.UserName}");
                    var taskExists = t != null;
                    if (runStartup)
                    {
                        if (taskExists) return;
                        var td = TaskService.Instance.NewTask();
                        td.Principal.RunLevel = TaskRunLevel.Highest;

                        td.RegistrationInfo.Author = "Octopodal Solutions";
                        td.RegistrationInfo.Date = new DateTime();
                        td.RegistrationInfo.Description =
                            "Keeps your Ulterius server up to date. If this task is disabled or stopped, your Ulterius server will not be kept up to date, meaning security vulnerabilities that may arise cannot be fixed and features may not work.";

                        var logT = new LogonTrigger
                        {
                            Delay = new TimeSpan(0, 0, 0, 10),
                            UserId = username
                        };
                        //wait 10 seconds until after login is complete to boot
                        td.Triggers.Add(logT);

                        td.Actions.Add(fileName);
                        TaskService.Instance.RootFolder.RegisterTaskDefinition($"Ulterius {Environment.UserName}", td);
                        Console.WriteLine("Task Registered");
                    }
                    else
                    {
                        if (taskExists)
                        {
                            sched.RootFolder.DeleteTask($"Ulterius {Environment.UserName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not set startup task");
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