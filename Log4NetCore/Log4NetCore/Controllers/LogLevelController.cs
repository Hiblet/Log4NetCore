using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Log4NetCore.ViewModels;
using Microsoft.Extensions.Logging;


namespace Log4NetCore.Controllers 
{
    /// <summary>
    /// An example class to illustrate how to change Log Level in the running.
    /// </summary>
    /// <remarks>
    /// The critical point is that the level is set by a call to the static function:
    ///     NZ01.Log4NetManager.SetLogLevel(rbLogLevel);
    /// How you achieve this is up to you.  Personally I would expose an API endpoint
    /// and allow Admin privilege users to call with a string, or create one endpoint
    /// per level so that changing log level is as simple as visiting a page.
    /// </remarks>
    public class LogLevelController : Controller
    {
        private readonly ILogger _logger;
        private readonly string _fnSuffix = "() - ";

        public LogLevelController(ILogger<LogLevelController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public ViewResult Index()
        {
            string prefix = nameof(Index) + _fnSuffix+  "[GET] - ";
            _logger.LogInformation(prefix + "Entering");

            LogLevelViewModel llvm = new LogLevelViewModel();

            // Get Log Level
            int iLogLevel = NZ01.Log4NetManager.GetLogLevel();
            string sLogLevel = NZ01.Log4NetManager.ConvertIntLogLevelToString(iLogLevel);

            llvm.LogLevelInt = iLogLevel;
            llvm.LogLevelString = sLogLevel;
            llvm.LogLevels = new List<string> { "DEBUG", "INFO", "WARN", "ERROR", "FATAL" };

            // Write a message at every level.  As you change the level in the POST function
            // below, this method is then called and you should see in the log file that 
            // messages below the current level are not written.
            // Copy the log file before opening it, so you do not lock it whilst the app is using it.


            _logger.LogCritical(prefix + $"[START] Current Log Level: {sLogLevel} ({iLogLevel})"); // Log at critical level to ensure this line is always written.

            _logger.LogTrace(prefix + "Trace level message");
            _logger.LogDebug(prefix + "Debug level message");
            _logger.LogInformation(prefix + "Information level message");
            _logger.LogWarning(prefix + "Warning level message");
            _logger.LogError(prefix + "Error level message");
            _logger.LogCritical(prefix + "Critical or Fatal level message");

            _logger.LogCritical(prefix + "[FINISH]"); // Log at critical level to ensure this line is always written.


            return View("Index", llvm);
        }

        [HttpPost]
        public ViewResult Index(string rbLogLevel)
        {
            string prefix = nameof(Index) + _fnSuffix + "[POST] - ";
            _logger.LogInformation(prefix + "Entering");

            // Set Log Level
            NZ01.Log4NetManager.SetLogLevel(rbLogLevel);

            return Index();
        }

    }
}
