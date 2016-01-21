#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Timers;
using UlteriusServer.TaskServer.Api.Models;
using UlteriusServer.TaskServer.Api.Serialization;
using UlteriusServer.Utilities;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers.Impl
{
    public class ProcessController : ApiController
    {
        private readonly WebSocket client;
        private readonly Packets packet;
        private readonly ApiSerializator serializator = new ApiSerializator();
        private Timer streamTimer;

        public ProcessController(WebSocket client, Packets packet)
        {
            this.client = client;
            this.packet = packet;
        }

        public void StartProcess()
        {
            var path = packet.args.AsEnumerable().First();
            bool processStarted;
            var processId = -1;
            try
            {
                var processStartInfo = new ProcessStartInfo((string) path);

                var process = new Process {StartInfo = processStartInfo};
                if (!process.Start())
                {
                    processStarted = false;
                }
                processStarted = true;
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
            serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
        }

        public void KillProcess()
        {
            //this is dumb
            var id = int.Parse(packet.args.First().ToString());
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
            serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
        }

        public void RequestProcessInformation()
        {
            var processInformation = GetProcessInformation();
            serializator.Serialize(client, packet.endpoint, packet.syncKey, processInformation);
        }

        public void StreamProcessInformation()
        {
            var loopTime = int.Parse(packet.args.First().ToString());
            streamTimer = new Timer(loopTime)
            {
                Enabled = true,
                AutoReset = true
            };
            streamTimer.Elapsed += StreamInformation;
        }


        private void StreamInformation(object sender, ElapsedEventArgs e)
        {
            if (client.IsConnected && API.ProcessState == API.States.StreamingProcessData)
            {
                RequestProcessInformation();
            }
            else if (client.IsConnected && API.ProcessState == API.States.Standard)
            {
                StopProcessStream();
            }
            else
            {
                StopProcessStream();
            }
        }

        public void StopProcessStream()
        {
            if (streamTimer != null)
            {
                streamTimer.Enabled = false;
                streamTimer.AutoReset = false;
                streamTimer.Stop();
                streamTimer.Dispose();
            }
            API.ProcessState = API.States.Standard;
        }

        private List<SystemProcesses> GetProcessInformation()
        {
            var processInformation = new List<SystemProcesses>();
            var simpleProcesses = new List<SimpleProcessInfo>();
            try
            {
                using (var searcher =
                    new ManagementObjectSearcher("root\\CIMV2",
                        "SELECT ExecutablePath, ProcessId FROM Win32_Process"))
                {
                    simpleProcesses.AddRange(from ManagementBaseObject info in searcher.Get()
                        let id = int.Parse(info["ProcessId"].ToString())
                        let fullPath = (string) info["ExecutablePath"]
                        select new SimpleProcessInfo
                        {
                            path = fullPath,
                            id = id
                        });
                }

                var options = new EnumerationOptions {ReturnImmediately = false};
                using (var searcher =
                    new ManagementObjectSearcher("root\\CIMV2",
                        "SELECT * FROM Win32_PerfFormattedData_PerfProc_Process", options))
                {
                    processInformation.AddRange(from ManagementBaseObject queryObj in searcher.Get()
                        where queryObj != null
                        let name = (string) queryObj["Name"]
                        let processId = int.Parse(queryObj["IDProcess"].ToString())
                        let handles = int.Parse(queryObj["HandleCount"].ToString())
                        let threads = int.Parse(queryObj["ThreadCount"].ToString())
                        let memory = long.Parse(queryObj["WorkingSetPrivate"].ToString())
                        let cpuUsage = int.Parse(queryObj["PercentProcessorTime"].ToString())
                        let ioReadOperationsPerSec = int.Parse(queryObj["IOReadOperationsPerSec"].ToString())
                        let ioWriteOperationsPerSec = int.Parse(queryObj["IOWriteOperationsPerSec"].ToString())
                        let fullPath = ""
                        let icon = ""
                        select new SystemProcesses
                        {
                            id = processId,
                            path = fullPath,
                            name = name,
                            icon = icon,
                            ramUsage = memory,
                            cpuUsage = cpuUsage,
                            threads = threads,
                            handles = handles,
                            ioWriteOperationsPerSec = ioWriteOperationsPerSec,
                            ioReadOperationsPerSec = ioReadOperationsPerSec
                        });
                    foreach (var result in processInformation)
                    {
                        foreach (var process in simpleProcesses.Where(process => process.id == result.id))
                        {
                            result.path = process.path;
                            if (!string.IsNullOrEmpty(result.path))
                            {
                                result.icon = Tools.GetIconForProcess(result.path);
                            }
                            else
                            {
                                result.path = "null";
                                result.icon = "null";
                            }
                        }
                    }
                }
            }
            catch (ManagementException e)
            {
                var data = new
                {
                    managementException = true,
                    message  = e.Message
                };
                serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
            }

            return processInformation;
        }

        public class SimpleProcessInfo
        {
            public int id;
            public string path;
        }
    }
}