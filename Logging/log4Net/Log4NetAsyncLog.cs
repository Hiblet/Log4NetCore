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
        private static ConcurrentDictionary<string, Log4NetAsyncLog> _registry = new ConcurrentDictionary<string, Log4NetAsyncLog>();

        // Instance Members
        private log4net.ILog _logger;

        private Thread _thread;
        private bool _running = true;
        private AutoResetEvent _eventThreadExit = new AutoResetEvent(false);
        private AutoResetEvent _eventThreadAction = new AutoResetEvent(false);
        private int _waitTimeout = 1000; // 1 second - Effects responsiveness to shutdown

        private string _name = "NAME_NOT_DEFINED";
        private readonly XmlElement _xmlElement;
        private ILoggerRepository _loggerRepository;

        private bool _bConsoleWrite = false;

        private ConcurrentQueue<Log4NetAsyncQueueWrapper> _queue = new ConcurrentQueue<Log4NetAsyncQueueWrapper>();
        private static string _qSizeFormatter = "D8";

        private int _thresholdLevelWarn = 10000; // Warn but accept messages if this number of messages is on the queue
        private bool _bLevelWarnPassed = false;
        private int _thresholdLevelError = 1000000; // Error and reject messages if this number of messages is on the queue
        private bool _bLevelErrorPassed = false;

        // Instance Ctor
        public Log4NetAsyncLog(string name, XmlElement xmlElement, bool bConsoleWrite = false)
        {
            var prefix = "Log4NetAsyncLog() [INSTANCE CTOR] - ";
            _bConsoleWrite = bConsoleWrite;

            _name = name;
            _xmlElement = xmlElement;
            _loggerRepository = log4net.LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy));

            log4net.Config.XmlConfigurator.Configure(_loggerRepository, xmlElement);

            _logger = log4net.LogManager.GetLogger(_loggerRepository.Name, name);

            setQSizeFormatter();

            _registry.GetOrAdd(name, this); // record this log file for direct access

            _thread = new Thread(new ThreadStart(RunThread));
            _thread.Name = name;

            _logger.Info(prefix + $"About to start {name} logging Thread...");
            _thread.Start();
            _logger.Info(prefix + $"Thread {name} started...");
        }

        // Instance Dtor
        ~Log4NetAsyncLog()
        {
            Stop();
        }

        public void Stop()
        {
            var prefix = "Stop() - ";

            _running = false; // Stops the thread wait infinite loop

            if (_eventThreadExit.WaitOne())
                _logger.Info(prefix + $"Graceful Shutdown - Log Thread [{_name}] has signalled that it has stopped.");
            else
                _logger.Info(prefix + $"Bad Shutdown - Log Thread [{_name}] did not signal that it had stopped.");
        }


        public static Log4NetAsyncLog GetLog4NetAsyncLogByName(string name)
        {
            Log4NetAsyncLog target = null;
            if (_registry.TryGetValue(name, out target))
                return target;
            return null;
        }


        ////////////////////
        // Thread Function

        private void RunThread()
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

        private void consumeQueue()
        {
            Log4NetAsyncQueueWrapper wrapper = null;
            while (_queue.TryDequeue(out wrapper))
            {
                processQueuedItem(wrapper, _queue.Count);
            }
        }


        private void processQueuedItem(Log4NetAsyncQueueWrapper wrapper, int countQueued)
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


        public bool IsFatalEnabled { get { return _logger.IsFatalEnabled; } }
        public bool IsDebugEnabled { get { return _logger.IsDebugEnabled; } }
        public bool IsErrorEnabled { get { return _logger.IsErrorEnabled; } }
        public bool IsInfoEnabled { get { return _logger.IsInfoEnabled; } }
        public bool IsWarnEnabled { get { return _logger.IsWarnEnabled; } }

        public void Enqueue(LogLevel loglevel, EventId eventid, object tstate, Exception ex, Delegate formatter, Type tstatetype)
        {
            int countQueued = _queue.Count;
            if (checkCapacity(countQueued))
            {
                Log4NetAsyncQueueWrapper wrapper = new Log4NetAsyncQueueWrapper(loglevel, eventid, tstate, ex, formatter, tstatetype, getEnqueueData());
                _queue.Enqueue(wrapper);
                _eventThreadAction.Set();
            }
        }

        public void Fatal(object message, Exception ex = null) { Enqueue(LogLevel.Critical, 0, message, null, null, typeof(String)); } 
        public void Debug(object message, Exception ex = null) { Enqueue(LogLevel.Debug, 0, message, null, null, typeof(String)); }
        public void Info(object message, Exception ex = null) { Enqueue(LogLevel.Information, 0, message, null, null, typeof(String)); }
        public void Warn(object message, Exception ex = null) { Enqueue(LogLevel.Warning, 0, message, null, null, typeof(String)); }
        public void Error(object message, Exception ex = null) { Enqueue(LogLevel.Error, 0, message, null, null, typeof(String)); }

        private string getEnqueueData()
        {
            string sThreadID = string.Format("NQTHR={0},", Thread.CurrentThread.ManagedThreadId.ToString("D3"));
            string sCount = string.Format("NQ={0},", _queue.Count.ToString(_qSizeFormatter));
            string sTime = string.Format("NQUTC={0},", DateTime.UtcNow.ToString(_dateTimeFormatter));
            string sThreadName = (Thread.CurrentThread.Name == null) ? "" : string.Format("THRNM={0},", Thread.CurrentThread.Name);

            return sCount + sTime + sThreadID + sThreadName + _name + ",";
        }

        private bool checkCapacity(int countQueued)
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

        public bool WarnFlag(bool value)
        {
            _bLevelWarnPassed = value;
            return _bLevelWarnPassed;
        }

        public bool ErrorFlag(bool value)
        {
            _bLevelErrorPassed = value;
            return _bLevelErrorPassed;
        }



        ///////////////////////////
        // Get/Set Warning Levels

        public int ThresholdLevelWarn(int qSize)
        {
            _thresholdLevelWarn = qSize;
            return _thresholdLevelWarn;
        }

        public int ThresholdLevelWarn()
        { return _thresholdLevelWarn; }

        public int ThresholdLevelError(int qSize)
        {
            _thresholdLevelError = qSize;
            setQSizeFormatter();
            return _thresholdLevelError;
        }

        public int ThresholdLevelError()
        { return _thresholdLevelError; }

        private void setQSizeFormatter()
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
