using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging; // ILogger
using System.Text;

namespace NZ01
{
    public class Log4NetLogger : ILogger
    {
        private string _className;

        //public static int CountCalls_InstanceCtor { get; set; } = 0;
        //public static int CountCalls_BeginScope { get; set; } = 0;
        //public static int CountCalls_IsEnabled { get; set; } = 0;
        //public static int CountCalls_Log { get; set; } = 0;

        public Log4NetLogger(string className)
        {
            //++CountCalls_InstanceCtor;
            _className = className;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            //++CountCalls_BeginScope;
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            //++CountCalls_IsEnabled;

            switch (logLevel)
            {
                case LogLevel.Critical:
                    return Log4NetAsyncLog.IsFatalEnabled;
                case LogLevel.Debug:
                case LogLevel.Trace:
                    return Log4NetAsyncLog.IsDebugEnabled;
                case LogLevel.Error:
                    return Log4NetAsyncLog.IsErrorEnabled;
                case LogLevel.Information:
                    return Log4NetAsyncLog.IsInfoEnabled;
                case LogLevel.Warning:
                    return Log4NetAsyncLog.IsWarnEnabled;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel));
            }
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            //++CountCalls_Log;

            if (!IsEnabled(logLevel)) { return; }

            if (formatter == null) { throw new ArgumentNullException(nameof(formatter)); }

            if (state != null || exception != null)
            {
                Log4NetAsyncLog.Enqueue(
                    logLevel, eventId, (object)state, exception, 
                    formatter, state.GetType(), _className);
            }
        }

        // Used in testing Log<T> method
        public static string MyFormatter<TState, Exception>(TState t, Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(t?.ToString());
            sb.Append(" ");

            if (ex != null)
                sb.Append(ex.ToString()); 

            return sb.ToString();
        }

    }
}

