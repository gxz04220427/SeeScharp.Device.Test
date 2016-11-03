using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using SeeScharp.Device.Test.Core;

namespace SeeScharp.Device.Test.Runner.Runner
{
    public abstract class Runner
    {
        protected Logger logger;
        protected TestDevice device;
        public abstract void Run();
    }
}
