#region

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using RemoteTaskServer.WebServer;
using UlteriusServer.Properties;
using UlteriusServer.Server;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.System;
using UlteriusServer.Windows.Api;

#endregion

namespace UlteriusServer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.Title = Resources.Program_Title;
            if (!Debugger.IsAttached)
                ExceptionHandler.AddGlobalHandlers();

            SettingsApi.GenerateSettings();
            HttpServer.Setup();
            var systemUtilities = new SystemUtilities();
            systemUtilities.Start();
            //Keep down here if you actually want a functional program
            TaskServer.Start();
            Console.ReadLine();
        }
    }
}