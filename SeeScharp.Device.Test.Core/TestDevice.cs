using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeeScharp.Device.Test.Core
{
    public abstract class TestDevice
    {
        public abstract void Initial();
        public abstract void Run();
        public abstract void Flush();
    }
}
