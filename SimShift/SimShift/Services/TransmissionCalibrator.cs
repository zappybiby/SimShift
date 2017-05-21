using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SimShift.Data.Common;
using SimShift.Entities;
using SimShift.Utils;

namespace SimShift.Services
{
    public enum TransmissionCalibratorStatus
    {
        Idle,

        FindThrottlePoint,

        FindClutchBitePoint,

        IterateCalibrationCycle
    }

    /// <summary>
    ///     This module calibrates transmission whenever it can.
    /// </summary>
    public class TransmissionCalibrator : IControlChainObj
    {
        public float err = 0.0f;

        private float calRpm = 0.0f;

        private float clutch = 0.0f;

        private int gearTest = 1;

        private bool isStationary = false;

        private bool rpmInRange = false;

        private DateTime rpmInRangeTimer = DateTime.Now;

        private TransmissionCalibratorStatus state = TransmissionCalibratorStatus.Idle;

        private DateTime stationary = DateTime.Now;

        private float throttle = 0.0f;

        public bool Active { get; private set; }

        public bool Enabled => true;

        public IEnumerable<string> SimulatorsBan => new string[0];

        public IEnumerable<string> SimulatorsOnly => new string[0];

        public TransmissionCalibratorStatus State => this.state;

        public double GetAxis(JoyControls c, double val)
        {
            if (!this.Active)
            {
                return val;
            }

            switch (c)
            {
                case JoyControls.Throttle: return this.throttle;
                case JoyControls.Clutch: return this.clutch;
                default: return val;
            }
        }

        public bool GetButton(JoyControls c, bool val)
        {
            return val;
        }

        public bool Requires(JoyControls c)
        {
            if (this.Active == false)
            {
                return false;
            }
            else
            {
                if (c == JoyControls.Clutch)
                {
                    return true;
                }

                if (c == JoyControls.Throttle)
                {
                    return true;
                }

                return false;
            }
        }

        public void TickControls()
        { }

        public void TickTelemetry(IDataMiner data)
        {
            var rpm = data.Telemetry.EngineRpm;
            this.err = this.calRpm - rpm;
            if (this.err > 500)
            {
                this.err = 500;
            }

            if (this.err < -500)
            {
                this.err = -500;
            }

            switch (this.state)
            {
                case TransmissionCalibratorStatus.Idle:
                    Main.Transmission.OverruleShifts = false;

                    // If we're stationary for a few seconds, we can apply brakes and calibrate the clutch
                    bool wasStationary = this.isStationary;
                    this.isStationary = Math.Abs(data.Telemetry.Speed) < 1 && Main.GetAxisIn(JoyControls.Throttle) < 0.05 && data.Telemetry.EngineRpm > 200;

                    if (this.isStationary && !wasStationary)
                    {
                        this.stationary = DateTime.Now;
                    }

                    if (this.isStationary && DateTime.Now.Subtract(this.stationary).TotalMilliseconds > 2500)
                    {
                        this.clutch = 1.0f;

                        // CALIBRATE
                        this.state = TransmissionCalibratorStatus.IterateCalibrationCycle;
                    }

                    break;

                case TransmissionCalibratorStatus.FindThrottlePoint:

                    this.throttle += this.err / 500000.0f;
                    if (this.throttle >= 0.8)
                    {
                        // Cannot rev the engine
                    }

                    var wasRpmInRange = this.rpmInRange;
                    this.rpmInRange = Math.Abs(this.err) < 5;

                    if (!wasRpmInRange && this.rpmInRange)
                    {
                        this.rpmInRangeTimer = DateTime.Now;
                    }

                    // stable at RPM
                    if (this.rpmInRange && DateTime.Now.Subtract(this.rpmInRangeTimer).TotalMilliseconds > 250)
                    {
                        // set error to 0
                        this.calRpm = rpm;
                        this.state = TransmissionCalibratorStatus.FindClutchBitePoint;
                    }

                    break;

                case TransmissionCalibratorStatus.FindClutchBitePoint:

                    if (Main.Transmission.IsShifting)
                    {
                        break;
                    }

                    if (data.Telemetry.Gear != this.gearTest)
                    {
                        Main.Transmission.Shift(data.Telemetry.Gear, this.gearTest, "normal");
                        break;
                    }

                    // Decrease clutch 0.25% at a time to find the bite point
                    this.clutch -= 0.25f / 100.0f;

                    if (this.err > 50)
                    {
                        // We found the clutch bite point
                        var fs = File.AppendText("./gear-clutch");
                        fs.WriteLine(this.gearTest + "," + this.calRpm + "," + this.clutch);
                        fs.Close();
                        this.state = TransmissionCalibratorStatus.IterateCalibrationCycle;
                    }

                    break;

                case TransmissionCalibratorStatus.IterateCalibrationCycle:

                    this.clutch = 1;
                    this.gearTest++;

                    this.calRpm = 700.0f;

                    // Find throttle point
                    this.state = TransmissionCalibratorStatus.FindThrottlePoint;
                    this.throttle = 0.001f;

                    this.rpmInRange = false;

                    break;
            }

            // abort when we give power
            if (Main.GetAxisIn(JoyControls.Throttle) > 0.1)
            {
                this.stationary = DateTime.Now;
                this.state = TransmissionCalibratorStatus.Idle;
            }

            this.Active = this.state != TransmissionCalibratorStatus.Idle;
        }
    }
}