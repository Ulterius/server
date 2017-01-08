#region

using System;
using System.Diagnostics;
using Topshelf;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Usage;

#endregion

namespace UlteriusServer
{
    public class UlteriusAgent
    {


        private Ulterius _ulterius;

        public void Start()
        {
            _ulterius = new Ulterius();
            _ulterius.Start(true);
            HandleMonitor();
            var hardware = new HardwareSurvey();
            hardware.Setup(true);
            Console.ReadLine();
        }

       
        public void HandleMonitor()
        {
            Tools.RestartAgent();
        }


        public void Stop()
        {
            var agentList = Process.GetProcessesByName("UlteriusAgent");
            foreach (var agent in agentList)
            {
                try
                {
                    agent.Kill();
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            var managerList = Process.GetProcessesByName("DaemonManager");
            foreach (var manager in managerList)
            {
                try
                {
                    manager.Kill();
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            var serverInstanceList = Process.GetProcessesByName("Ulterius Server");
            foreach (var server in serverInstanceList)
            {
                try
                {
                    server.Kill();
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        public void HandleEvent(HostControl hostControl, SessionChangedArguments arg3)
        {
            HandleMonitor();
        }
    }
}