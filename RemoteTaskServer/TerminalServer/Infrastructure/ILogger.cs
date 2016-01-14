#region

using System;

#endregion

namespace UlteriusServer.TerminalServer.Infrastructure
{
    public interface ILogger
    {
        bool IsDebugEnabled { get; }

        void Debug(string format, params object[] args);
        string Error(string format, params object[] args);
        string Error(string message, Exception exception);
        void Fatal(string format, params object[] args);
        void Fatal(string message, Exception exception);
        void Info(string format, params object[] args);
        void Info(string message);
        void Warn(string format, params object[] args);
        string Warn(string message, Exception exception);
        string Warn(string message, string controller, string action, Exception error);
    }
}