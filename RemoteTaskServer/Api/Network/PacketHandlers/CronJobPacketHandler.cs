#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
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
                case PacketManager.PacketTypes.GetAllJobs:
                    GetAllJobs();
                    break;
               
            }
        }

     

        private void GetJobContents()
        {
            try
            {
                string contents = null;
                var exist = false;
                var existIndex = false;
                var localPath = string.Empty;
                var jobId = Guid.Parse(_packet.Args[0].ToString());
                if (_cronJobService.JobList.ContainsKey(jobId))
                {
                    existIndex = true;
                    var path = _cronJobService.JobList[jobId].Name;
                    localPath = path;
                    if (File.Exists(path))
                    {
                        exist = true;
                        contents = Convert.ToBase64String(File.ReadAllBytes(path));
                    } 
                }
                var response = new
                {
                    
                    Id = jobId.ToString(),
                    Base64ScriptContents = contents,
                    ScriptExist = exist,
                    IndexExist = existIndex,
                    LocalPath  = localPath
                };
                _builder.WriteMessage(response);
            }
            catch (Exception ex)
            {
                var exceptionResponse = new
                {

                    Message = ex.Message,
                    Exception = ex.StackTrace
                };
                _builder.WriteMessage(exceptionResponse);
            }
        }

        public void GetAllJobs()
        {
            _builder.WriteMessage(_cronJobService.JobList);
        }

        private void GetJobDaemonStatus()
        {
            var response = new
            {
                Online = _cronJobService.Status()
            };
            _builder.WriteMessage(response);
        }

        private void StopJobDaemon()
        {
            var response = new
            {
                ShutDown = true
            };
            _cronJobService.ShutDown();
            _builder.WriteMessage(response);
        }

        public void StartJobDaemon()
        {
            var response = new
            {
                Started = true
            };
            _cronJobService.AddJobs();
            _cronJobService.Start();
            _builder.WriteMessage(response);
        }


        //Evan crys a lot
        public void RemoveJob()
        {
            try
            {
                
                var removed = false;
                var scriptRemoved = false;
                var jobId = Guid.Parse(_packet.Args[0].ToString());
                var jobExist = _cronJobService.JobList.ContainsKey(jobId);
                if (jobExist)
                {
                    var oldPath = _cronJobService.JobList[jobId].Name;
                    removed = _cronJobService.RemoveJob(jobId);
                    if (removed)
                    {
                        if (File.Exists(oldPath))
                        {
                            File.Delete(oldPath);
                            scriptRemoved = true;
                        }
                        _cronJobService.Save();
                    }
                }
                var response = new
                {
                    Id = jobId.ToString(),
                    JobRemoved = removed,
                    ScriptRemoved = scriptRemoved,
                    JobExisted = jobExist
                };
                _builder.WriteMessage(response);
            }
            catch (Exception ex)
            {
                var exceptionResponse = new
                {
                    Removed = false,
                    Message = ex.Message,
                    Exception = ex.StackTrace
                };
                _builder.WriteMessage(exceptionResponse);
            }
        }

        private void AddOrUpdateJob()
        {
            try
            {
                var argumentObject = (JObject) _packet.Args[0];
                var jobId = Guid.Parse(argumentObject["Guid"].ToString());
                var base64ScriptContents = argumentObject["Base64ScriptContents"].ToString();
                var data = Convert.FromBase64String(base64ScriptContents);
                var decodedString = Encoding.UTF8.GetString(data);
                var scriptSchedule = argumentObject["Schedule"].ToString();
                var scriptName = argumentObject["Name"].ToString();
                var scriptType = argumentObject["Type"].ToString();
                var newPath = Path.Combine(_cronJobService.JobScriptsPath, scriptName);
                if (_cronJobService.JobList.ContainsKey(jobId))
                {
                    var oldPath = _cronJobService.JobList[jobId].Name;
                    _cronJobService.JobList[jobId].Name = newPath;
                    if (!oldPath.Equals(newPath) && File.Exists(oldPath))
                    {
                        File.Delete(oldPath);
                    }
                    _cronJobService.JobList[jobId].Schedule = scriptSchedule;
                    _cronJobService.JobList[jobId].Type = scriptType;
                    var path = _cronJobService.JobList[jobId].Name;
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
                        Name = newPath
                    };
                    _cronJobService.AddOrUpdateJob(jobId, job);
                    _cronJobService.JobList.TryAdd(jobId, job);
                    _cronJobService.Save();
                }
                var response = new
                {
                    AddedOrUpdated = true,
                    Message = $"{jobId} was added to the server.",
                    SavedTo = newPath
                };
                _builder.WriteMessage(response);
            }
            catch (Exception ex)
            {
                var exceptionResponse = new
                {
                    AddedOrUpdated = false,
                    Message = ex.Message,
                    Exception = ex.StackTrace
                };
                _builder.WriteMessage(exceptionResponse);
            }
        }
    }
}