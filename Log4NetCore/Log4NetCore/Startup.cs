using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NZ01; // #ADDITION

namespace Log4NetCore
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddLog4Net(); // #ADDITION

            if (env.IsDevelopment()) { app.UseDeveloperExceptionPage(); }

            app.UseStatusCodePages();

            app.UseMvc(routes =>
            {
                routes.MapRoute(name: "controller-action", template: "{controller}/{action}"); // Exercise: http://localhost:64065/HelloWorld/Index or http://localhost:64065/LogLevel/Index
                routes.MapRoute(name: "default", template: "{controller=HelloWorld}/{action=Index}"); // Exercise: http://localhost:64065
            });
        }
    }
}
