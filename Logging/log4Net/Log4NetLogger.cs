using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging; // ILogger
using System.Xml; // XmlElement
using System.Text;

namespace NZ01
{
    public class Log4NetLogger : ILogger
    {
        private readonly Log4NetAsyncLog _log;

        public static int CountCalls_InstanceCtor { get; set; } = 0;
        public static int CountCalls_BeginScope { get; set; } = 0;
        public static int CountCalls_IsEnabled { get; set; } = 0;
        public static int CountCalls_Log { get; set; } = 0;

        public Log4NetLogger(string name, XmlElement xmlElement)
        {
            ++CountCalls_InstanceCtor;
            _log = new Log4NetAsyncLog(name, xmlElement);
        }

        public IDisposable BeginScope<TState>(TState state) { ++CountCalls_BeginScope; return null; }

        public bool IsEnabled(LogLevel logLevel)
        {
            ++CountCalls_IsEnabled;
            switch (logLevel)
            {
                case LogLevel.Critical:
                    return _log.IsFatalEnabled;
                case LogLevel.Debug:
                case LogLevel.Trace:
                    return _log.IsDebugEnabled;
                case LogLevel.Error:
                    return _log.IsErrorEnabled;
                case LogLevel.Information:
                    return _log.IsInfoEnabled;
                case LogLevel.Warning:
                    return _log.IsWarnEnabled;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel));
            }
        }

        public static string MyFormatter<TState, Exception>(TState t, Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(t?.ToString());
            sb.Append(" ");
            if (ex != null)
            {
                sb.Append(ex.ToString());
            }

            return sb.ToString();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            ++CountCalls_Log;

            if (!IsEnabled(logLevel)) { return; }

            if (formatter == null) { throw new ArgumentNullException(nameof(formatter)); }

            if (state != null || exception != null)
            {
                _log.Enqueue(logLevel, eventId, (object)state, exception, formatter, state.GetType());
            }
        }
    }
}

