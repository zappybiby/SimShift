using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SimShift.Data.Common;
using SimShift.Entities;

namespace SimShift.Services
{
    public enum PowerMeterState
    {
        Idle,

        Prearm,

        Revup,

        Revdown,

        Cooldown
    }

    /// <summary>
    ///     Estimates power level of engine by revving up
    /// </summary>
    class Ets2PowerMeter : IControlChainObj
    {
        public PowerMeterState State;

        private DateTime endRevdown;

        private DateTime endRevup;

        private float integralRevver = 0.0f;

        private float prearmSettler = 0;

        private float preArmThr;

        private float revdownRpm;

        private DateTime startRevdown;

        private DateTime startRevup;

        public bool Active { get; private set; }

        public bool Enabled { get; private set; }

        public IEnumerable<string> SimulatorsBan => new string[0];

        public IEnumerable<string> SimulatorsOnly => new string[0];

        public double GetAxis(JoyControls c, double val)
        {
            switch (c)
            {
                case JoyControls.Throttle:
                    if (!this.Active)
                    {
                        return val;
                    }
                    else if (this.State == PowerMeterState.Prearm)
                    {
                        return this.preArmThr;
                    }
                    else if (this.State == PowerMeterState.Revup)
                    {
                        return 1;
                    }
                    else if (this.State == PowerMeterState.Revdown)
                    {
                        return 0;
                    }
                    else
                    {
                        return val;
                    }

                    break;
                case JoyControls.Clutch: return this.Active ? 1 : val;

                default: return val;
            }
        }

        public bool GetButton(JoyControls c, bool val)
        {
            return val;
        }

        public bool Requires(JoyControls c)
        {
            return c == JoyControls.Clutch || c == JoyControls.Throttle;
        }

        public void TickControls()
        { }

        public void TickTelemetry(IDataMiner data)
        {
            this.Enabled = true;
            switch (this.State)
            {
                case PowerMeterState.Idle:
                    if (Main.GetButtonIn(JoyControls.MeasurePower) && false)
                    {
                        this.integralRevver = 0;
                        this.State = PowerMeterState.Prearm;
                        this.Active = true;
                    }
                    else
                    {
                        this.Active = false;
                    }

                    break;

                case PowerMeterState.Prearm:
                    this.preArmThr = (1000 - data.Telemetry.EngineRpm) / 1500;
                    if (Math.Abs(data.Telemetry.EngineRpm - 1000) < 100)
                    {
                        this.integralRevver += (1000 - data.Telemetry.EngineRpm) / 750000.0f;
                    }
                    else
                    {
                        this.integralRevver = 0;
                    }

                    this.preArmThr += this.integralRevver;

                    if (this.preArmThr > 0.7f)
                    {
                        this.preArmThr = 0.7f;
                    }

                    if (this.preArmThr < 0)
                    {
                        this.preArmThr = 0;
                    }

                    if (Math.Abs(data.Telemetry.EngineRpm - 1000) < 5)
                    {
                        this.prearmSettler++;
                        if (this.prearmSettler > 200)
                        {
                            this.startRevup = DateTime.Now;
                            this.State = PowerMeterState.Revup;
                        }
                    }
                    else
                    {
                        this.prearmSettler = 0;
                    }

                    break;

                case PowerMeterState.Revup:
                    if (data.Telemetry.EngineRpm >= 2000)
                    {
                        this.endRevup = DateTime.Now;
                        this.startRevdown = DateTime.Now;
                        this.State = PowerMeterState.Revdown;
                        this.revdownRpm = data.Telemetry.EngineRpm;
                    }

                    break;

                case PowerMeterState.Revdown:
                    if (data.Telemetry.EngineRpm <= 1000)
                    {
                        this.endRevdown = DateTime.Now;
                        this.State = PowerMeterState.Cooldown;
                        var fallTime = this.endRevdown.Subtract(this.startRevdown).TotalMilliseconds / 1000.0;
                        var fallRpm = this.revdownRpm - data.Telemetry.EngineRpm;
                        Console.WriteLine("Rev up: " + this.endRevup.Subtract(this.startRevup).TotalMilliseconds + "ms, rev down: " + fallTime + "ms (" + (fallRpm / fallTime) + "rpm/s");
                    }

                    break;

                case PowerMeterState.Cooldown:
                    this.Active = false;
                    if (data.Telemetry.EngineRpm < 700)
                    {
                        this.State = PowerMeterState.Idle;
                    }

                    break;
            }
        }
    }
}