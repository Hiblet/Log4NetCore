using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace NZ01
{
    /// <summary>
    /// Example class intended for use with DI.
    /// The expectation is that the ServiceProvider will set the lifetime of the class.
    /// </summary>
    public class ExampleDependencyInjection
    {
        private readonly ILogger<ExampleDependencyInjection> _logger;

        public string InstanceMember { get; set; } = "INSTANCE";

        public ExampleDependencyInjection(ILogger<ExampleDependencyInjection> logger)
        {
            _logger = logger;
        }

        public void InstanceFunction(string whatever)
        {
            InstanceMember += whatever;

            _logger.LogDebug($"InstanceFunction() has set the current instance member string to {InstanceMember}");
        }
    }
}
