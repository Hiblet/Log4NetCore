using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using log4net.Repository; // ILoggerRepository
using Microsoft.Extensions.Logging; // ILogger
using System.Xml; // XmlElement
using System.Reflection; // Assembly

using System.Collections.Concurrent;
using System.Threading;
using System.IO;

namespace NZ01
{
    /// <summary>
    /// A parcelling object to allow us to enqueue data to log
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    public class Log4NetAsyncQueueWrapper
    {
        public LogLevel LogLevel { get; set; }
        public EventId EventId { get; set; }
        public object TStateObject { get; set; }
        public Exception Exception { get; set; }
        public Delegate FormatterFn { get; set; }
        public Type TStateType { get; set; }
        public string EnqueueData { get; set; }

        public Log4NetAsyncQueueWrapper(LogLevel loglevel, EventId eventid, object tStateObject, Exception ex, Delegate formatter, Type type, string nq)
        {
            LogLevel = loglevel;
            EventId = eventid;
            TStateObject = tStateObject;
            Exception = ex;
            FormatterFn = formatter;
            TStateType = type;
            EnqueueData = nq;
        }
    }

    public class Log4NetAsyncLog
    {
        // Enum
        public enum Log4NetAsyncLogLevel { UNKNOWN = -1, UNUSED0 = 0, UNUSED1 = 1, FATAL = 2, ERROR = 3, WARN = 4, INFO = 5, DEBUG = 6, MAX = 7 };


        // Static Members
        private static string _dateTimeFormatter = "HH:mm:ss.fff";

        private static log4net.ILog _logger;

        private static Thread _thread;
        private static bool _running = true;
        private static AutoResetEvent _eventThreadExit = new AutoResetEvent(false);
        private static AutoResetEvent _eventThreadAction = new AutoResetEvent(false);
        private static int _waitTimeout = 1000; // 1 second - Effects responsiveness to shutdown

        private static ConcurrentQueue<Log4NetAsyncQueueWrapper> _queue = new ConcurrentQueue<Log4NetAsyncQueueWrapper>();
        private static string _qSizeFormatter = "D8";

        private static int _thresholdLevelWarn = 10000; // Warn but accept messages if this number of messages is on the queue
        private static bool _bLevelWarnPassed = false;
        private static int _thresholdLevelError = 1000000; // Error and reject messages if this number of messages is on the queue
        private static bool _bLevelErrorPassed = false;


        // Static Ctor
        static Log4NetAsyncLog()
        {
            var prefix = "Log4NetAsyncLog() [STATIC CTOR] - ";

            var loggerRepository = log4net.LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy));

            var xmlElement = parseLog4NetConfigFile("./Logging/log4Net/log4net.config");
            if (!loggerRepository.Configured)            
                log4net.Config.XmlConfigurator.Configure(loggerRepository, xmlElement);
            
            _logger = log4net.LogManager.GetLogger(loggerRepository.Name,"App");

            setQSizeFormatter();

            _thread = new Thread(new ThreadStart(RunThread));
            _thread.Name = "Log4Net";
            _thread.IsBackground = true; // Thread will be stopped like a pool thread if app is closed.

