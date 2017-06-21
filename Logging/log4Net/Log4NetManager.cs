using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using log4net;
using System.Reflection;

namespace NZ01
{
    /// <summary>
    /// Class to allow changing logging level in the running.
    /// </summary>
    public static class Log4NetManager
    {
        public static int SetLogLevel(string level)
        {
            log4net.Core.Level logLevel = lookupLogLevelFromString(level);
            return setLevel(logLevel);
        }

        public static int SetLogLevel(int iLevel)
        {
            log4net.Core.Level logLevel = lookupLogLevelFromInt(iLevel);
            return setLevel(logLevel);
        }

        public static int GetLogLevel()
        {
            log4net.Repository.Hierarchy.Hierarchy repository
                = ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository(Assembly.GetEntryAssembly()));

            if (repository == null)
                return -1;

            return lookupIntFromLogLevel(repository.Root.Level);
        }

        /// <summary>
        /// Set the log level as specified and signal that a change has occurred.
        /// </summary>
        /// <param name="level">log4net.Core.Level; Level Enum</param>
        private static int setLevel(log4net.Core.Level logLevel)
        {
            log4net.Repository.Hierarchy.Hierarchy repository
                = ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository(Assembly.GetEntryAssembly()));

            if (repository == null)
                return -1;

            repository.Root.Level = logLevel;

            repository.Configured = true;
            repository.RaiseConfigurationChanged(EventArgs.Empty);

            return lookupIntFromLogLevel(repository.Root.Level);
        }


        /// <summary>
        /// Convert the key integer to an Enum
        /// </summary>
        /// <param name="iLevel">int; Integer representing log level</param>
        /// <returns>log4net.Core.Level; Enum for log level</returns>
        private static log4net.Core.Level lookupLogLevelFromInt(int iLevel)
        {
            switch (iLevel)
            {
                case 6: return log4net.Core.Level.Debug;
                case 5: return log4net.Core.Level.Info;
                case 4: return log4net.Core.Level.Warn;
                case 3: return log4net.Core.Level.Error;
                case 2: return log4net.Core.Level.Fatal;
                default: return log4net.Core.Level.Info;
            }
        }

        private static log4net.Core.Level lookupLogLevelFromString(string level)
        {
            if (string.Equals("DEBUG", level, StringComparison.OrdinalIgnoreCase)) return log4net.Core.Level.Debug;

            if (string.Equals("INFO", level, StringComparison.OrdinalIgnoreCase)) return log4net.Core.Level.Info;
            if (string.Equals("INFORMATION", level, StringComparison.OrdinalIgnoreCase)) return log4net.Core.Level.Info;

            if (string.Equals("WARN", level, StringComparison.OrdinalIgnoreCase)) return log4net.Core.Level.Warn;
            if (string.Equals("WARNING", level, StringComparison.OrdinalIgnoreCase)) return log4net.Core.Level.Warn;

            if (string.Equals("ERROR", level, StringComparison.OrdinalIgnoreCase)) return log4net.Core.Level.Error;

            if (string.Equals("FATAL", level, StringComparison.OrdinalIgnoreCase)) return log4net.Core.Level.Fatal;
            if (string.Equals("CRITICAL", level, StringComparison.OrdinalIgnoreCase)) return log4net.Core.Level.Fatal;

            return log4net.Core.Level.Info; // Default
        }

        /// <summary>
        /// Convert log4net log level enum to key int
        /// </summary>
        /// <param name="logLevel">log4net.Core.Level; Enum for log level</param>
        /// <returns>int; Key integer code</returns>
        private static int lookupIntFromLogLevel(log4net.Core.Level logLevel)
        {
            if (logLevel == log4net.Core.Level.Debug)
                return 6;
            else if (logLevel == log4net.Core.Level.Info)
                return 5;
            else if (logLevel == log4net.Core.Level.Warn)
                return 4;
            else if (logLevel == log4net.Core.Level.Error)
                return 3;
            else if (logLevel == log4net.Core.Level.Fatal)
                return 2;
            else
                return 5;
        }

        public static string ConvertIntLogLevelToString(int iLevel)
        {
            switch (iLevel)
            {
                case 6: return "DEBUG";
                case 5: return "INFO";
                case 4: return "WARN";
                case 3: return "ERROR";
                case 2: return "FATAL";
                default: return "INFO";
            }
        }
        
    }

}
