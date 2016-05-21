using System;
using System.Collections.Generic;
using System.IO;
using Magnum.Calendar;
using UlteriusServer.Utilities.Extensions;

namespace UlteriusServer.Utilities
{
    public static class Trace
    {
        #region Delegates

        public delegate void TraceDelegate(string text);

        #endregion

        public static readonly string TraceFilenameFormat = "Trace-" + $"{DateTime.Now:HH-mm-ss tt} {(int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds}.log";

        private static List<TraceDelegate> _traceDelegates;

        public static void AddTraceLogger(TraceDelegate traceAction)
        {
            _traceDelegates.Add(traceAction);
        }

        public static void Debug(string format, params object[] args)
        {
            if (Settings.Get("Debug").TraceDebug)
            {
                Log(format, args);
            }
        }

        public static void Initialize()
        {
            _traceDelegates = new List<TraceDelegate>();
            var tracePath = Path.Combine(ExceptionHandler.LogsPath, "Traces");
            var filePath = Path.Combine(tracePath, TraceFilenameFormat);
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            var textWriter = File.CreateText(filePath);
            AddTraceLogger(s =>
            {
                textWriter.WriteLine("[%]: %".Form(DateTime.Now.ToLongTimeString(), s));
                textWriter.Flush();
            });

            /*#if DEBUG
                        var consoleThread = new Thread(AddConsole)
                        {
                            IsBackground = true,
                        };
                        //consoleThread.Start();
            #endif*/
        }

        public static void Log(string format, params object[] args)
        {
            _traceDelegates.ForEach(action => action(format.Form(args)));
        }
    }
}
