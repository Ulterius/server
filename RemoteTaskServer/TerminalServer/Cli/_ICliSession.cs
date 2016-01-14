#region

using System;

#endregion

namespace UlteriusServer.TerminalServer.Cli
{
    public interface ICliSession : IDisposable
    {
        string Type { get; }
        string CurrentPath { get; }
        Action<string, int, bool> Output { get; set; }
        void Input(string value, int commandCorrelationId);
    }
}