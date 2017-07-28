using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace NZ01
{
    public static class Log4netExtensions
    {
        public static int CountCalls_AddLog4Net { get; set; } = 0;

        public static ILoggerFactory AddLog4Net(this ILoggerFactory factory)
        {
            ++CountCalls_AddLog4Net;
            factory.AddProvider(new Log4NetProvider("App"));
            return factory;
        }
    }
}
