#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AgentInterface.Api.Win32;
using Ionic.Zip;
using NetFwTypeLib;
using Open.Nat;
using UlteriusServer.Utilities.Settings;
using static System.Security.Principal.WindowsIdentity;
using Task = System.Threading.Tasks.Task;

#endregion

namespace UlteriusServer.Utilities
{
    internal class Tools
    {
        private static int _LastSession;
        private static int _CurrentSession;
        private static Process agentProcess;
        private static Process managerProcess;

        public enum Platform
        {
            Windows,
            Linux,
            Mac
        }

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

        public static void RestartDaemon()
        {

            try
            {
                if (Process.GetProcessesByName("DaemonManager").Length != 0) return;
                ProcessStarter.PROCESS_INFORMATION managerInfo;
                var managerPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "DaemonManager.exe");
                ProcessStarter.StartProcessAndBypassUAC(managerPath,
                out managerInfo);
                managerProcess = Process.GetProcessById((int)managerInfo.dwProcessId);
            }
            catch (Exception)
            {

               
            }
        }

        public static void RestartAgent()
        {
            
            _LastSession = Desktop.WTSGetActiveConsoleSessionId();
            _CurrentSession = _LastSession;
            try
            {
                agentProcess?.Kill();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
         
            Thread.Sleep(3000);
            ProcessStarter.PROCESS_INFORMATION agentInfo;
        
         
            var agentPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "UlteriusAgent.exe");

            

            ProcessStarter.StartProcessAndBypassUAC(agentPath,
                out agentInfo);
            
            agentProcess = Process.GetProcessById((int)agentInfo.dwProcessId);
            if (agentProcess != null)
            {
                Console.WriteLine("Started Monitor on " + _CurrentSession);
            }
            else
            {
                Console.WriteLine("Failed to start monitor on " + _CurrentSession);
            }
        }
        private static T GetComObject<T>(string progId)
        {
            var t = Type.GetTypeFromProgID(progId, true);
            return (T) Activator.CreateInstance(t);
        }

        private static void OpenFirewallPort(int port, string name)
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
                    return Platform.Linux;

                case PlatformID.MacOSX:
                    return Platform.Mac;

