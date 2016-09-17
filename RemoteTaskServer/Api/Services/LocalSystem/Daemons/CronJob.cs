#region

using System;
using System.Threading.Tasks;
using UlteriusServer.Api.Network.Models;

#endregion

namespace UlteriusServer.Api.Services.LocalSystem.Daemons
{
    public interface ICronJob
    {
        Task Execute(DateTime dateTime);
        void Abort();
    }

    public class CronJob : ICronJob
    {
        private readonly ICronSchedule _cronSchedule;
        private readonly JobModel _job;

        private readonly object _lock = new object();

        public CronJob(string schedule, JobModel job)
        {
            _cronSchedule = new CronSchedule(schedule);
            _job = job;
        }

        public async Task Execute(DateTime dateTime)
        {
            if (!_cronSchedule.IsTime(dateTime))
                return;

            if (_job.Running)
                return;

            await _job.Execute();
        }

        public void Abort()
        {
            if (_job.Running)
            {
                _job.StopExecute();
            }
        }
    }
}