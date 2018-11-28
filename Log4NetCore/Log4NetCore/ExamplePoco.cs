using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NZ01
{
    /// <summary>
    /// Class to illustrate how to log in a simple class without using Dependency Injection
    /// </summary>
    public class ExamplePoco
    {
        public string InstanceMember { get; set; } = "INSTANCE";
        public static string StaticMember { get; set; } = "STATIC";

        // Static Ctor
        static ExamplePoco()
        {
            NZ01.Log4NetAsyncLog.Debug("Static Constructor has been run");
        } 

        // Instance Ctor
        public ExamplePoco()
        {
            NZ01.Log4NetAsyncLog.Debug("Instance Constructor has been run");
        }

        // Example Instance Function
        public void InstanceFunction(string whatever)
        {
            InstanceMember += whatever;

            NZ01.Log4NetAsyncLog.Info($"InstanceFunction() has set the current instance member string to {InstanceMember}");
        }

        // Example Static Function
        public static void StaticFunction(string whatever)
        {
            StaticMember += whatever;

            NZ01.Log4NetAsyncLog.Info($"StaticFunction() has set the current static member string to {StaticMember}");
        }
    }
}
