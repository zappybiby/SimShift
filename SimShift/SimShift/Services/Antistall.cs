using System;
using System.Collections.Generic;
using System.Diagnostics;

using SimShift.Data;
using SimShift.Data.Common;
using SimShift.Entities;
using SimShift.Utils;

namespace SimShift.Services
{
    /// <summary>
    ///     This module is the "Auto-Clutch" feature and engages the clutch when the engine is about to stall.
    ///     It also ensures smooth get-away when the user engages throttle when the vehicle has stopped.
    /// </summary>
    public class Antistall : IControlChainObj, IConfigurable
    {
        protected bool BlipFull;

        private double _throttle;

        private double AntiStallRpm;

        public Antistall()
        {
            this.Enabled = true;
        }

        public static bool Stalling { get; private set; }

        public IEnumerable<string> AcceptsConfigs => new[] { "Antistall" };

        public bool Active => Stalling;

        public bool Enabled { get; set; }

        public double EngineRpm { get; set; }

        public double MinClutch { get; private set; }

        public bool Override { get; set; }

        public bool ReverseAndAccelerate { get; private set; }

        public double Rpm { get; private set; }

        public IEnumerable<string> SimulatorsBan => new string[0];

        public IEnumerable<string> SimulatorsOnly => new string[0];

        public bool SlipLowGear { get; private set; }

        public double Speed { get; private set; }

        public double SpeedCutoff { get; private set; }

        public double ThrottleSensitivity { get; private set; }

        protected bool EngineStalled { get; set; }

        private bool SlippingLowGear { get; set; }

        public void ApplyParameter(IniValueObject obj)
        {
            switch (obj.Key)
            {
                case "MinClutch":
                    this.MinClutch = obj.ReadAsDouble();
                    break;
                case "ThrottleSensitivity":
                    this.ThrottleSensitivity = obj.ReadAsDouble();
                    break;
            }
        }

        public IEnumerable<IniValueObject> ExportParameters()
        {
            var group = new List<string>(new[] { "Antistall" });
            var parameters = new List<IniValueObject>();

            parameters.Add(new IniValueObject(group, "MinClutch", this.SpeedCutoff.ToString("0.00")));
            parameters.Add(new IniValueObject(group, "ThrottleSensitivity", this.SpeedCutoff.ToString("0.00")));

            return parameters;
        }

        public double GetAxis(JoyControls c, double val)
        {
            switch (c)
            {
                case JoyControls.Throttle:
                    if (this.ReverseAndAccelerate)
                    {
                        return 0;
                    }

                    this._throttle = val;
                    return val;
                    if (this.EngineRpm > this.AntiStallRpm)
                    {
                        val /= 5 * (this.EngineRpm - this.AntiStallRpm) / this.AntiStallRpm;
                    }

                    this._throttle = val;
                    return val;
                    if (this.Override)
                    {
                        return 1;
                    }

                    if (!Stalling)
                    {
                        return val;
                    }

                    if (this.EngineStalled)
                    {
                        this._throttle = val;
                        return val;
                    }

                    if (val < 0.01)
                    {
                        this._throttle = 0;
                        return 0;
                    }
                    else
                    {
                        var maxRpm = Main.Drivetrain.StallRpm * 1.4;
                        var maxV = 2 - 2 * this.Rpm / maxRpm;
                        if (maxV > 1)
                        {
                            maxV = 1;
                        }

                        if (maxV < 0)
                        {
                            maxV = 0;
                        }

                        this._throttle = val;
                        return maxV;
                    }

                    break;

                case JoyControls.Clutch:
                    if (this.ReverseAndAccelerate)
                    {
                        return 1;
                    }

                    if (Stalling && this._throttle < 0.01)
                    {
                        if (this.SpeedCutoff >= this.Speed)
                        {
                            return 1;
                        }
                        else
                        {
                            return 1 - (this.Speed - this.SpeedCutoff) / 0.5f;
                        }
                    }

                    var cl = 1 - this._throttle * this.ThrottleSensitivity; // 2
                    if (cl < this.MinClutch)
                    {
                        cl = this.MinClutch; // 0.1
                    }

                    cl = Math.Max(cl, val);

                    if (this.EngineRpm < this.AntiStallRpm)
                    {
                        cl += (this.AntiStallRpm - this.EngineRpm) / this.AntiStallRpm;
                    }

                    return cl;

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
                case JoyControls.Throttle: return this.Enabled && Stalling;

                case JoyControls.Clutch: return this.Enabled && Stalling;

                default: return false;
            }
        }

        public void ResetParameters()
        {
            // Reset to default
            this.MinClutch = 0.1;
            this.ThrottleSensitivity = 2;
        }

        public void TickControls()
        { }

        public void TickTelemetry(IDataMiner telemetry)
        {
            bool wasStalling = Stalling;
            bool wasEngineStalled = this.EngineStalled;

            this.Rpm = telemetry.Telemetry.EngineRpm;
            this.EngineStalled = telemetry.Telemetry.EngineRpm < 300;
            this.AntiStallRpm = Main.Drivetrain.StallRpm * 1.25f;

            var gear = telemetry.Telemetry.Gear - 1;
            if (gear == -2)
            {
                gear = 0;
            }

            if (gear == 0)
            {
                gear = 1;
            }

            this.SpeedCutoff = Main.Drivetrain.CalculateSpeedForRpm(gear, (float) this.AntiStallRpm);

            if (telemetry.Telemetry.Gear == -1)
            {
                this.ReverseAndAccelerate = telemetry.Telemetry.Speed > 0.5;
                Stalling = telemetry.Telemetry.Speed + 0.25 >= -this.SpeedCutoff;
            }
            else
            {
                this.ReverseAndAccelerate = telemetry.Telemetry.Speed < -0.5;
                Stalling = telemetry.Telemetry.Speed - 0.25 <= this.SpeedCutoff;
            }

            Stalling |= this.ReverseAndAccelerate;

            this.Speed = telemetry.Telemetry.Speed;
            this.EngineRpm = telemetry.Telemetry.EngineRpm;

            if (this.EngineStalled && this._throttle > 0)
            {
                this.Override = true;
            }
            else
            {
                this.Override = false;
            }
        }
    }
}