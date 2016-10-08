#region

using System;
using System.Collections.Concurrent;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UlteriusServer.Api.Network.Models;
using UlteriusServer.Api.Services.LocalSystem.Daemons;

#endregion

namespace UlteriusServer.Api.Services.LocalSystem
{
    public class CronJobService
    {
        public static readonly CronDaemon CronDaemon = new CronDaemon();
        public readonly string JobDatabasePath;
        public readonly string JobScriptsPath;
        public ConcurrentDictionary<Guid, JobModel> JobList;

        public CronJobService(string jobDatabasePath, string jobScriptsPath)
        {
            JobDatabasePath = jobDatabasePath;
            JobScriptsPath = jobScriptsPath;
        }

        public void Save()
        {
            File.WriteAllText(JobDatabasePath,
                JsonConvert.SerializeObject(JobList, Formatting.Indented, new StringEnumConverter()));
        }

        public void ShutDown()
        {
            if (!Status())
            {
                return;
            }
            CronDaemon.Stop();
        }

        public void Start()
        {
            if (Status())
            {
                return;
            }
            CronDaemon.Start();
        }

        public void AddJobs()
        {
            //load all our jobs 
            var jsonData = File.ReadAllText(JobDatabasePath);
            JobList = JsonConvert.DeserializeObject<ConcurrentDictionary<Guid, JobModel>>(jsonData)
                      ?? new ConcurrentDictionary<Guid, JobModel>();
            foreach (var job in JobList)
            {
                CronDaemon.AddJob(job.Value.Schedule, job.Key, job.Value);
            }
        }
        public bool Status()
        {
            return CronDaemon.Online;
        }

        public void AddOrUpdateJob(Guid id, JobModel job)
        {
            CronDaemon.AddJob(job.Schedule, id, job);
        }

        public bool RemoveJob(Guid id)
        {
            JobModel model;
            return JobList.TryRemove(id, out model) && CronDaemon.RemoveJob(id);
        }


        public void ConfigureJobs()
        {
            if (!File.Exists(JobDatabasePath))
            {
                File.WriteAllText(JobDatabasePath, "{}");
            }
            if (!Directory.Exists(JobScriptsPath))
            {
                Directory.CreateDirectory(JobScriptsPath);
            }
           AddJobs();
            // Grab the Scheduler instance from the Factory
            Start();
        }
    }
}