#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UlteriusServer.TaskServer.Api.Models;
using UlteriusServer.TaskServer.Api.Serialization;
using UlteriusServer.Utilities;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers.Impl
{
    public class ProcessController : ApiController
    {
        private readonly WebSocket _client;
        private readonly Packets _packet;
        private readonly ApiSerializator _serializator = new ApiSerializator();

        public ProcessController(WebSocket client, Packets packet)
        {
            _client = client;
            _packet = packet;
        }

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
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
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
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, data);
        }

        public void RequestProcessInformation()
        {
            var processInformation = GetProcessInformation();
            _serializator.Serialize(_client, _packet.Endpoint, _packet.SyncKey, processInformation);
        }

     

        private List<SystemProcesses> GetProcessInformation()
        {
            var processInformation = new List<SystemProcesses>();
            var processKil = Process.GetProcesses();
            foreach (var process in processKil)
            {
                try
                {
                    var name = process.ProcessName;

                    var fullPath = process.MainModule.FileName;
                    var icon = Tools.GetIconForProcess(fullPath);
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

                    //Who cares
                }
            }
            return processInformation;
        }

      
    }
}