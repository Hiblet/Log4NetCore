using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging; // ILoggerProvider 
using System.Xml; // XmlElement
using System.IO; // File
using System.Collections.Concurrent;


namespace NZ01
{
    public class Log4NetProvider : ILoggerProvider
    {
        private Dictionary<string, Log4NetLogger> _registry = new Dictionary<string, Log4NetLogger>();
        private bool _bRunning = true;

        // DIAGS
        //public static int CountCalls_InstanceCtor { get; set; } = 0;
        //public static int CountCalls_CreateLogger { get; set; } = 0;
        //public static int CountCalls_Dispose { get; set; } = 0;


        // Ctor
        public Log4NetProvider()
        {
            //++CountCalls_InstanceCtor;
        }

        /// <summary>
        /// Create an instance of the Logger that holds the name of the class being logged.
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        public ILogger CreateLogger(string className)
        {
            //++CountCalls_CreateLogger;

            if (!_bRunning)
                return null;

            Log4NetLogger logger = null;
            if (_registry.TryGetValue(className, out logger))
            {
                return logger;
            }
            else
            {
                logger = new Log4NetLogger(className);
                _registry.Add(className, logger);
                return logger;
            }            
        }

        public void Dispose()
        {
            //++CountCalls_Dispose;
            _bRunning = false;
            _registry.Clear();
        }
    }    
}
