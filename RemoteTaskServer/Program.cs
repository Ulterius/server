#region

using System;
using System.Diagnostics;
using RemoteTaskServer.WebServer;
using UlteriusServer.Properties;
using UlteriusServer.TaskServer;
using UlteriusServer.TaskServer.Services.System;
using UlteriusServer.TerminalServer;
using UlteriusServer.Utilities;

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
          
            
            Tools.GenerateSettings();
            HttpServer.Setup();
            var systemUtilities = new SystemUtilities();
            systemUtilities.Start();
            //Keep down here if you actually want a functional program

            TaskManagerServer.Start();
            TerminalManagerServer.Start();

            Console.ReadLine();
        }
    }
}