using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using SeeScharp.Device.Test.Core.Device.PCI;

namespace SeeScharp.Device.Test.Runner.Runner.PCI
{
    public class PCI62205TestRunner:Runner
    {
        public PCI62205TestRunner()
        {
            this.logger = LogManager.GetCurrentClassLogger();
            this.device=new PCI62205Test();
        }
        public override void Run()
        {
            this.logger.Info("初始化");
            this.device.Initial();
            logger.Info("AI0,AI31,AI63接入信号源CH1，EXTDTRIG接入信号源CH2");
            this.device.Run();
        }
    }
}
