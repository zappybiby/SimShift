using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;

using AForge.Imaging.Filters;

using SimShift.Data;
using SimShift.Data.Common;
using SimShift.Entities;
using SimShift.Utils;

namespace SimShift.Services
{
    /// <summary>
    ///     Limits maximum speed of vehicle to avoid reckless driving, or create realism for some vehicles (eg 255km/h limit on
    ///     german saloon cars).
    ///     Maximum speed can be adjusted and must be set some km/h lower than the desired speed. Aggresiveness is determined
    ///     by slope.
    /// </summary>
    public class Speedlimiter : IControlChainObj, IConfigurable
    {
        private double brakeFactor;

        private double limiterFactor;

        public IEnumerable<string> AcceptsConfigs => new[] { "Speedlimit" };

        public bool Active => this.limiterFactor < 0.99;

        public bool Adaptive { get; private set; }

        public bool Enabled { get; private set; }

        public IEnumerable<string> SimulatorsBan => new string[0];

        public IEnumerable<string> SimulatorsOnly => new string[0];

        public int SpeedLimit { get; private set; }

        public float SpeedSlope { get; private set; }

        public void ApplyParameter(IniValueObject obj)
        {
            switch (obj.Key)
            {
                case "Adaptive":
                    this.Adaptive = obj.ReadAsInteger() == 1;
                    break;

                case "Limit":
                    this.SpeedLimit = obj.ReadAsInteger();
                    break;

                case "Slope":
                    this.SpeedSlope = obj.ReadAsFloat();
                    break;

                case "Disable":
                    this.Enabled = false;
                    break;
            }
        }

        public IEnumerable<IniValueObject> ExportParameters()
        {
            List<IniValueObject> exportedObjects = new List<IniValueObject>();

            if (this.Enabled == false)
            {
                exportedObjects.Add(new IniValueObject(this.AcceptsConfigs, "Disable", "1"));
            }
            else
            {
                exportedObjects.Add(new IniValueObject(this.AcceptsConfigs, "Limit", this.SpeedLimit.ToString()));
                exportedObjects.Add(new IniValueObject(this.AcceptsConfigs, "Slope", this.SpeedSlope.ToString()));
            }

            return exportedObjects;
        }

        public double GetAxis(JoyControls c, double val)
        {
            switch (c)
            {
                case JoyControls.Throttle: return val * this.limiterFactor;
                case JoyControls.Brake: return this.brakeFactor;

                default: return val;
            }
        }

        public bool GetButton(JoyControls c, bool val)
        {
            return val;
        }

        public bool Requires(JoyControls c)
        {
            switch (c)
            {
                case JoyControls.Throttle: return true;
                case JoyControls.Brake: return this.brakeFactor > 0.005;
                default: return false;
            }
        }

        public void ResetParameters()
        {
            this.SpeedLimit = 255;
            this.SpeedSlope = 10;
            this.Enabled = true;
            this.Adaptive = false;
        }

        public void TickControls()
        { }

        public void TickTelemetry(IDataMiner data)
        {
            if (this.Adaptive && Main.Data.Active.Application == "eurotrucks2")
            {
                var ets2data = (Ets2DataMiner) Main.Data.Active;
                var ets2limit = ets2data.MyTelemetry.Job.SpeedLimit * 3.6 - 4;
                if (ets2limit < 0)
                {
                    ets2limit = 120;
                }

                this.SpeedLimit = (int) ets2limit;
            }

            if (!this.Enabled)
            {
                this.brakeFactor = 0;
                this.limiterFactor = 1;
            }
            else
            {
                this.limiterFactor = 1 + (this.SpeedLimit - data.Telemetry.Speed * 3.6) / this.SpeedSlope;

                if (this.limiterFactor < 0)
                {
                    this.limiterFactor = 0;
                }

                if (this.limiterFactor > 1)
                {
                    this.limiterFactor = 1;
                }

                if (data.Telemetry.Speed * 3.6 - 2 >= this.SpeedLimit)
                {
                    var err = (data.Telemetry.Speed * 3.6 - 3 - this.SpeedLimit) / 25.0 * 0.15f;
                    this.brakeFactor = err;
                    this.limiterFactor = 0;
                }
                else
                {
                    this.brakeFactor = 0;
                }
            }

            if (data.Telemetry.EngineRpm > 21750)
            {
                this.Enabled = true;
                this.limiterFactor *= Math.Max(0, 1 - (data.Telemetry.EngineRpm - 1750) / 350.0f);
            }

            var pwrLimiter = Main.Drivetrain.CalculateThrottleByPower(data.Telemetry.EngineRpm, 1000);

            if (pwrLimiter > 1)
            {
                pwrLimiter = 1;
            }

            if (pwrLimiter < 0.2)
            {
                pwrLimiter = 0.2;
            }

            this.limiterFactor *= pwrLimiter;
        }
    }
}