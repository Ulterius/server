#region

using System;
using System.Collections.Concurrent;
using System.Timers;
using UlteriusServer.Api.Network.Models;

#endregion

namespace UlteriusServer.Api.Services.LocalSystem.Daemons
{
    public interface ICronDaemon
    {
        void AddJob(string schedule, Guid id, JobModel job);
        void Start();
        void Stop();
    }

    public class CronDaemon : ICronDaemon
    {
        private readonly ConcurrentDictionary<Guid, ICronJob> _cronJobs = new ConcurrentDictionary<Guid, ICronJob>();
        private readonly Timer _timer = new Timer(30000);
        private DateTime _last = DateTime.Now;
        public bool Online;
        public CronDaemon()
        {
            _timer.AutoReset = true;
            _timer.Elapsed += timerElapsed;
        }

        public void AddJob(string schedule, Guid id, JobModel job)
        {
            var cj = new CronJob(schedule, job);
            if (_cronJobs.ContainsKey(id))
            {
                _cronJobs[id] = cj;
            }
            else
            {
                _cronJobs.TryAdd(id, cj);
            }
        }

        public void Start()
        {
            Online = true;
            _timer.Start();
            Console.WriteLine("Cron Daemon Started");
        }

        public void Stop()
        {
            Online = false;
        
            _timer.Stop();

            foreach (var cronJob in _cronJobs.Values)
            {
                cronJob.Abort();
            }

            //clear all alive jobs
            _cronJobs.Clear();
        }

        private void timerElapsed(object sender, ElapsedEventArgs e)
        {
            if (DateTime.Now.Minute == _last.Minute) return;
            _last = DateTime.Now;
            foreach (var job in _cronJobs.Values)
                job.Execute(DateTime.Now);
        }

        public bool RemoveJob(Guid id)
        {
            ICronJob model;
            return _cronJobs.TryRemove(id, out model);
        }
    }
}