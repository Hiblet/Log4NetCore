using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Log4NetCore.ViewModels
{
    public class LogLevelViewModel
    {
        public int LogLevelInt { get; set; }
        public string LogLevelString { get; set; }
        public IEnumerable<string> LogLevels { get; set; }
    }
}
