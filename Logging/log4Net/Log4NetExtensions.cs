using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace NZ01
{
    public static class Log4netExtensions
    {
        public static int CountCalls_AddLog4Net1 { get; set; } = 0;
        public static int CountCalls_AddLog4Net2 { get; set; } = 0;


        public static ILoggerFactory AddLog4Net(this ILoggerFactory factory)
        {
            ++CountCalls_AddLog4Net1;
            factory.AddProvider(new Log4NetProvider("./Logging/log4Net/log4net.config"));
            return factory;
        }

        public static ILoggerFactory AddLog4Net(this ILoggerFactory factory, string log4NetConfigFile)
        {
            ++CountCalls_AddLog4Net2;
            factory.AddProvider(new Log4NetProvider(log4NetConfigFile));
            return factory;
        }
    }
}
