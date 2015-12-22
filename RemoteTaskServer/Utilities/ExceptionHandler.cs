using System;
using System.IO;
using System.Net.Mime;
using Newtonsoft.Json;


namespace RemoteTaskServer.Utilities
{
    public static class ExceptionHandler
    {
        private static readonly string LogsPath = Path.Combine("", "Logs");

        public static void AddGlobalHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                try
                {
                    if (!Directory.Exists(ExceptionHandler.LogsPath))
                        Directory.CreateDirectory(ExceptionHandler.LogsPath);

                    string filePath = Path.Combine(ExceptionHandler.LogsPath, string.Format("UnhandledException_{0}.json", DateTime.Now.ToShortDateString().Replace("/", "-")));

                    File.AppendAllText(filePath, JsonConvert.SerializeObject(args.ExceptionObject, Formatting.Indented) + "\r\n\r\n");

                  Console.WriteLine(string.Format("An Unhandled Exception was Caught and Logged to:\r\n{0}", filePath));
                }
                catch { }
            };
        }
    }
}