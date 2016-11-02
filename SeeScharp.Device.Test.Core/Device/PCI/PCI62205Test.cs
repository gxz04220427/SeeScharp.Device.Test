using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SeeScharp.Device.Test.Core.Task.Analog.AI;
using SeeScharp.Device.Test.Core.Task.Analog.AO;
using SeeSharpTools;
using JYPXI62205;
using RigolDG1032ZUSB;
using TekDPO2024BUSB;

namespace SeeScharp.Device.Test.Core.Device.PCI
{
    public class PCI62205Test:PCITest,IAITestTask,IAOTestTask
    {
        public SignalGenerator signalGenerator { get; set; }
         
        public override void Initial()
        {
            try
            {
                Tek_DPO2024B_USB SCOPE = new Tek_DPO2024B_USB();
                Rigol_DG1032Z_USB FWG = new Rigol_DG1032Z_USB();
                JYPXI62205AITask AITask = new JYPXI62205AITask(0);
            }
            catch (System.Exception ex)
            {
                return UUT_ErrorCode.Instrumenterror;
            }
        }

        public override void Run()
        {
            throw new NotImplementedException();
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public void AItest()
        {
            throw new NotImplementedException();
        }

        public void AOTest()
        {
            throw new NotImplementedException();
        }

       

        #region----------AI测试----------
        /// <summary>
        /// AI单通道单点采集验证
        /// </summary>
        /// <param name="boardNum">板卡号</param>
        /// <returns>验证状态</returns>
        public static bool AI_SingleMode_SingleChannel(int boardNum)
        {
            Rigol_DG1032Z_USB FWG = new Rigol_DG1032Z_USB();
            JYPXI62205AITask AITask = new JYPXI62205AITask(boardNum);
            AITask.Mode = EnumAIMode.Single;
            err = AITask.AddChannel(0, -10, 10, JYPXI62205.EnumCoupling.DC, EnumAITerminalConfig.RSE);
            for (int i = 0; i < 5; i++)
            {
                FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH1, 10 - i * (10 / 2));
                FWG.Start();
                Thread.Sleep(1000);
                err = AITask.Start();
                err = AITask.ReadSinglePoint(ref Singledata);
                if (Math.Abs(Singledata - (10 - i * (10 / 2))) > 0.1)
                {
                    return false;
                }
                err = AITask.Stop();
            }
            FWG.Stop();
            return true;
        }

        /// <summary>
        /// AI单通道有限点验证
        /// </summary>
        /// <param name="boardNum">板号</param>
        /// <param name="Amp">峰峰值</param>
        /// <param name="Type">波形类型</param>
        /// <param name="sampleRate">采样率</param>
        /// <param name="samplesPerChannel">采样点数</param>
        /// <returns>验证状态</returns>
        public static bool AI_Finite_SingleChannel(int boardNum, double Amp, Rigol_DG1032Z_USB.Function Type, int sampleRate, int samplesPerChannel, double LowLevel = -10.0, double HighLevel = 10.0)
        {
            double[] Findata = new double[samplesPerChannel];

            Rigol_DG1032Z_USB FWG = new Rigol_DG1032Z_USB();
            JYPXI62205AITask AITask = new JYPXI62205AITask(boardNum);
            AITask.SampleRate = sampleRate;
            AITask.SamplesToAcquire = samplesPerChannel;
            AITask.Mode = EnumAIMode.Finite;
            err = AITask.AddChannel(0, LowLevel, HighLevel, JYPXI62205.EnumCoupling.DC, EnumAITerminalConfig.RSE);
            FWG.SetChannel(Rigol_DG1032Z_USB.Channal.CH1, Type, sampleRate / 10, Amp);
            FWG.Start();
            Thread.Sleep(2000);
            err = AITask.Start();
            err = AITask.ReadData(ref Findata, samplesPerChannel, false, -1);
            err = AITask.Stop();
            FWG.Stop();
            return GetVerifyState(Findata, sampleRate, Amp);
        }

        /// <summary> 
        /// AI单通道连续采集，该方法可设耦合、端接方式，用于测量不同的硬件连接
        /// </summary>
        /// <param name="boardNum">板卡号</param>
        /// <param name="Amp">峰峰值</param>
        /// <param name="Type">波形类型</param>
        /// <param name="sampleRate">采样率</param>
        /// <param name="samplesPerChannel">采样点数</param>
        /// <param name="Cycle">循环读取次数</param>
        /// <param name="LowLevel">量程</param>
        /// <param name="HighLevel">量程</param>
        /// <param name="Coup">耦合方式</param>
        /// <param name="Terminal">端接方式</param>
        /// <returns>验证状态</returns>
        public static bool AI_Continuouse_SignleChannel(int boardNum, double Amp, Rigol_DG1032Z_USB.Function Type, int sampleRate, int samplesPerChannel, int Cycle, double LowLevel = -10.0, double HighLevel = 10.0,
            JYPXI62205.EnumCoupling Coup = JYPXI62205.EnumCoupling.DC, EnumAITerminalConfig Terminal = EnumAITerminalConfig.RSE)
        {
            double[] Condata = new double[samplesPerChannel];

            Rigol_DG1032Z_USB FWG = new Rigol_DG1032Z_USB();
            JYPXI62205AITask AITask = new JYPXI62205AITask(boardNum);
            AITask.SampleRate = sampleRate;
            AITask.SamplesToAcquire = samplesPerChannel;
            AITask.Mode = EnumAIMode.Continuous;
            err = AITask.AddChannel(0, LowLevel, HighLevel, Coup, Terminal);
            FWG.SetChannel(Rigol_DG1032Z_USB.Channal.CH1, Type, sampleRate / 10, Amp);
            FWG.Start();
            Thread.Sleep(2000);
            err = AITask.Start();
            for (int i = 0; i < Cycle; i++)
            {
                Thread.Sleep(100);
                err = AITask.ReadData(ref Condata, samplesPerChannel, false, -1);
            }
            err = AITask.Stop();
            FWG.Stop();
            return GetVerifyState(Condata, sampleRate, Amp);
        }

