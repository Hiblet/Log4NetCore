using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Log4NetCore.Models;
using Log4NetCore.ViewModels;
using Microsoft.Extensions.Logging;



namespace Log4NetCore.Controllers
{
    public class HelloWorldController : Controller
    {
        private readonly ILogger _logger;

        public HelloWorldController(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HelloWorldController>();
        }

        public ViewResult Index()
        {
            string prefix = "Index() - ";
            _logger.LogInformation(prefix + "Entering"); // Simple logging

            NZ01.Log4NetAsyncLog.Debug(prefix + "This message was quick because it avoided Microsoft's Extension facade and did not use Reflection.  It can be used from any class as the call is to a static function.");

            _logger.Log(LogLevel.Information, 1234, (prefix + "Exiting"), null, NZ01.Log4NetLogger.MyFormatter<string, Exception>); // Complex logging

            return View(new HelloWorldViewModel { Message = (new HelloWorld()).Timestamp });
        }


    }
}
