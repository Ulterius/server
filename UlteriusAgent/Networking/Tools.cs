#region

using System;
using System.Diagnostics;
using System.Linq;

#endregion

namespace UlteriusAgent.Networking
{
    public static class Tools
    {
        public static void KillAllButMe()
        {
            try
            {
                var current = Process.GetCurrentProcess();
                //kill any other agent that may be running
                var processes = Process.GetProcessesByName(current.ProcessName)
                    .Where(t => t.Id != current.Id)
                    .ToList();
                foreach (var process in processes)
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                // ignored
            }
        }
    }
}