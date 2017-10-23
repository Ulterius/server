using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;
using Warden.Core;
using Warden.Core.Utils;

namespace UlteriusAgent
{
    public class UlteriusAgent
    {
        private readonly string _ulteriusPath;
        private WardenProcess _ulteriusInstance;


        public UlteriusAgent()
        {
            const string ulteriusFileName = "Ulterius Server.exe";
            _ulteriusPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ulteriusFileName);
        }

        public void Start()
        {
            if (!File.Exists(_ulteriusPath))
            {
                throw new InvalidOperationException($"Unable to locate Ulterius at {_ulteriusPath}");
            }
            if (Respawn())
            {
                Console.WriteLine("Rainway started as a service!");
                Console.ReadLine();
            }
            else
            {
                Console.WriteLine("Failed to start Rainway!");
                Environment.Exit(1);
            }
        }

        private bool Respawn()
        {
            _ulteriusInstance = null;
            _ulteriusInstance = WardenProcess.Start(_ulteriusPath, string.Empty, null, true).GetAwaiter().GetResult();
            if (_ulteriusInstance == null || !_ulteriusInstance.IsTreeActive())
            {
                return false;
            }
            _ulteriusInstance.OnStateChange += UlteriusInstanceOnOnStateChange;
            return true;
        }

        public void Stop()
        {
            const string ulteriusFileName = "Ulterius Server.exe";
            if (_ulteriusInstance != null)
            {
                _ulteriusInstance?.Kill();
                WardenManager.Flush(_ulteriusInstance.Id);
            }
            _ulteriusInstance = null;
            EndProcessTree(ulteriusFileName);
        }

        private void EndProcessTree(string imageName)
        {
            try
            {
                var taskKill = new TaskKill
                {
                    Arguments = new List<TaskSwitch>()
                    {
                        TaskSwitch.Force,
                        TaskSwitch.TerminateChildren,
                        TaskSwitch.ImageName.SetValue(imageName)
                    }
                };
                taskKill.Execute(out var output, out var errror);
            }
            catch
            {
                //
            }
        }

        private void UlteriusInstanceOnOnStateChange(object sender, StateEventArgs stateEventArgs)
        {
            if (stateEventArgs.Id == _ulteriusInstance.Id && stateEventArgs.State == ProcessState.Dead)
            {
                //Kill the entire tree.
                _ulteriusInstance.Kill();
                WardenManager.Flush(_ulteriusInstance.Id);
                if (Respawn())
                {
                   Console.WriteLine("Rainway restarted!");
                }
            }
        }

        public void HandleEvent(HostControl hostControl, SessionChangedArguments arg3)
        {
            
        }
    }
}
