#region

using System;
using System.Diagnostics;
using log4net;

#endregion

namespace UlteriusServer.TerminalServer.Infrastructure
{
    public class Log4NetLogger : ILogger
    {
        private readonly ILog _logger = LogManager.GetLogger("Main");

        public bool IsDebugEnabled
        {
            get { return _logger.IsDebugEnabled; }
        }

        public void Debug(string format, params object[] args)
        {
            try
            {
                if (_logger.IsDebugEnabled)
                {
                    _logger.DebugFormat(format, args);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("LOG ERROR: " + ex.Message);
            }
        }

        public void Info(string message)
        {
            try
            {
                if (_logger.IsInfoEnabled)
                {
                    _logger.Info(message);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("LOG ERROR: " + ex.Message);
            }
        }

        public void Info(string format, params object[] args)
        {
            try
            {
                if (_logger.IsInfoEnabled)
                {
                    _logger.InfoFormat(format, args);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("LOG ERROR: " + ex.Message);
            }
        }

        public void Warn(string format, params object[] args)
        {
            try
            {
                if (_logger.IsWarnEnabled)
                {
                    _logger.WarnFormat(format, args);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("LOG ERROR: " + ex.Message);
            }
        }

        public string Warn(string message, Exception exception)
        {
            try
            {
                if (!_logger.IsWarnEnabled) return null;
                var guid = Guid.NewGuid().ToString();
                _logger.Warn(message, exception);
                return guid;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("LOG ERROR: " + ex.Message);
                return "[Cannot be logged]";
            }
        }

        public string Error(string format, params object[] args)
        {
            try
            {
                if (_logger.IsErrorEnabled)
                {
                    var guid = Guid.NewGuid().ToString();
                    _logger.ErrorFormat("[ErrorTicket:" + guid + "]" + format, args);
                    return guid;
                }
                return null;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("LOG ERROR: " + ex.Message);
                return "[Cannot be logged]";
            }
        }

        public string Error(string message, Exception exception)
        {
            try
            {
                if (_logger.IsErrorEnabled)
                {
                    var guid = Guid.NewGuid().ToString();
                    _logger.Error("[ErrorTicket:" + guid + "]" + message, exception);
                    return guid;
                }
                return null;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("LOG ERROR: " + ex.Message);
                return "[Cannot be logged]";
            }
        }

        public string Warn(string message, string controller, string action, Exception error)
        {
            return Warn($"[controller: {controller}] [action: {action}] : {message}", error);
        }

        public void Fatal(string format, params object[] args)
        {
            try
            {
                if (_logger.IsFatalEnabled)
                {
                    _logger.FatalFormat(format, args);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("LOG ERROR: " + ex.Message);
            }
        }

        public void Fatal(string message, Exception exception)
        {
            try
            {
                if (_logger.IsFatalEnabled)
                {
                    _logger.Fatal(message, exception);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("LOG ERROR: " + ex.Message);
            }
        }
    }
}