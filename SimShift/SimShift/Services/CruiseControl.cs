using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using SimShift.Data;
using SimShift.Data.Common;
using SimShift.Entities;
using SimShift.Utils;

namespace SimShift.Services
{
    /// <summary>
    ///     This module simulates a CruiseControl. Although some games (like ETS2) incorporate Cruise Control, previously there
    ///     were no features like resume CC, speed up or slow down CC
    ///     And above all: the CC in-game disengages when shifting gear.
    /// </summary>
    public class CruiseControl : IControlChainObj, IConfigurable
    {
        private double IntegralTime;

        private double PreviousError = 0;

        public IEnumerable<string> AcceptsConfigs => new[] { "Cruise" };

        public bool Active => this.Cruising && !this.ManualOverride;

        public bool Cruising { get; private set; }

        public double DSlope { get; private set; }

        public bool Enabled => true;

        public double Imax { get; private set; }

        public double ISlope { get; private set; }

        public bool ManualOverride { get; private set; }

        public double PSlope { get; private set; }

        public IEnumerable<string> SimulatorsBan => new string[0];

        public IEnumerable<string> SimulatorsOnly => new string[0];

        public double Speed { get; private set; }

        public double SpeedCruise { get; private set; }

        private DateTime CruiseTimeout { get; set; }

        public void ApplyParameter(IniValueObject obj)
        {
            switch (obj.Key)
            {
                case "P":
                    this.PSlope = obj.ReadAsFloat();
                    break;
                case "Imax":
                    this.Imax = obj.ReadAsFloat();
                    break;
                case "I":
                    this.ISlope = obj.ReadAsFloat();
                    break;
                case "D":
                    this.DSlope = obj.ReadAsFloat();
                    break;

                // TODO: implement PID
            }
        }

        public IEnumerable<IniValueObject> ExportParameters()
        {
            List<IniValueObject> o = new List<IniValueObject>();
            o.Add(new IniValueObject(this.AcceptsConfigs, "P", this.PSlope.ToString("0.0000")));
            o.Add(new IniValueObject(this.AcceptsConfigs, "I", this.ISlope.ToString("0.0000")));
            o.Add(new IniValueObject(this.AcceptsConfigs, "Imax", this.Imax.ToString("0.0000")));
            o.Add(new IniValueObject(this.AcceptsConfigs, "D", this.DSlope.ToString("0.0000")));
            return o;
        }

        public double GetAxis(JoyControls c, double val)
        {
            switch (c)
            {
                case JoyControls.Throttle:
                    var error = this.SpeedCruise - this.Speed;
                    this.IntegralTime += error * this.ISlope;
                    var Differential = (error - this.PreviousError) * this.DSlope;
                    this.PreviousError = error;
                    if (this.IntegralTime > this.Imax)
                    {
                        this.IntegralTime = this.Imax;
                    }

                    if (this.IntegralTime < -this.Imax)
                    {
                        this.IntegralTime = -this.Imax;
                    }

                    var cruiseVal = error * 3.6 * this.PSlope + this.IntegralTime + Differential;
                    this.ManualOverride = val >= cruiseVal;
                    if (this.Cruising && cruiseVal > val)
                    {
                        val = cruiseVal;
                    }

                    var t = val;
                    if (t > 1)
                    {
                        t = 1;
                    }

                    if (t < 0)
                    {
                        t = 0;
                    }

                    return t;
                case JoyControls.Brake:
                    if (val > 0.1)
                    {
                        this.Cruising = false;
                    }

                    return val;

                default: return val;
            }
        }

        public bool GetButton(JoyControls c, bool val)
        {
            switch (c)
            {
                case JoyControls.CruiseControlMaintain:
                    if (val && DateTime.Now.Subtract(this.CruiseTimeout).TotalMilliseconds > 500)
                    {
                        this.Cruising = !this.Cruising;
                        this.SpeedCruise = this.Speed;
                        Debug.WriteLine("Cruising set to " + this.Cruising + " and " + this.SpeedCruise + " m/s");
                        this.CruiseTimeout = DateTime.Now;
                    }

                    return false;
                    break;

                case JoyControls.CruiseControlUp:
                    if (val && DateTime.Now.Subtract(this.CruiseTimeout).TotalMilliseconds > 400)
                    {
                        this.SpeedCruise += 1 / 3.6f;
                        this.CruiseTimeout = DateTime.Now;
                    }

                    return false;
                    break;
                case JoyControls.CruiseControlDown:
                    if (val && DateTime.Now.Subtract(this.CruiseTimeout).TotalMilliseconds > 400)
                    {
                        this.SpeedCruise -= 1 / 3.6f;
                        this.CruiseTimeout = DateTime.Now;
                    }

                    return false;
                    break;
                case JoyControls.CruiseControlOnOff:
                    if (val && DateTime.Now.Subtract(this.CruiseTimeout).TotalMilliseconds > 500)
                    {
                        this.Cruising = !this.Cruising;
                        Debug.WriteLine("Cruising set to " + this.Cruising);
                        this.CruiseTimeout = DateTime.Now;
                    }

                    return false;
                    break;
                default: return val;
            }
        }

        public bool Requires(JoyControls c)
        {
            switch (c)
            {
                case JoyControls.Throttle:
                case JoyControls.Brake: return this.Cruising;

                case JoyControls.CruiseControlMaintain:
                case JoyControls.CruiseControlUp:
                case JoyControls.CruiseControlDown:
                case JoyControls.CruiseControlOnOff: return true;

                default: return false;
            }
        }

        public void ResetParameters()
        {
            this.SpeedCruise = 1000 / 3.6;
            this.PSlope = 0.25;
            this.ISlope = 0;
            this.Imax = 0;
            this.DSlope = 0;
        }

        public void TickControls()
        { }

        public void TickTelemetry(IDataMiner data)
        {
            this.Speed = data.Telemetry.Speed;
        }
    }
}