                default:
                    return Platform.Windows;
            }
        }

        public static void ForwardPorts(PortMapper type = PortMapper.Upnp, bool retry = false)
        {
            var config = Config.Load();
            var webServerPort = config.WebServer.WebServerPort;
            var apiPort = config.TaskServer.TaskServerPort;
            var webCamPort = config.Webcams.WebcamPort;
            var terminalPort = config.Terminal.TerminalPort;
            var screenSharePort = config.ScreenShareService.ScreenSharePort;
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
                     return device.DeletePortMapAsync(
                         new Mapping(Protocol.Tcp, webServerPort, webServerPort, 0, "Ulterius Web Server"));
                 })
                    .Unwrap()
                    .ContinueWith(task =>
                    {
                        return device.DeletePortMapAsync(
                            new Mapping(Protocol.Tcp, screenSharePort, screenSharePort, 0, "Ulterius Screen Share"));
                    })
                    .Unwrap()
                    .ContinueWith(task =>
                    {
                        return device.DeletePortMapAsync(
                            new Mapping(Protocol.Tcp, apiPort, apiPort, 0, "Ulterius Api"));
                    })
                    .Unwrap()
                    .ContinueWith(task =>
                    {
                        return device.DeletePortMapAsync(
                            new Mapping(Protocol.Tcp, webCamPort, webCamPort, 0, "Ulterius Webcams"));
                    })
                    .Unwrap()
                    .ContinueWith(task =>
                    {
                        return device.DeletePortMapAsync(
                            new Mapping(Protocol.Tcp, terminalPort, terminalPort, 0, "Ulterius Terminal"));
                    })
                    .Unwrap()
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

        public static string GetUsernameAsService()
        {
            if (!Environment.UserName.Equals("SYSTEM")) return Environment.UserName;
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT UserName FROM Win32_ComputerSystem"))
                {
                    using (var collection = searcher.Get())
                    {
                        var s =
                            ((string) collection.Cast<ManagementBaseObject>().First()["UserName"]).Split('\\').ToList();
                        //remove the Guest account
                        s.Remove("HomeGroupUser$");
                        s.Remove("Guest");
                        return s.Count > 1 ? s.LastOrDefault() : s.FirstOrDefault();
                    }
                }
            }
            catch (Exception)
            {
                //If we can't get the current user then we result to this
                var users = new List<string>();
                var path = $"WinNT://{Environment.MachineName},computer";
                using (var computerEntry = new DirectoryEntry(path))
                {
                    users.AddRange(from DirectoryEntry childEntry in computerEntry.Children
                        where childEntry.SchemaClassName == "User"
                        select childEntry.Name);
                }
                //remove the Guest account
                users.Remove("HomeGroupUser$");
                users.Remove("Guest");
                return users.Count > 1 ? users.LastOrDefault() : users.FirstOrDefault();
            }
        }

        private static bool SetLogging()
        {
            try
            {
                var filestream = new FileStream(Path.Combine(AppEnvironment.DataPath, "server.log"), FileMode.Create);
                var streamwriter = new StreamWriter(filestream, Encoding.UTF8) {AutoFlush = true};
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
            if (Config.Empty)
            {
                if (RunningPlatform() == Platform.Windows)
                {
                    var config = Config.Load();
                    var webServerPort = config.WebServer.WebServerPort;
                    var apiPort = config.TaskServer.TaskServerPort;
                    var webcamPort = config.Webcams.WebcamPort;
                    var terminalPort = config.Terminal.TerminalPort;
                    var screenSharePort = config.ScreenShareService.ScreenSharePort;
                    var prefix = $"http://*:{webServerPort}/";
                    var username = Environment.GetEnvironmentVariable("USERNAME");
                    var userdomain = Environment.GetEnvironmentVariable("USERDOMAIN");
                    var command = $@"/C netsh http add urlacl url={prefix} user={userdomain}\{username} listen=yes";
                    if (RunningAsService())
                    {
                        ProcessStarter.PROCESS_INFORMATION procInfo;
                        ProcessStarter.StartProcessAndBypassUAC("CMD.exe " + command,
                            out procInfo);
                    }
                    else
                    {
                        Process.Start("CMD.exe", command);
                    }

                    OpenFirewallPort(webcamPort, "Ulterius Web Cams");
                    OpenFirewallPort(webServerPort, "Ulterius Web Server");
                    OpenFirewallPort(apiPort, "Ulterius Task Server");
                    OpenFirewallPort(terminalPort, "Ulterius Terminal Server");
                    OpenFirewallPort(screenSharePort, "Ulterius ScreenShareService");
                    if (RunningAsService())
                    {
                        var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        OpenFirewallForProgram(Path.Combine(path, "Ulterius Server.exe"),
                            "Ulterius Server");
                    }
                }
            }
            if (File.Exists("client.zip"))
            {
                Task.Run(() => InstallClient());
            }
        }

       


        private static void OpenFirewallForProgram(string exeFileName, string displayName)
        {
            var proc = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments =
                        string.Format(
                            "firewall add allowedprogram program=\"{0}\" name=\"{1}\" profile=\"ALL\"",
                            exeFileName, displayName),
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            proc.WaitForExit();
        }

        public static bool RunningAsService()
        {
            return GetCurrent().Name.ToLower().Contains(@"nt authority\system");
        }


        public static bool InstallClient()
        {
            try
            {
                var clientPath = Path.Combine(AppEnvironment.DataPath, "client/");
                Console.WriteLine("Extracting client archive");
                using (var zip = ZipFile.Read("client.zip"))
                {
                    zip.ExtractAll(clientPath, ExtractExistingFileAction.OverwriteSilently);
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