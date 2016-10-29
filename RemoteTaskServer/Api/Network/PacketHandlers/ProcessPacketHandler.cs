#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UlteriusServer.Api.Network.Messages;
using UlteriusServer.Api.Network.Models;
using UlteriusServer.Api.Win32;
using UlteriusServer.Utilities;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.Api.Network.PacketHandlers
{
    public class ProcessPacketHandler : PacketHandler
    {
        private AuthClient _authClient;
        private MessageBuilder _builder;
        private WebSocket _client;
        private Packet _packet;


        public void StartProcess()
        {
            var path = _packet.Args[0].ToString();
            bool processStarted;
            var processId = -1;
            try
            {
                if (Environment.UserName.Equals("SYSTEM") && Tools.RunningPlatform() == Tools.Platform.Windows)
                {
                    var task = Task.Run(() =>
                    {
                        try
                        {
                            ProcessStarter.PROCESS_INFORMATION procInfo;
                            ProcessStarter.StartProcessAndBypassUAC(path, out procInfo);
                        }
                        catch (Exception)
                        {
                            //continue
                        }
                    });
                    processStarted = task.Wait(TimeSpan.FromSeconds(5));
                }
                else
                {
                    var processStartInfo = new ProcessStartInfo(path);
                    var process = new Process {StartInfo = processStartInfo};
                    processStarted = process.Start();
                    processId = process.Id;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                processStarted = false;
            }
            var data = new
            {
                processStarted,
                processId,
                path
            };
            _builder.WriteMessage(data);
        }

        public void KillProcess()
        {
            //this is dumb
            var id = int.Parse(_packet.Args[0].ToString());
            string processName = null;
            var processKilled = false;
            foreach (var p in Process.GetProcesses().Where(p => p.Id == id))
            {
                processName = p.ProcessName;
                p.Kill();
                p.WaitForExit();
                processKilled = true;
                break;
            }
            var data = new
            {
                processKilled,
                id,
                processName
            };
            _builder.WriteMessage(data);
        }

        public void RequestProcessInformation()
        {
            var processInformation = GetProcessInformation();
            _builder.WriteMessage(processInformation);
        }

        public string GetIconForProcess(string path)
        {
            if (Tools.RunningPlatform() == Tools.Platform.Mac)
            {
                return "null";
            }
            if (string.IsNullOrEmpty(path))
            {
                return "null";
            }
            var icon = IconTools.GetIconForFile(path, ShellIconSize.LargeIcon);
            if (icon == null) return "null";
            var ms = new MemoryStream();
            icon.ToBitmap().Save(ms, ImageFormat.Png);
            var byteImage = ms.ToArray();
            var sigBase64 = Convert.ToBase64String(byteImage); //Get Base64
            return sigBase64;
        }

        private List<SystemProcesses> GetProcessInformation()
        {
            var processInformation = new List<SystemProcesses>();
            var processKil = Process.GetProcesses();
            foreach (var process in processKil)
            {
                try

                {
                    if (process.HasExited) continue;
                    var name = process.ProcessName;
                    var fullPath = process.MainModule.FileName;
                    var icon = GetIconForProcess(fullPath);
                    var processId = process.Id;
                    var handles = process.HandleCount;
                    var threads = process.Threads.Count;
                    var memory = process.WorkingSet64;
                    var wallTime = DateTime.Now - process.StartTime;
                    if (process.HasExited) wallTime = process.ExitTime - process.StartTime;
                    var procTime = process.TotalProcessorTime;
                    var cpuUsage = 100*procTime.TotalMilliseconds/wallTime.TotalMilliseconds;
                    var sysP = new SystemProcesses
                    {
                        Id = processId,
                        Path = fullPath,
                        Name = name,
                        Icon = icon,
                        RamUsage = memory,
                        CpuUsage = cpuUsage,
                        Threads = threads,
                        Handles = handles
                    };
                    processInformation.Add(sysP);
                }
                catch (Exception)
                {
                    //who cares
                }
            }
            return processInformation;
        }

        public override void HandlePacket(Packet packet)
        {
            _client = packet.Client;
            _authClient = packet.AuthClient;
            _packet = packet;
            _builder = new MessageBuilder(_authClient, _client, _packet.EndPoint, _packet.SyncKey);
            switch (_packet.PacketType)
            {
                case PacketManager.PacketTypes.RequestProcessInformation:
                    RequestProcessInformation();
                    break;
                case PacketManager.PacketTypes.StartProcess:

                    StartProcess();
                    break;
                case PacketManager.PacketTypes.KillProcess:
                    KillProcess();
                    break;
            }
        }
    }
}