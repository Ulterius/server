#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using UlteriusServer.TaskServer.Network;
using UlteriusServer.TaskServer.Network.Messages;
using UlteriusServer.TaskServer.Network.Models;
using UlteriusServer.Utilities;
using UlteriusServer.WebSocketAPI.Authentication;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers.Impl
{
    public class ProcessPacketHandler : PacketHandler
    {
        private MessageBuilder _builder;
        private AuthClient _client;
        private Packet _packet;


        public void StartProcess()
        {
            var path = _packet.Args.AsEnumerable().First();
            bool processStarted;
            var processId = -1;
            try
            {
                var processStartInfo = new ProcessStartInfo((string) path);

                var process = new Process {StartInfo = processStartInfo};
                processStarted = process.Start();
                processId = process.Id;
            }
            catch (Exception)
            {
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
            if (string.IsNullOrEmpty(path))
            {
                return "null";
            }
            var icon = IconTools.GetIconForFile(
                path,
                ShellIconSize.LargeIcon
                );
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
            _client = packet.AuthClient;
            _packet = packet;
            _builder = new MessageBuilder(_client, _packet.EndPoint, _packet.SyncKey);
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