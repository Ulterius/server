#region

using System;
using System.IO;
using System.Text;
using UlteriusServer.Api.Network.Messages;
using UlteriusServer.Api.Network.Models;
using UlteriusServer.Api.Services.LocalSystem;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.Api.Network.PacketHandlers
{
    public class CronJobPacketHandler : PacketHandler
    {
        private readonly CronJobService _cronJobService = UlteriusApiServer.CronJobService;
        private AuthClient _authClient;
        private MessageBuilder _builder;
        private WebSocket _client;
        private Packet _packet;


        public override void HandlePacket(Packet packet)
        {
            _client = packet.Client;
            _authClient = packet.AuthClient;
            _packet = packet;
            _builder = new MessageBuilder(_authClient, _client, _packet.EndPoint, _packet.SyncKey);
            switch (_packet.PacketType)
            {
                case PacketManager.PacketTypes.AddOrUpdateJob:
                    AddOrUpdateJob();
                    break;
                case PacketManager.PacketTypes.RemoveJob:
                    RemoveJob();
                    break;
                case PacketManager.PacketTypes.StopJobDaemon:
                    StopJobDaemon();
                    break;
                case PacketManager.PacketTypes.StartJobDaemon:
                    StartJobDaemon();
                    break;
                case PacketManager.PacketTypes.GetJobDaemonStatus:
                    GetJobDaemonStatus();
                    break;
                case PacketManager.PacketTypes.GetJobContents:
                    GetJobContents();
                    break;
            }
        }

        private void GetJobContents()
        {
            try
            {
                string contents = null;
                var exist = false;
                var jobId = Guid.Parse(_packet.Args[0].ToString());
                if (_cronJobService.JobList.ContainsKey(jobId))
                {
                    var path = _cronJobService.JobList[jobId].Path;
                    if (File.Exists(path))
                    {
                        exist = true;
                        contents = Convert.ToBase64String(File.ReadAllBytes(path));
                    }
                }
                var response = new
                {
                    id = jobId.ToString(),
                    scriptContents = contents,
                    scriptExist = exist
                };
                _builder.WriteMessage(response);
            }
            catch (Exception ex)
            {
                var exceptionResponse = new
                {
                    removed = false,
                    message = ex.Message,
                    exception = ex.StackTrace
                };
                _builder.WriteMessage(exceptionResponse);
            }
        }

        private void GetJobDaemonStatus()
        {
            var response = new
            {
                online = _cronJobService.Status()
            };
            _builder.WriteMessage(response);
        }

        private void StopJobDaemon()
        {
            var response = new
            {
                shutDown = true
            };
            _cronJobService.ShutDown();
            _builder.WriteMessage(response);
        }

        public void StartJobDaemon()
        {
            var response = new
            {
                started = true
            };
            _cronJobService.Start();
            _builder.WriteMessage(response);
        }

        public void RemoveJob()
        {
            try
            {
                var removed = false;
                var jobId = Guid.Parse(_packet.Args[0].ToString());
                if (_cronJobService.JobList.ContainsKey(jobId))
                {
                    var oldPath = _cronJobService.JobList[jobId].Path;
                    removed = _cronJobService.RemoveJob(jobId);
                    if (removed)
                    {
                        if (File.Exists(oldPath))
                        {
                            File.Delete(oldPath);
                        }
                        _cronJobService.Save();
                    }
                }
                var response = new
                {
                    id = jobId.ToString(),
                    jobRemoved = removed
                };
                _builder.WriteMessage(response);
            }
            catch (Exception ex)
            {
                var exceptionResponse = new
                {
                    removed = false,
                    message = ex.Message,
                    exception = ex.StackTrace
                };
                _builder.WriteMessage(exceptionResponse);
            }
        }

        private void AddOrUpdateJob()
        {
            try
            {
                var jobId = Guid.Parse(_packet.Args[0].ToString());
                var scriptContents = _packet.Args[1].ToString();
                var data = Convert.FromBase64String(scriptContents);
                var decodedString = Encoding.UTF8.GetString(data);
                var scriptSchedule = _packet.Args[2].ToString();
                var scriptName = _packet.Args[3].ToString();
                var scriptType = _packet.Args[4].ToString();
                var newPath = Path.Combine(_cronJobService.JobScriptsPath, scriptName);
                if (_cronJobService.JobList.ContainsKey(jobId))
                {
                    var oldPath = _cronJobService.JobList[jobId].Path;
                    _cronJobService.JobList[jobId].Path = newPath;
                    if (!oldPath.Equals(newPath) && File.Exists(oldPath))
                    {
                        File.Delete(oldPath);
                    }
                    _cronJobService.JobList[jobId].Schedule = scriptSchedule;
                    _cronJobService.JobList[jobId].Type = scriptType;
                    var path = _cronJobService.JobList[jobId].Path;
                    //job exist, lets update its contents
                    File.WriteAllText(path, decodedString);
                    _cronJobService.AddOrUpdateJob(jobId, _cronJobService.JobList[jobId]);
                    _cronJobService.Save();
                }
                else
                {
                    File.WriteAllText(newPath, decodedString);
                    var job = new JobModel
                    {
                        Schedule = scriptSchedule,
                        Type = scriptType,
                        Path = newPath
                    };
                    _cronJobService.AddOrUpdateJob(jobId, job);
                    _cronJobService.JobList.TryAdd(jobId, job);
                    _cronJobService.Save();
                }
                var response = new
                {
                    addedOrUpdated = true,
                    message = $"{jobId} was added to the server.",
                    savedTo = newPath
                };
                _builder.WriteMessage(response);
            }
            catch (Exception ex)
            {
                var exceptionResponse = new
                {
                    addedOrUpdated = false,
                    message = ex.Message,
                    exception = ex.StackTrace
                };
                _builder.WriteMessage(exceptionResponse);
            }
        }
    }
}