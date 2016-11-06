#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using UlteriusServer.TerminalServer.Infrastructure;
using UlteriusServer.Utilities.Extensions;

#endregion

namespace UlteriusServer.TerminalServer.Cli
{
    public class PowerShellFactory : ICliSessionFactory
    {
        private readonly ILogger _log;

        public PowerShellFactory(ILogger log)
        {
            _log = log;
        }

        public string Type => "powershell";

        public ICliSession Create()
        {
            return new PowerShellSession(_log);
        }
    }

    public class PowerShellSession : ICliSession
    {
        private readonly ILogger _log;
        private readonly PowerShell _proc;
        private Action<string, int, bool, bool> _output;

        public PowerShellSession(ILogger log)
        {
            _proc = PowerShell.Create();
            _log = log;
            _proc.Commands.Clear();
            _proc.AddCommand("cd\\");
           
            _proc.Invoke();
            _proc.Commands.Clear();
            _proc.AddCommand("Get-Location");
            _proc.AddCommand("Out-String");
            CurrentPath = _proc.Invoke()
                .First()
                .ToString()
                .Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)[2].Trim();
        }

        public string Type => "powershell";

        public string CurrentPath { get; private set; }

        public Action<string, int, bool, bool> Output
        {
            get { return _output; }
            set
            {
                _output = value;
                _output("Welcome to Powershell (System.Management.Automation.dll)", 0, true, false);
            }
        }

        public void Input(string value, int commandCorrelationId)
        {
            var lines = new List<string>();
            try
            {
                _proc.Commands.Clear();
                _proc.AddCommand(value);
                _proc.AddCommand("Out-String");
                lines.AddRange(_proc.Invoke().Select(result => result.ToString().UnicodeUtf8()));
            }
            catch (Exception ex)
            {
                lines.Add(ex.Message);
            }
            _proc.Commands.Clear();
            _proc.AddCommand("Get-Location");
            _proc.AddCommand("Out-String");
            CurrentPath = _proc.Invoke()
                .First()
                .ToString()
                .Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)[2].Trim();

            foreach (var line in lines)
            {

                Output?.Invoke(line, commandCorrelationId, line == lines.Last(), false);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
                GC.SuppressFinalize(this);
            _proc.Dispose();
        }

        ~PowerShellSession()
        {
            Dispose(false);
        }
    }
}