            _logger.Info(prefix + $"About to start Logging Thread...");
            _thread.Start();
            _logger.Info(prefix + $"Logging Thread started...");
        }


        public static void Stop()
        {
            var prefix = "Stop() - ";

            _running = false; // Stops the thread wait infinite loop

            if (_eventThreadExit.WaitOne())
                _logger.Info(prefix + $"Graceful Shutdown - Log Thread has signalled that it has stopped.");
            else
                _logger.Info(prefix + $"Bad Shutdown - Log Thread did not signal that it had stopped.");
        }




        ////////////////////
        // Thread Function

        private static void RunThread()
        {
            while (_running)
            {
                if (_eventThreadAction.WaitOne(_waitTimeout))
                    consumeQueue(); // Signal Received                
            }

            // Signal that the thread has exited.
            _eventThreadExit.Set();
        }



        ///////////////////////
        // Internal Mechanics

        private static XmlElement parseLog4NetConfigFile(string filename)
        {
            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.Load(File.OpenRead(filename));
            return log4netConfig["log4net"];
        }

        private static void consumeQueue()
        {
            Log4NetAsyncQueueWrapper wrapper = null;
            while (_queue.TryDequeue(out wrapper))
            {
                processQueuedItem(wrapper, _queue.Count);
            }
        }


        private static void processQueuedItem(Log4NetAsyncQueueWrapper wrapper, int countQueued)
        {
            
            Type tType = wrapper.TStateType;
            var t = Convert.ChangeType(wrapper.TStateObject, tType);

            string msg = string.Format("DQ={0},EVT={1},", countQueued.ToString(_qSizeFormatter), wrapper.EventId.Id.ToString("D4"));
            msg += wrapper.EnqueueData;

            if (wrapper.FormatterFn != null)
            {
                msg += (string)wrapper.FormatterFn.DynamicInvoke(t, wrapper.Exception);
            }
            else
            {
                msg += t.ToString();
            }

            switch (wrapper.LogLevel)
            {
                case LogLevel.Debug:
                case LogLevel.Trace:
                    _logger.Debug(msg, wrapper.Exception);
                    break;
                case LogLevel.Information:
                    _logger.Info(msg, wrapper.Exception);
                    break;
                case LogLevel.Warning:
                    _logger.Warn(msg, wrapper.Exception);
                    break;
                case LogLevel.Error:
                    _logger.Error(msg, wrapper.Exception);
                    break;
                case LogLevel.Critical:
                    _logger.Fatal(msg, wrapper.Exception);
                    break;
                default:
                    msg = $"[Unrecognised logLevel {wrapper.LogLevel}] " + msg;
                    _logger.Error(msg, wrapper.Exception);
                    break;
            }

        }


        public static bool IsFatalEnabled { get { return _logger.IsFatalEnabled; } }
        public static bool IsDebugEnabled { get { return _logger.IsDebugEnabled; } }
        public static bool IsErrorEnabled { get { return _logger.IsErrorEnabled; } }
        public static bool IsInfoEnabled { get { return _logger.IsInfoEnabled; } }
        public static bool IsWarnEnabled { get { return _logger.IsWarnEnabled; } }

        public static void Enqueue(LogLevel loglevel, EventId eventid, object tstate, Exception ex, Delegate formatter, Type tstatetype, string name = "!")
        {
            int countQueued = _queue.Count;
            if (checkCapacity(countQueued))
            {
                Log4NetAsyncQueueWrapper wrapper = new Log4NetAsyncQueueWrapper(loglevel, eventid, tstate, ex, formatter, tstatetype, getEnqueueData(name));
                _queue.Enqueue(wrapper);
                _eventThreadAction.Set();
            }
        }

        public static void Fatal(object message, Exception ex = null) { Enqueue(LogLevel.Critical, 0, message, null, null, typeof(String)); } 
        public static void Debug(object message, Exception ex = null) { Enqueue(LogLevel.Debug, 0, message, null, null, typeof(String)); }
        public static void Info(object message, Exception ex = null) { Enqueue(LogLevel.Information, 0, message, null, null, typeof(String)); }
        public static void Warn(object message, Exception ex = null) { Enqueue(LogLevel.Warning, 0, message, null, null, typeof(String)); }
        public static void Error(object message, Exception ex = null) { Enqueue(LogLevel.Error, 0, message, null, null, typeof(String)); }

        private static string getEnqueueData(string name)
        {
            string sThreadID = string.Format("NQTHR={0},", Thread.CurrentThread.ManagedThreadId.ToString("D3"));
            string sCount = string.Format("NQ={0},", _queue.Count.ToString(_qSizeFormatter));
            string sTime = string.Format("NQUTC={0},", DateTime.UtcNow.ToString(_dateTimeFormatter));
            string sThreadName = (Thread.CurrentThread.Name == null) ? "" : string.Format("THRNM={0},", Thread.CurrentThread.Name);

            return sCount + sTime + sThreadID + sThreadName + name + ",";
        }

        private static bool checkCapacity(int countQueued)
        {
            var prefix = "checkCapacity() - ";

            if (countQueued > _thresholdLevelWarn && !_bLevelWarnPassed)
            {
                _bLevelWarnPassed = true;
                string msg = string.Format("The logging message queue has passed {0} messages.", _thresholdLevelWarn);
                if (_logger != null)
                    _logger.Warn(prefix + msg);
            }

            if (countQueued > _thresholdLevelError)
            {
                if (!_bLevelErrorPassed)
                {
                    _bLevelErrorPassed = true;
                    string msg = string.Format("The logging message queue has passed {0} messages.", _thresholdLevelError);
                    if (_logger != null)
                        _logger.Error(prefix + msg);
                }

                return false; // Do not add this message to queue
            }

            return true; // Queue message
        }

        public static bool WarnFlag(bool value)
        {
            _bLevelWarnPassed = value;
            return _bLevelWarnPassed;
        }

        public static bool ErrorFlag(bool value)
        {
            _bLevelErrorPassed = value;
            return _bLevelErrorPassed;
        }



        ///////////////////////////
        // Get/Set Warning Levels

        public static int ThresholdLevelWarn(int qSize)
        {
            _thresholdLevelWarn = qSize;
            return _thresholdLevelWarn;
        }

        public static int ThresholdLevelWarn()
        { return _thresholdLevelWarn; }

        public static int ThresholdLevelError(int qSize)
        {
            _thresholdLevelError = qSize;
            setQSizeFormatter();
            return _thresholdLevelError;
        }

        public static int ThresholdLevelError()
        { return _thresholdLevelError; }

        private static void setQSizeFormatter()
        {
            double dLogTen = Math.Log10((double)_thresholdLevelError);
            int iLogTen = (int)Math.Ceiling(dLogTen);
            if (iLogTen <= 0)
                _qSizeFormatter = "D4";
            else
                _qSizeFormatter = "D" + iLogTen.ToString(); // eg "D8"
        }
    }
}
