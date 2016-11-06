#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UlteriusServer.TerminalServer.Infrastructure;
using UlteriusServer.Utilities.Extensions;

#endregion

namespace UlteriusServer.TerminalServer.Cli
{
    public class CommandSessionFactory : ICliSessionFactory
    {
        private readonly ILogger _log;

        public CommandSessionFactory(ILogger log)
        {
            _log = log;
        }

        public string Type
        {
            get { return ConsoleSession.TypeName; }
        }

        public ICliSession Create()
        {
            return new ConsoleSession(_log);
        }
    }

    public class ConsoleSession : ICliSession
    {
        private static readonly string _preCDID = "xx_ulterius_xx";
        private static readonly string _postCDID = "yy_ulterius_yy";
        public static readonly string TypeName = "cmd.exe";
        private readonly CancellationTokenSource _cancel;
        private readonly List<string> _errorBuffer;
        private readonly ILogger _log;

        private readonly Process _proc;
        private int _commandCorrelationId;
        private string _lastCommand;
        private bool _nextIsPath;

        public ConsoleSession(ILogger log)
        {
            _log = log;
            _cancel = new CancellationTokenSource();
            _errorBuffer = new List<string>();
            _proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardErrorEncoding = Encoding.GetEncoding(866),
                    StandardOutputEncoding = Encoding.GetEncoding(866),
                    WorkingDirectory = CurrentPath = Path.GetPathRoot(Environment.SystemDirectory)
                }
            };

            _proc.Start();

            Task.Run(ReadAsync);
            Task.Run(ReadErrorAsync);
        }

        public string Type => TypeName;

        public string CurrentPath { get; private set; }
        public Action<string, int, bool, bool> Output { get; set; }

        public void Input(string value, int commandCorrelationId)
        {
            if (value.ToLowerInvariant() == "exit")
            {
                _cancel.Cancel();
            }
            else if (_lastCommand != null)
            {
                _proc.StandardInput.WriteLine(value);
            }
            else
            {
                _commandCorrelationId = commandCorrelationId;
                _lastCommand = value + " & echo " + _preCDID + "& cd & echo " + _postCDID;
                _proc.StandardInput.WriteLine(_lastCommand);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Push(string line)
        {
            if (_nextIsPath)
            {
                CurrentPath = line;
                _nextIsPath = false;
            }
            else if (line == _preCDID)
                _nextIsPath = true;
            else if (line == _postCDID)
            {
                Output(string.Empty, _commandCorrelationId, _errorBuffer.Count == 0, false);
                _lastCommand = null;
                if (_errorBuffer.Count != 0)
                {
                    for (var i = 0; i < _errorBuffer.Count; i++)
                        Output(_errorBuffer[i], _commandCorrelationId, i == _errorBuffer.Count - 1, false);
                    _errorBuffer.Clear();
                }
            }
            else if (_lastCommand != null && line.EndsWith(_lastCommand))
            {
            }
            else if (Output != null && !string.IsNullOrWhiteSpace(line))
                Output(line, _commandCorrelationId, _lastCommand == null, false);
        }

     


   
        private async Task ReadAsync()
        {
            while (!_cancel.IsCancellationRequested && !_proc.HasExited)
            {
                try
                {
                    var line = await _proc.StandardOutput.ReadLineAsync().ConfigureAwait(false);
                    if (line != null)
                    {
                        Push(line.UnicodeUtf8());
                    }
                    Thread.Sleep(20);
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception ex)
                {
                    _log.Error("cmd.exe session error", ex);
                    _cancel.Cancel();
                }
            }
        }

        private async Task ReadErrorAsync()
        {
            while (!_cancel.IsCancellationRequested && !_proc.HasExited)
            {
                try
                {
                    var line = await _proc.StandardError.ReadLineAsync().ConfigureAwait(false);
                    if (line != null)
                    {
                        _errorBuffer.Add(line.UnicodeUtf8());
                    }
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception ex)
                {
                    _log.Error("cmd.exe session error", ex);
                    _cancel.Cancel();
                }
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
                GC.SuppressFinalize(this);

            _proc.Dispose();
            _cancel.Cancel();
        }

        ~ConsoleSession()
        {
            Dispose(false);
        }
    }
}