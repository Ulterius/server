#region

using System;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;

#endregion

namespace UlteriusServer.Utilities
{
    public static class ExceptionHandler
    {
        public static readonly string LogsPath = Path.Combine(AppEnvironment.DataPath, "Logs");

        public static void AddGlobalHandlers()
        {

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                try
                {
                    if (!Directory.Exists(LogsPath))
                        Directory.CreateDirectory(LogsPath);


                    var filePath = Path.Combine(LogsPath,
                        $"UnhandledException_{DateTime.Now.ToShortDateString().Replace("/", "-")}.json");

                    File.AppendAllText(filePath, JsonConvert.SerializeObject(args.ExceptionObject, Formatting.Indented) + "\r\n\r\n");

                    Console.WriteLine($"An Unhandled Exception was Caught and Logged to:\r\n{filePath}");
                }
                catch
                {
                    // ignored
                }
            };


            Application.ThreadException += (sender, args) =>
            {
                try
                {
                    if (!Directory.Exists(LogsPath))
                        Directory.CreateDirectory(LogsPath);

                    var filePath = Path.Combine(LogsPath,
                      $"ThreadException_{DateTime.Now.ToShortDateString().Replace("/", "-")}.json");
                 

                    File.AppendAllText(filePath, JsonConvert.SerializeObject(args.Exception, Formatting.Indented) + "\r\n\r\n");

                   Console.WriteLine($"An Unhandled Thread Exception was Caught and Logged to:\r\n{filePath}");
                }
                catch
                {
                    // ignored
                }
            };

         
        }
    }
}