        /// <summary>
        /// AI全通道循环采集，0、31、63通道接入信号，验证该三通道采集是否正确
        /// </summary>
        /// <param name="boardNum">板卡号</param>
        /// <param name="Amp">峰峰值</param>
        /// <param name="Type">波形类型</param>
        /// <param name="sampleRate">采样率</param>
        /// <param name="samplesPerChannel">采样点数</param>
        /// <param name="Cycle">循环读取次数</param>
        /// <returns>验证状态</returns>
        public static bool AI_Continuouse_MultiChannel(int boardNum, double Amp, Rigol_DG1032Z_USB.Function Type, int sampleRate, int samplesPerChannel, int Cycle)
        {
            double[,] MulCondata = new double[samplesPerChannel, SEChannels];
            double[] Condata0 = new double[samplesPerChannel];
            double[] Condata31 = new double[samplesPerChannel];
            double[] Condata63 = new double[samplesPerChannel];
            double Frequency0 = 0.0;
            double Frequency31 = 0.0;
            double Frequency63 = 0.0;
            double Vpp0 = 0.0;
            double Vpp31 = 0.0;
            double Vpp63 = 0.0;

            Rigol_DG1032Z_USB FWG = new Rigol_DG1032Z_USB();
            JYPXI62205AITask AITask = new JYPXI62205AITask(boardNum);
            AITask.SampleRate = sampleRate;
            AITask.SamplesToAcquire = samplesPerChannel;
            AITask.Mode = EnumAIMode.Continuous;
            err = AITask.AddChannel(-1, -10, 10, JYPXI62205.EnumCoupling.Default, EnumAITerminalConfig.Default);
            FWG.SetChannel(Rigol_DG1032Z_USB.Channal.CH1, Type, sampleRate / 10, Amp);
            FWG.Start();
            Thread.Sleep(2000);
            err = AITask.Start();
            for (int i = 0; i < Cycle; i++)
            {
                Thread.Sleep(100);
                err = AITask.ReadData(ref MulCondata, samplesPerChannel, false, -1);
            }
            err = AITask.Stop();
            FWG.Stop();

            for (int i = 0; i < samplesPerChannel; i++)
            {
                Condata0[i] = MulCondata[i, 0];
            }
            GetFrequency(ref Frequency0, Condata0, sampleRate);
            Vpp0 = Condata0.Max() - Condata0.Min();
            for (int i = 0; i < samplesPerChannel; i++)
            {
                Condata31[i] = MulCondata[i, 31];
            }
            GetFrequency(ref Frequency31, Condata31, sampleRate);
            Vpp31 = Condata31.Max() - Condata31.Min();
            for (int i = 0; i < samplesPerChannel; i++)
            {
                Condata63[i] = MulCondata[i, 63];
            }
            GetFrequency(ref Frequency63, Condata63, sampleRate);
            Vpp63 = Condata63.Max() - Condata63.Min();
            if ((Math.Abs(Frequency0 - sampleRate / 10) > sampleRate / 10 * 0.1) || (Math.Abs(Vpp0 - Amp) > Amp * 0.2) || (Math.Abs(Frequency31 - sampleRate / 10) > sampleRate / 10 * 0.1)
                || (Math.Abs(Vpp31 - Amp) > Amp * 0.2) || (Math.Abs(Frequency63 - sampleRate / 10) > sampleRate / 10 * 0.1) || (Math.Abs(Vpp63 - Amp) > Amp * 0.2))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// AI各量程波形测试
        /// </summary>
        /// <param name="boardNum">板卡号</param>
        /// <param name="Type">波形类型</param>
        /// <returns>验证状态</returns>
        public static bool AI_Range(int boardNum, Rigol_DG1032Z_USB.Function Type)
        {
            bool state1, state2, state3, state4;
            state1 = AI_Finite_SingleChannel(boardNum, 10, Type, 10000, 1000, -10, 10);
            state2 = AI_Finite_SingleChannel(boardNum, 5, Type, 10000, 1000, -5, 5);
            state3 = AI_Finite_SingleChannel(boardNum, 2.5, Type, 10000, 1000, -2.5, 2.5);
            state4 = AI_Finite_SingleChannel(boardNum, 1.25, Type, 10000, 1000, -1.25, 1.25);
            Thread.Sleep(2000);
            if (state1 && state2 && state3 && state4)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// AI外部数字触发,触发类型PostTrigger
        /// </summary>
        /// <param name="boardNum">板卡号</param>
        /// <param name="Edge">触发边沿</param>
        /// <returns>验证状态</returns>
        public static bool AI_Digital_PostTrigger(int boardNum, JYPXI62205.EnumAIDigitalTrgEdge Edge)
        {
            double[] Findata = new double[1000];

            Rigol_DG1032Z_USB FWG = new Rigol_DG1032Z_USB();
            JYPXI62205AITask AITask = new JYPXI62205AITask(boardNum);
            AITask.SampleRate = 10000;
            AITask.SamplesToAcquire = 1000;
            AITask.Mode = EnumAIMode.Finite;
            err = AITask.AddChannel(0, -10, 10, JYPXI62205.EnumCoupling.Default, JYPXI62205.EnumAITerminalConfig.Default);
            AITask.TriggerParam.TriggerMode = EnumAITriggerMode.DigitalTrigger;
            AITask.TriggerParam.TriggerType = EnumAITriggerType.PostTrigger;
            AITask.TriggerParam.DigitialTriggerSettings.TriggerSrc = EnumAIDigitalTriggerSrc.ExtDigital;
            AITask.TriggerParam.DigitialTriggerSettings.TriggerEdge = Edge;
            FWG.SetChannel(Rigol_DG1032Z_USB.Channal.CH1, Rigol_DG1032Z_USB.Function.SIN, 1000, 10);
            Thread.Sleep(500);
            switch (Edge)
            {
                case EnumAIDigitalTrgEdge.Rising:
                    {
                        FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH2, 0);
                        FWG.Start();
                        err = AITask.Start();
                        Thread.Sleep(500);
                        FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH2, 3);
                        FWG.Start();
                        break;
                    }
                case EnumAIDigitalTrgEdge.Falling:
                    {
                        FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH2, 3);
                        FWG.Start();
                        err = AITask.Start();
                        Thread.Sleep(500);
                        FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH2, 0);
                        FWG.Start();
                        break;
                    }
            }
            err = AITask.ReadData(ref Findata, 1000, false, -1);
            err = AITask.Stop();
            FWG.Stop();
            return GetVerifyState(Findata, 10000, 10);
        }

        /// <summary>
        /// AI外部数字触发，触发类型DelayTrigger
        /// </summary>
        /// <param name="boardNum"></param>
        /// <param name="Edge"></param>
        /// <param name="Delay"></param>
        /// <returns></returns>
        public static bool AI_Digital_DelayTrigger(int boardNum, JYPXI62205.EnumAIDigitalTrgEdge Edge, int Delay)
        {
            double[] Findata = new double[1000];
            double DelayTime = 0.0;

            DateTime Time = DateTime.Now;
            Rigol_DG1032Z_USB FWG = new Rigol_DG1032Z_USB();
            JYPXI62205AITask AITask = new JYPXI62205AITask(boardNum);
            AITask.SampleRate = 10000;
            AITask.SamplesToAcquire = 1000;
            AITask.Mode = EnumAIMode.Finite;
            err = AITask.AddChannel(0, -10, 10, JYPXI62205.EnumCoupling.Default, JYPXI62205.EnumAITerminalConfig.Default);
            AITask.TriggerParam.TriggerMode = EnumAITriggerMode.DigitalTrigger;
            AITask.TriggerParam.TriggerType = EnumAITriggerType.DelayTrigger;
            AITask.TriggerParam.TriggerDelaySrc = EnumAITriggerDelaySrc.AISampleClk;
            AITask.TriggerParam.TriggerDelay = Delay;//1S
            AITask.TriggerParam.DigitialTriggerSettings.TriggerSrc = EnumAIDigitalTriggerSrc.ExtDigital;
            AITask.TriggerParam.DigitialTriggerSettings.TriggerEdge = Edge;
            FWG.SetChannel(Rigol_DG1032Z_USB.Channal.CH1, Rigol_DG1032Z_USB.Function.SIN, 1000, 10);
            switch (Edge)
            {
                case EnumAIDigitalTrgEdge.Rising:
                    {
                        FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH2, 0);
                        FWG.Start();
                        err = AITask.Start();
                        FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH2, 3);
                        FWG.Start();
                        Time = DateTime.Now;
                        break;
                    }
                case EnumAIDigitalTrgEdge.Falling:
                    {
                        FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH2, 3);
                        FWG.Start();
                        err = AITask.Start();
                        FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH2, 0);
                        FWG.Start();
                        Time = DateTime.Now;
                        break;
                    }
            }
            err = AITask.ReadData(ref Findata, 1000, false, -1);
            var ts = DateTime.Now - Time;
            DelayTime = ts.TotalMilliseconds;
            err = AITask.Stop();
            FWG.Stop();
            return GetVerifyState(Findata, 10000, 10);
        }

        /// <summary>
        /// AI外部数字触发，触发类型PreTrigger，samplesPerChannel必须小于PreCnt
        /// </summary>
        /// <param name="boardNum">板卡号</param>
        /// <param name="Edge">触发边沿</param>
        /// <param name="samplesPerChannel">每通道采集的点数</param>
        /// <param name="PreCnt">预触发的点数</param>
        /// <returns></returns>
        public static bool AI_Digital_PreTrigger(int boardNum, JYPXI62205.EnumAIDigitalTrgEdge Edge, int samplesPerChannel, int PreCnt)
        {
            double[] Findata = new double[samplesPerChannel];

            DateTime Time = DateTime.Now;
            Rigol_DG1032Z_USB FWG = new Rigol_DG1032Z_USB();
            JYPXI62205AITask AITask = new JYPXI62205AITask(boardNum);
            AITask.SampleRate = 10000;
            AITask.SamplesToAcquire = samplesPerChannel;
            AITask.Mode = EnumAIMode.Finite;
            err = AITask.AddChannel(0, -10, 10, JYPXI62205.EnumCoupling.Default, JYPXI62205.EnumAITerminalConfig.Default);
            AITask.TriggerParam.TriggerMode = EnumAITriggerMode.DigitalTrigger;
            AITask.TriggerParam.TriggerType = EnumAITriggerType.PreTrigger;
            AITask.TriggerParam.PreTriggerCnt = 1000;
            AITask.TriggerParam.DigitialTriggerSettings.TriggerSrc = EnumAIDigitalTriggerSrc.ExtDigital;
            AITask.TriggerParam.DigitialTriggerSettings.TriggerEdge = Edge;
            FWG.SetChannel(Rigol_DG1032Z_USB.Channal.CH1, Rigol_DG1032Z_USB.Function.SIN, 1000, 10);
            switch (Edge)
            {
                case EnumAIDigitalTrgEdge.Rising:
                    {
                        FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH2, 0);
                        FWG.Start();
                        err = AITask.Start();
                        FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH2, 3);
                        FWG.Start();
                        break;
                    }
                case EnumAIDigitalTrgEdge.Falling:
                    {
                        FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH2, 3);
                        FWG.Start();
                        err = AITask.Start();
                        FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH2, 0);
                        FWG.Start();
                        break;
                    }
            }
            err = AITask.ReadData(ref Findata, samplesPerChannel, false, -1);
            err = AITask.Stop();
            FWG.Stop();
            return GetVerifyState(Findata, 10000, 10);
        }

        /// <summary>
        /// AI外部数字触发，触发类型MidTrigger，samplesPerChannel必须等于MidTriggerBeforeCnt+MidTriggerAffterCnt
        /// </summary>
        /// <param name="boardNum">板卡编号</param>
        /// <param name="Edge">触发边沿</param>
        /// <param name="samplesPerChannel">采样率</param>
        /// <param name="MidTriggerBeforeCnt">前触发点数</param>
        /// <param name="MidTriggerAffterCnt">后触发点数</param>
        /// <returns>验证状态</returns>
        public static bool AI_Digital_MidTrigger(int boardNum, JYPXI62205.EnumAIDigitalTrgEdge Edge, int samplesPerChannel, int MidTriggerBeforeCnt, int MidTriggerAffterCnt)
        {
            double[] Findata = new double[samplesPerChannel];

            DateTime Time = DateTime.Now;
            Rigol_DG1032Z_USB FWG = new Rigol_DG1032Z_USB();
            JYPXI62205AITask AITask = new JYPXI62205AITask(boardNum);
            AITask.SampleRate = 10000;
            AITask.SamplesToAcquire = samplesPerChannel;
            AITask.Mode = EnumAIMode.Finite;
            err = AITask.AddChannel(0, -10, 10, JYPXI62205.EnumCoupling.Default, JYPXI62205.EnumAITerminalConfig.Default);
            AITask.TriggerParam.TriggerMode = EnumAITriggerMode.DigitalTrigger;
            AITask.TriggerParam.TriggerType = EnumAITriggerType.MidTrigger;
            AITask.TriggerParam.MidTriggerBeforeCnt = MidTriggerBeforeCnt;
            AITask.TriggerParam.MidTriggerAffterCnt = MidTriggerAffterCnt;
            AITask.TriggerParam.DigitialTriggerSettings.TriggerSrc = EnumAIDigitalTriggerSrc.ExtDigital;
            AITask.TriggerParam.DigitialTriggerSettings.TriggerEdge = Edge;
            FWG.SetChannel(Rigol_DG1032Z_USB.Channal.CH1, Rigol_DG1032Z_USB.Function.SIN, 1000, 10);
            switch (Edge)
            {
                case EnumAIDigitalTrgEdge.Rising:
                    {
                        FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH2, 0);
                        FWG.Start();
                        err = AITask.Start();
                        FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH2, 3);
                        FWG.Start();
                        break;
                    }
                case EnumAIDigitalTrgEdge.Falling:
                    {
                        FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH2, 3);
                        FWG.Start();
                        err = AITask.Start();
                        FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH2, 0);
                        FWG.Start();
                        break;
                    }
            }
            err = AITask.ReadData(ref Findata, samplesPerChannel, false, -1);
            err = AITask.Stop();
            FWG.Stop();
            return GetVerifyState(Findata, 10000, 10);
        }

        /// <summary>
        /// AI外部数字触发，触发类型PostTrigger,重触发
        /// </summary>
        /// <param name="boardNum">板卡编号</param>
        /// <param name="Edge">触发边沿</param>
        /// <param name="ReTriggerCount">触发次数，小于5次（信号源问题）</param>
        /// <returns>验证状态</returns>
        public static bool AI_Digital_ReTrigger(int boardNum, JYPXI62205.EnumAIDigitalTrgEdge Edge, int ReTriggerCount)
        {
            double[] Findata = new double[1000];

            DateTime Time = DateTime.Now;
            Rigol_DG1032Z_USB FWG = new Rigol_DG1032Z_USB();
            JYPXI62205AITask AITask = new JYPXI62205AITask(boardNum);
            AITask.SampleRate = 10000;
            AITask.SamplesToAcquire = 1000;
            AITask.Mode = EnumAIMode.Finite;
            err = AITask.AddChannel(0, -10, 10, JYPXI62205.EnumCoupling.Default, JYPXI62205.EnumAITerminalConfig.Default);
            AITask.TriggerParam.TriggerMode = EnumAITriggerMode.DigitalTrigger;
            AITask.TriggerParam.TriggerType = EnumAITriggerType.PostTrigger;
            AITask.TriggerParam.ReTriggerCount = ReTriggerCount;
            AITask.TriggerParam.DigitialTriggerSettings.TriggerSrc = EnumAIDigitalTriggerSrc.ExtDigital;
            AITask.TriggerParam.DigitialTriggerSettings.TriggerEdge = Edge;
            FWG.SetChannel(Rigol_DG1032Z_USB.Channal.CH1, Rigol_DG1032Z_USB.Function.SIN, 1000, 10);
            err = AITask.Start();
            for (int i = 0; i < ReTriggerCount; i++)
            {
                Thread.Sleep(1000);
                switch (Edge)
                {
                    case EnumAIDigitalTrgEdge.Rising:
                        {
                            FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH2, 0);
                            FWG.Start();
                            FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH2, 3);
                            FWG.Start();
                            err = AITask.ReadData(ref Findata, 1000, false, -1);
                            break;
                        }
                    case EnumAIDigitalTrgEdge.Falling:
                        {
                            FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH2, 3);
                            FWG.Start();
                            FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH2, 0);
                            FWG.Start();
                            err = AITask.ReadData(ref Findata, 1000, false, -1);
                            break;
                        }
                }
            }
            err = AITask.Stop();
            FWG.Stop();
            return GetVerifyState(Findata, 10000, 10);
        }

        /// <summary>
        /// AI模拟触发,触发类型PostTrigger
        /// </summary>
        /// <param name="boardNum">板卡编号</param>
        /// <param name="Edge">触发边沿</param>
        /// <param name="TriggerHigh">高触发值</param>
        /// <param name="TriggerLow">低触发值</param>
        /// <param name="Source">触发源</param>
        /// <returns>验证状态</returns>
        public static bool AI_Analog_PostTrigger(int boardNum, JYPXI62205.EnumAIAnalogTrgEdge Edge, double TriggerHigh, double TriggerLow, JYPXI62205.EnumAIAnalogTriggerSrc Source = EnumAIAnalogTriggerSrc.ExtAnalog)
        {
            double[] Findata = new double[1000];
            bool state1, state2;

            Rigol_DG1032Z_USB FWG = new Rigol_DG1032Z_USB();
            JYPXI62205AITask AITask = new JYPXI62205AITask(boardNum);
            AITask.SampleRate = 10000;
            AITask.SamplesToAcquire = 1000;
            AITask.Mode = EnumAIMode.Finite;
            err = AITask.AddChannel(0, -10, 10, JYPXI62205.EnumCoupling.Default, JYPXI62205.EnumAITerminalConfig.Default);
            AITask.TriggerParam.TriggerMode = EnumAITriggerMode.AnalogTrigger;
            AITask.TriggerParam.TriggerType = EnumAITriggerType.PostTrigger;
            AITask.TriggerParam.AnalogTriggerSettings.TriggerSrc = Source;
            AITask.TriggerParam.AnalogTriggerSettings.TriggerEdge = Edge;
            AITask.TriggerParam.AnalogTriggerSettings.TriggerHighLevel = TriggerHigh;
            AITask.TriggerParam.AnalogTriggerSettings.TriggerLowLevel = TriggerLow;
            FWG.SetChannel(Rigol_DG1032Z_USB.Channal.CH1, Rigol_DG1032Z_USB.Function.SIN, 1000, 20);
            if (Source == EnumAIAnalogTriggerSrc.ExtAnalog)
            {
                FWG.SetChannel(Rigol_DG1032Z_USB.Channal.CH2, Rigol_DG1032Z_USB.Function.SIN, 1000, 20);
                FWG.Start();
                err = AITask.Start();
                err = AITask.ReadData(ref Findata, 1000, false, -1);
                err = AITask.Stop();
                FWG.Stop();
                return GetVerifyState(Findata, 10000, 20);
            }
            FWG.Start();
            err = AITask.Start();
            err = AITask.ReadData(ref Findata, 1000, false, -1);
            err = AITask.Stop();
            FWG.Stop();

            if (Edge == EnumAIAnalogTrgEdge.AboveHighLevle)
            {
                if ((Math.Abs(Findata[0] - TriggerHigh) <= TriggerHigh * 0.1) && (Findata[0] <= Findata[1]))
                {
                    state1 = true;
                }
                else
                {
                    state1 = false;
                }
            }
            else if (Edge == EnumAIAnalogTrgEdge.BelowLowLevel)
            {
                if ((Math.Abs(Findata[0] - TriggerLow) <= TriggerLow * 0.1) && (Findata[0] >= Findata[1]))
                {
                    state1 = true;
                }
                else
                {
                    state1 = false;
                }
            }
            else if (Edge == EnumAIAnalogTrgEdge.InsideRegion)
            {
                if (Findata[0] >= TriggerLow && Findata[0] <= TriggerHigh)
                {
                    state1 = true;
                }
                else
                {
                    state1 = false;
                }
            }
            else
            {
                state1 = true;
            }
            state2 = GetVerifyState(Findata, 10000, 20);
            if (state1 && state2)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// AI模拟触发,触发类型DelayTriggerr
        /// </summary>
        /// <param name="boardNum">板卡编号</param>
        /// <param name="Edge">触发边沿</param>
        /// <param name="Delay">延时</param>
        /// <param name="TriggerHigh">高触发值</param>
        /// <param name="TriggerLow">低触发值</param>
        /// <param name="Source">触发源</param>
        /// <returns>验证状态</returns>
        public static bool AI_Analog_DelayTrigger(int boardNum, JYPXI62205.EnumAIAnalogTrgEdge Edge, int Delay, double TriggerHigh, double TriggerLow, JYPXI62205.EnumAIAnalogTriggerSrc Source = EnumAIAnalogTriggerSrc.ExtAnalog)
        {
            double[] Findata = new double[1000];
            double DelayTime = 0.0;

            DateTime Time = DateTime.Now;
            Rigol_DG1032Z_USB FWG = new Rigol_DG1032Z_USB();
            JYPXI62205AITask AITask = new JYPXI62205AITask(boardNum);
            AITask.SampleRate = 10000;
            AITask.SamplesToAcquire = 1000;
            AITask.Mode = EnumAIMode.Finite;
            err = AITask.AddChannel(0, -10, 10, JYPXI62205.EnumCoupling.Default, JYPXI62205.EnumAITerminalConfig.Default);
            AITask.TriggerParam.TriggerMode = EnumAITriggerMode.AnalogTrigger;
            AITask.TriggerParam.TriggerType = EnumAITriggerType.DelayTrigger;
            AITask.TriggerParam.AnalogTriggerSettings.TriggerSrc = Source;
            AITask.TriggerParam.AnalogTriggerSettings.TriggerEdge = Edge;
            AITask.TriggerParam.AnalogTriggerSettings.TriggerHighLevel = TriggerHigh;
            AITask.TriggerParam.AnalogTriggerSettings.TriggerLowLevel = TriggerLow;
            AITask.TriggerParam.TriggerDelaySrc = EnumAITriggerDelaySrc.AISampleClk;
            AITask.TriggerParam.TriggerDelay = Delay;//1S
            FWG.SetChannel(Rigol_DG1032Z_USB.Channal.CH1, Rigol_DG1032Z_USB.Function.SIN, 1000, 20);
            if (Source == EnumAIAnalogTriggerSrc.ExtAnalog)
            {
                FWG.SetChannel(Rigol_DG1032Z_USB.Channal.CH2, Rigol_DG1032Z_USB.Function.SIN, 1000, 20);
            }
            FWG.Start();
            Thread.Sleep(500);
            err = AITask.Start();
            err = AITask.ReadData(ref Findata, 1000, false, -1);
            var ts = DateTime.Now - Time;
            DelayTime = ts.TotalMilliseconds;
            err = AITask.Stop();
            FWG.Stop();
            return GetVerifyState(Findata, 10000, 20);
        }

        /// <summary>
        /// AI模拟触发，触发类型PreTrigger，samplesPerChannel必须小于PreCnt
        /// </summary>
        /// <param name="boardNum">板卡号</param>
        /// <param name="Edge">触发边沿</param>
        /// <param name="samplesPerChannel">每通道采集的点数</param>
        /// <param name="PreCnt">预触发的点数</param>
        /// <returns></returns>
        public static bool AI_Analog_PreTrigger(int boardNum, JYPXI62205.EnumAIAnalogTrgEdge Edge, int samplesPerChannel, int PreCnt, JYPXI62205.EnumAIAnalogTriggerSrc Source = EnumAIAnalogTriggerSrc.ExtAnalog)
        {
            double[] Findata = new double[samplesPerChannel];

            DateTime Time = DateTime.Now;
            Rigol_DG1032Z_USB FWG = new Rigol_DG1032Z_USB();
            JYPXI62205AITask AITask = new JYPXI62205AITask(boardNum);
            AITask.SampleRate = 10000;
            AITask.SamplesToAcquire = samplesPerChannel;
            AITask.Mode = EnumAIMode.Finite;
            err = AITask.AddChannel(0, -10, 10, JYPXI62205.EnumCoupling.Default, JYPXI62205.EnumAITerminalConfig.Default);
            AITask.TriggerParam.TriggerMode = EnumAITriggerMode.AnalogTrigger;
            AITask.TriggerParam.AnalogTriggerSettings.TriggerSrc = Source;
            AITask.TriggerParam.AnalogTriggerSettings.TriggerEdge = Edge;
            AITask.TriggerParam.AnalogTriggerSettings.TriggerHighLevel = 5;
            AITask.TriggerParam.AnalogTriggerSettings.TriggerLowLevel = -5;
            AITask.TriggerParam.TriggerType = EnumAITriggerType.PreTrigger;
            AITask.TriggerParam.PreTriggerCnt = 1000;
            FWG.SetChannel(Rigol_DG1032Z_USB.Channal.CH1, Rigol_DG1032Z_USB.Function.SIN, 1000, 10);
            if (Source == EnumAIAnalogTriggerSrc.ExtAnalog)
            {
                FWG.SetChannel(Rigol_DG1032Z_USB.Channal.CH2, Rigol_DG1032Z_USB.Function.SIN, 1000, 20);
            }
            FWG.Start();
            Thread.Sleep(2000);
            err = AITask.Start();
            err = AITask.ReadData(ref Findata, samplesPerChannel, false, -1);
            err = AITask.Stop();
            FWG.Stop();
            return GetVerifyState(Findata, 10000, 10);
        }

        /// <summary>
        /// AI模拟触发，触发类型MidTrigger，samplesPerChannel必须等于MidTriggerBeforeCnt+MidTriggerAffterCnt
        /// </summary>
        /// <param name="boardNum">板卡编号</param>
        /// <param name="Edge">触发边沿</param>
        /// <param name="samplesPerChannel">采样率</param>
        /// <param name="MidTriggerBeforeCnt">前触发点数</param>
        /// <param name="MidTriggerAffterCnt">后触发点数</param>
        /// <returns>验证状态</returns>
        public static bool AI_Analog_MidTrigger(int boardNum, JYPXI62205.EnumAIAnalogTrgEdge Edge, int samplesPerChannel, int MidTriggerBeforeCnt, int MidTriggerAffterCnt, JYPXI62205.EnumAIAnalogTriggerSrc Source = EnumAIAnalogTriggerSrc.ExtAnalog)
        {
            double[] Findata = new double[samplesPerChannel];

            DateTime Time = DateTime.Now;
            Rigol_DG1032Z_USB FWG = new Rigol_DG1032Z_USB();
            JYPXI62205AITask AITask = new JYPXI62205AITask(boardNum);
            AITask.SampleRate = 10000;
            AITask.SamplesToAcquire = samplesPerChannel;
            AITask.Mode = EnumAIMode.Finite;
            err = AITask.AddChannel(0, -10, 10, JYPXI62205.EnumCoupling.Default, JYPXI62205.EnumAITerminalConfig.Default);
            AITask.TriggerParam.TriggerMode = EnumAITriggerMode.AnalogTrigger;
            AITask.TriggerParam.TriggerType = EnumAITriggerType.MidTrigger;
            AITask.TriggerParam.MidTriggerBeforeCnt = MidTriggerBeforeCnt;
            AITask.TriggerParam.MidTriggerAffterCnt = MidTriggerAffterCnt;
            AITask.TriggerParam.AnalogTriggerSettings.TriggerSrc = Source;
            AITask.TriggerParam.AnalogTriggerSettings.TriggerEdge = Edge;
            AITask.TriggerParam.AnalogTriggerSettings.TriggerHighLevel = 2;
            AITask.TriggerParam.AnalogTriggerSettings.TriggerLowLevel = -2;
            FWG.SetChannel(Rigol_DG1032Z_USB.Channal.CH1, Rigol_DG1032Z_USB.Function.SIN, 1000, 10);
            if (Source == EnumAIAnalogTriggerSrc.ExtAnalog)
            {
                FWG.SetChannel(Rigol_DG1032Z_USB.Channal.CH2, Rigol_DG1032Z_USB.Function.SIN, 1000, 20);
            }
            FWG.Start();
            err = AITask.Start();
            err = AITask.ReadData(ref Findata, samplesPerChannel, false, -1);
            err = AITask.Stop();
            FWG.Stop();
            return GetVerifyState(Findata, 10000, 10);
        }

        /// <summary>
        /// AI外部数字触发，触发类型PostTrigger,重触发
        /// </summary>
        /// <param name="boardNum">板卡编号</param>
        /// <param name="Edge">触发边沿</param>
        /// <param name="ReTriggerCount">触发次数，小于5次（信号源问题）</param>
        /// <returns>验证状态</returns>
        public static bool AI_Analog_ReTrigger(int boardNum, JYPXI62205.EnumAIAnalogTrgEdge Edge, int ReTriggerCount)
        {
            double[] Findata = new double[1000];

            DateTime Time = DateTime.Now;
            Rigol_DG1032Z_USB FWG = new Rigol_DG1032Z_USB();
            JYPXI62205AITask AITask = new JYPXI62205AITask(boardNum);
            AITask.SampleRate = 10000;
            AITask.SamplesToAcquire = 1000;
            AITask.Mode = EnumAIMode.Finite;
            err = AITask.AddChannel(0, -10, 10, JYPXI62205.EnumCoupling.Default, JYPXI62205.EnumAITerminalConfig.Default);
            AITask.TriggerParam.TriggerMode = EnumAITriggerMode.AnalogTrigger;
            AITask.TriggerParam.TriggerType = EnumAITriggerType.PostTrigger;
            AITask.TriggerParam.ReTriggerCount = ReTriggerCount;
            AITask.TriggerParam.AnalogTriggerSettings.TriggerSrc = EnumAIAnalogTriggerSrc.ChannelAnalog;
            AITask.TriggerParam.AnalogTriggerSettings.TriggerEdge = Edge;
            AITask.TriggerParam.AnalogTriggerSettings.TriggerHighLevel = 2;
            AITask.TriggerParam.AnalogTriggerSettings.TriggerLowLevel = -2;
            FWG.SetChannel(Rigol_DG1032Z_USB.Channal.CH1, Rigol_DG1032Z_USB.Function.SIN, 1000, 10);
            FWG.Start();
            err = AITask.Start();
            for (int i = 0; i < ReTriggerCount; i++)
            {
                err = AITask.ReadData(ref Findata, 1000, false, -1);
                Thread.Sleep(500);
            }
            err = AITask.Stop();
            FWG.Stop();
            return GetVerifyState(Findata, 10000, 10);
        }

        #endregion

        #region----------AO测试---------- 

        /// <summary>
        /// AO单点输出测试
        /// </summary>
        /// <param name="Amp">AO输出幅值</param>
        public static bool AO_SingleMode_SingleChannel(int boardNum, double Amp)
        {
            double MesAmp;
            Tek_DPO2024B_USB SCOPE = new Tek_DPO2024B_USB();
            JYPXI62205AOTask AOTask = new JYPXI62205AOTask(boardNum);
            AOTask.UpdateRate = 1000;
            AOTask.Mode = EnumAOMode.Single;
            err = AOTask.AddChannel(0, -10, 10);
            err = AOTask.WriteSinglePoint(Amp);
            SCOPE.CHProbeGain = EnumCHProbeGain.X10;
            SCOPE.ChannleScale = EnumChannelScale.Scale_2_V;
            SCOPE.SetHorizontalScale(EnumHorizontalValue.Uint_10, EnumHorizontalUnit.ms);
            SCOPE.OpenChannel(EnumChannel.CH1);
            err = AOTask.Start();
            MesAmp = SCOPE.Measure(EnumChannel.CH1, EnumMeasureType.MaxValue);
            if (Math.Abs(MesAmp - Amp) <= Amp * 0.1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 单通道生成10Hz正弦波，有限1000个点，共计10个周期
        /// </summary>
        /// <param name="boardNum">板卡编号</param>
        /// <returns></returns>
        public static bool AO_Finite_SingleChannel(int boardNum)
        {
            double MesAmp;
            double MesFre;
            double[] WriteData = new double[1000];

            Tek_DPO2024B_USB SCOPE = new Tek_DPO2024B_USB();
            JYPXI62205AOTask AOTask = new JYPXI62205AOTask(boardNum);
            AOTask.UpdateRate = 1000;
            AOTask.SamplesToUpdate = 1000;
            AOTask.Mode = EnumAOMode.Finite;
            err = AOTask.AddChannel(0, -10, 10);
            SeeSharpTools.JY.DSP.Fundamental.Generation.SineWave(ref WriteData, 5, 0, 10);
            err = AOTask.WriteData(WriteData, -1);
            SCOPE.CHProbeGain = EnumCHProbeGain.X10;
            SCOPE.ChannleScale = EnumChannelScale.Scale_2_V;
            SCOPE.SetHorizontalScale(EnumHorizontalValue.Uint_100, EnumHorizontalUnit.ms);
            SCOPE.OpenChannel(EnumChannel.CH1);
            Thread.Sleep(1000);
            err = AOTask.Start();
            Thread.Sleep(1000);
            MesFre = SCOPE.Measure(EnumChannel.CH1, EnumMeasureType.Frequence);
            MesAmp = SCOPE.Measure(EnumChannel.CH1, EnumMeasureType.MaxValue);
            if ((Math.Abs(MesAmp - 5) <= 5 * 0.1) && (Math.Abs(MesFre - 10) <= 10 * 0.1))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 多通道生成10Hz正弦波，有限1000个点，共计10个周期
        /// </summary>
        /// <param name="boardNum">板卡编号</param>
        /// <returns></returns>
        public static bool AO_Finite_MulChannel(int boardNum)
        {
            double MesAmp;
            double MesFre;
            double[,] WriteData = new double[1000, 2];
            double[] WriteData1 = new double[1000];
            double[] WriteData2 = new double[1000];

            Tek_DPO2024B_USB SCOPE = new Tek_DPO2024B_USB();
            JYPXI62205AOTask AOTask = new JYPXI62205AOTask(boardNum);
            AOTask.UpdateRate = 1000;
            AOTask.SamplesToUpdate = 1000;
            AOTask.Mode = EnumAOMode.Finite;
            err = AOTask.AddChannel(-1, -10, 10);
            SeeSharpTools.JY.DSP.Fundamental.Generation.SineWave(ref WriteData1, 5, 0, 10);
            SeeSharpTools.JY.DSP.Fundamental.Generation.SineWave(ref WriteData1, 5, 0, 10);
            for (int i = 0; i < 1000; i++)
            {
                WriteData[i, 0] = WriteData1[i];
            }
            for (int j = 0; j < 1000; j++)
            {
                WriteData[j, 1] = WriteData1[j];
            }
            err = AOTask.WriteData(WriteData, -1);
            SCOPE.CHProbeGain = EnumCHProbeGain.X10;
            SCOPE.ChannleScale = EnumChannelScale.Scale_2_V;
            SCOPE.SetHorizontalScale(EnumHorizontalValue.Uint_100, EnumHorizontalUnit.ms);
            SCOPE.OpenChannel(EnumChannel.CH1);
            SCOPE.OpenChannel(EnumChannel.CH2);
            Thread.Sleep(1000);
            err = AOTask.Start();
            Thread.Sleep(1000);
            MesFre = SCOPE.Measure(EnumChannel.CH1, EnumMeasureType.Frequence);
            MesAmp = SCOPE.Measure(EnumChannel.CH2, EnumMeasureType.MaxValue);
            if ((Math.Abs(MesAmp - 5) <= 5 * 0.1) && (Math.Abs(MesFre - 10) <= 10 * 0.1))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// AO连续输出测试
        /// </summary>
        /// <param name="Amp">输出幅值</param>
        /// <param name="Type">波形类型</param>
        /// <param name="UpdateRate">刷新率</param>
        /// <param name="SamplesToUpdate">刷新点数</param>
        public static bool AO_ContinuousWrapping_SingleChannel(int boardNum)
        {
            double MesAmp;
            double MesFre;
            double[] WriteData = new double[1000];

            Tek_DPO2024B_USB SCOPE = new Tek_DPO2024B_USB();
            JYPXI62205AOTask AOTask = new JYPXI62205AOTask(boardNum);
            AOTask.UpdateRate = 1000000;
            AOTask.SamplesToUpdate = 1000;
            AOTask.Mode = EnumAOMode.ContinuousWrapping;
            err = AOTask.AddChannel(0, -10, 10);
            SeeSharpTools.JY.DSP.Fundamental.Generation.SineWave(ref WriteData, 10, 0, 10);
            err = AOTask.WriteData(WriteData, -1);
            SCOPE.CHProbeGain = EnumCHProbeGain.X10;
            SCOPE.ChannleScale = EnumChannelScale.Scale_5_V;
            SCOPE.SetHorizontalScale(EnumHorizontalValue.Uint_100, EnumHorizontalUnit.us);
            SCOPE.OpenChannel(EnumChannel.CH1);
            Thread.Sleep(1000);
            err = AOTask.Start();
            Thread.Sleep(2000);
            MesFre = SCOPE.Measure(EnumChannel.CH1, EnumMeasureType.Frequence);
            MesAmp = SCOPE.Measure(EnumChannel.CH1, EnumMeasureType.MaxValue);
            err = AOTask.Stop();
            if ((Math.Abs(MesAmp - 10) <= 10 * 0.1) && (Math.Abs(MesFre - 10000) <= 10000 * 0.1))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// AO数字PostTrigger
        /// </summary>
        /// <param name="boardNum">板卡编号</param>
        /// <param name="Edge">AO触发边沿</param>
        /// <returns></returns>
        public static bool A0_Digital_PostTrigger(int boardNum, JYPXI62205.EnumAOTrgEdge Edge)
        {
            double MesAmp = 0.0;
            double MesFre = 0.0;
            double[] WriteData = new double[1000];
            Rigol_DG1032Z_USB FWG = new Rigol_DG1032Z_USB();
            Tek_DPO2024B_USB SCOPE = new Tek_DPO2024B_USB();
            JYPXI62205AOTask AOTask = new JYPXI62205AOTask(boardNum);
            AOTask.UpdateRate = 1000000;
            AOTask.SamplesToUpdate = 1000;
            AOTask.Mode = EnumAOMode.ContinuousWrapping;
            err = AOTask.AddChannel(0, -10, 10);
            AOTask.TriggerParam.DigitialTriggerSettings.TriggerSrc = EnumAODigitalTriggerSrc.ExtDigital;
            AOTask.TriggerParam.DigitialTriggerSettings.TriggerEdge = Edge;
            AOTask.TriggerParam.TriggerMode = EnumAOTriggerMode.DigitalTrigger;
            AOTask.TriggerParam.TriggerType = EnumAOTriggerType.PostTrigger;
            SeeSharpTools.JY.DSP.Fundamental.Generation.SineWave(ref WriteData, 10, 0, 10);
            err = AOTask.WriteData(WriteData, -1);
            SCOPE.CHProbeGain = EnumCHProbeGain.X10;
            SCOPE.ChannleScale = EnumChannelScale.Scale_5_V;
            SCOPE.SetHorizontalScale(EnumHorizontalValue.Uint_100, EnumHorizontalUnit.us);
            SCOPE.OpenChannel(EnumChannel.CH1);
            err = AOTask.Start();
            switch (Edge)
            {
                case EnumAOTrgEdge.Rising:
                    {
                        FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH2, 0);
                        FWG.Start();
                        Thread.Sleep(10);
                        FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH2, 3);
                        FWG.Start();
                        break;
                    }
                case EnumAOTrgEdge.Falling:
                    {
                        FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH2, 3);
                        FWG.Start();
                        Thread.Sleep(10);
                        FWG.SetChannel_DC(Rigol_DG1032Z_USB.Channal.CH2, 0);
                        FWG.Start();
                        break;
                    }
            }
            Thread.Sleep(3000);
            MesFre = SCOPE.Measure(EnumChannel.CH1, EnumMeasureType.Frequence);
            MesAmp = SCOPE.Measure(EnumChannel.CH1, EnumMeasureType.MaxValue);
            err = AOTask.Stop();
            FWG.Stop();
            if ((Math.Abs(MesAmp - 10) <= 10 * 0.1) && (Math.Abs(MesFre - 10000) <= 10000 * 0.1))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion


        #region----------DIO测试---------- 
        /// <summary>
        /// DIO输入输出测试
        /// </summary>
        /// <param name="boardNum">板卡号</param>
        /// <param name="DIPort">DI口</param>
        /// <param name="DOPort">DO口</param>
        /// <returns>验证状态</returns>
        public static bool DIO_InOutput(int boardNum, int DIPort, int DOPort)
        {
            bool[] DIBuf1 = new bool[8];
            bool[] DIBuf2 = new bool[8];
            bool[] DOBuf = new bool[8];

            JYPXI62205DITask DITask = new JYPXI62205DITask(boardNum);
            JYPXI62205DOTask DOTask = new JYPXI62205DOTask(boardNum);
            for (int i = 0; i < 8; i++)
            {
                DOBuf[i] = true;
            }
            err = DITask.AddChannel(DIPort);
            err = DOTask.AddChannel(DOPort);
            err = DITask.Start();
            err = DOTask.Start();

            err = DOTask.WriteSinglePoint(DOBuf);
            err = DITask.ReadSinglePoint(ref DIBuf1);
            for (int i = 0; i < 8; i++)
            {
                DOBuf[i] = false;
            }
            err = DOTask.WriteSinglePoint(DOBuf);
            err = DITask.ReadSinglePoint(ref DIBuf2);
            err = DITask.Stop();
            err = DOTask.Stop();
            foreach (var item in DIBuf1)
            {
                if (item == false)
                {
                    return false;
                }
            }
            foreach (var item in DIBuf2)
            {
                if (item == true)
                {
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region----------CI测试----------
        /// <summary>
        /// 计数器输入和输出测试，两路计数器，一路输入，一路输出
        /// </summary>
        /// <param name="boardNum">板卡号</param>
        /// <param name="CIPort">计数器输入口</param>
        /// <param name="COPort">计数器输出口</param>
        /// <returns>验证状态</returns>
        public static bool CIO_Test(int boardNum, int CIPort, int COPort)
        {
            uint count = 65535;

            JYPXI62205CITask CITask = new JYPXI62205CITask(boardNum, CIPort);
            JYPXI62205COTask COTask = new JYPXI62205COTask(boardNum, COPort);

            COTask.ApplicationType = EnumCOApplicationType.ContGatedPulseGen;
            COTask.COParam.ClkSrc = EnumCOClkSrc.IntClk;
            COTask.COParam.GateSrc = EnumCOGateSrc.Software;
            COTask.COParam.GateActive = EnumCOActiveLevel.LowActive;
            COTask.COPulseParams.PulseParamType = EnumCOPulseParamType.DutyCycleFrequency;
            COTask.COParam.OutPutActive = EnumCOActiveLevel.HighActive;
            COTask.COPulseParams.Frequency = 100000;
            COTask.COPulseParams.DutyCycle = 0.5;

            CITask.ApplicationType = EnumCIApplicationType.EdgeCounting;
            CITask.CCIParam.ClkSrc = EnumCIClkSrc.ExtClk;
            CITask.CCIParam.GateSrc = EnumCIGateSrc.Software;
            CITask.CCIParam.GatePolarity = EnumCIPolarity.LowActive;
            CITask.CCIParam.CountDirection = EnumCountDirection.Down;
            CITask.CCIParam.InitialCount = count;

            err = CITask.Start();
            err = COTask.Start();
            while (count > 65535 / 2)
            {
                err = CITask.ReadCounter(ref count);
            }
            err = CITask.Stop();
            err = COTask.Stop();
            return true;
        }

        #endregion

        #region----------私有方法----------
        /// <summary>
        /// 获取验证状态
        /// </summary>
        /// <param name="SourceData">源数据</param>
        /// <param name="sampleRate">采样率</param>
        private static bool GetVerifyState(double[] SourceData, double sampleRate, double Amp)
        {
            double[] Spectrum = new double[SourceData.Length / 2 + 1];
            double df;
            double maxData;
            int index;
            SeeSharpTools.JY.DSP.Fundamental.Spectrum.PowerSpectrum(SourceData, sampleRate, ref Spectrum, out df);
            maxData = Spectrum.Max();
            index = Spectrum.ToList().IndexOf(maxData);
            Frequency = (double)index * df;
            Vpp = SourceData.Max() - SourceData.Min();
            if ((Math.Abs(Frequency - sampleRate / 10) < sampleRate / 10 * 0.1) && (Math.Abs(Vpp - Amp) < Amp * 0.2))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 获取频率
        /// </summary>
        /// <param name="Frequency">频率</param>
        /// <param name="SourceData">源数据</param>
        /// <param name="sampleRate">采样率</param>
        private static bool GetFrequency(ref double Frequency, double[] SourceData, double sampleRate)
        {
            double[] Spectrum = new double[SourceData.Length / 2 + 1];
            double df;
            double maxData;
            int index;
            SeeSharpTools.JY.DSP.Fundamental.Spectrum.PowerSpectrum(SourceData, sampleRate, ref Spectrum, out df);
            maxData = Spectrum.Max();
            index = Spectrum.ToList().IndexOf(maxData);
            Frequency = (double)index * df;
            Vpp = SourceData.Max() - SourceData.Min();
            if ((Math.Abs(Frequency - sampleRate / 10) < sampleRate / 10 * 0.1) && (Math.Abs(Vpp - 10) < 10 * 0.2))
            {
                return true;
            }
            else
            {
                return false;
            }
        }



        #endregion

        #region----------私有字段----------
        private static int SEChannels = 64;
        private static int DIChannels = 32;
        private static double Singledata;
        private static double Vpp = 0.0;
        private static double Frequency = 0.0;
        private static int err = 0;
        #endregion

        #region----------枚举----------
        /// <summary>
        /// 触发边沿
        /// </summary>
        public enum DigitalTriggleEdge
        {
            /// <summary>
            /// 上升沿
            /// </summary>
            Rising,

            /// <summary>
            /// 下降沿
            /// </summary>
            Falling

        };
        #endregion
    }
}
}
