using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging; // ILoggerProvider 
using System.Xml; // XmlElement
using System.IO; // File


namespace NZ01
{
    public class Log4NetProvider : ILoggerProvider
    {
        private Log4NetLogger _logger;

        // DIAGS
        public static int CountCalls_InstanceCtor { get; set; } = 0;
        public static int CountCalls_CreateLogger { get; set; } = 0;
        public static int CountCalls_Dispose { get; set; } = 0;
        public static int CountCalls_parseLog4NetConfigFile { get; set; } = 0;


        // Ctor
        public Log4NetProvider(string log4NetConfigFile)
        {
            ++CountCalls_InstanceCtor;
            _logger = new Log4NetLogger("App", parseLog4NetConfigFile(log4NetConfigFile));
        }

        public ILogger CreateLogger(string categoryName)
        {
            ++CountCalls_CreateLogger;
            return _logger;
        }

        public void Dispose()
        {
            ++CountCalls_Dispose;            
            _logger = null;
        }

        private static XmlElement parseLog4NetConfigFile(string filename)
        {
            ++CountCalls_parseLog4NetConfigFile;
            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.Load(File.OpenRead(filename));
            return log4netConfig["log4net"];
        }
    }
}
