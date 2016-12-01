#region

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Topshelf;
using UlteriusServer.Api.Win32;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Usage;

#endregion

namespace UlteriusServer
{
    public class UlteriusAgent
    {
        private static string lastDesktop = "";
        private static Process process;
        private long _CurrentSession;

        private DateTime _lastDeskSwitch = DateTime.Now.AddDays(-10);
        private int _LastSession;
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
                }
            }
            var serverInstanceList = Process.GetProcessesByName("Ulterius Server");
            if (serverInstanceList.Length > 0)
            {
                serverInstanceList[0].Kill();
            }
        }

        public void HandleEvent(HostControl hostControl, SessionChangedArguments arg3)
        {
            HandleMonitor();
        }
    }
}