using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Web.Script.Serialization;
using MiscUtil.Conversion;
using RemoteTaskServer.Utilities;

namespace RemoteTaskServer.Api
{
    internal class TaskApi
    {
        public string format = "JSON";


        public static void KillProcessById(int id)
        {
            Process p = Process.GetProcessById(id);
            p.Kill();

        }

        /// <summary>
        ///    Builds all of the system information and sends it off as JSON
        /// </summary>
        /// <returns></returns>
        public static string GetProcessInformation()
        {
            var results = new List<SystemProcesses>();

            foreach (var process in Process.GetProcesses())
            {
                var fullPath = "";
                var id = process.Id;
                var name = process.ProcessName;
                var icon = "";
                var memoryUsage = process.WorkingSet64;
                try
                {
                    fullPath = process.Modules[0].FileName;
                    icon = Tools.GetIconForProcess(fullPath);
                }
                catch (Win32Exception)
                {
                    fullPath = "null";
                    icon = "null";
                }

                results.Add(new SystemProcesses { id = id, path = fullPath, name = name, icon = icon });
            }


            var json = new JavaScriptSerializer().Serialize(results);

            return json;
        }
    }
}