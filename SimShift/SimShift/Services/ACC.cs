using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using SimShift.Data.Common;
using SimShift.Dialogs;
using SimShift.Entities;

namespace SimShift.Services
{
    public class ACC : IControlChainObj
    {
        private float b;

        private float t;

        public bool Active => this.Cruising;

        public bool Cruising { get; private set; }

        public bool Enabled => true;

        public IEnumerable<string> SimulatorsBan => new string[0];

        public IEnumerable<string> SimulatorsOnly => new string[0];

        public double Speed { get; private set; }

        public double SpeedCruise { get; private set; }

        private DateTime CruiseTimeout { get; set; }

        public double GetAxis(JoyControls c, double val)
        {
            switch (c)
            {
                case JoyControls.Throttle:
                    if (this.Cruising)
                    {
                        return Math.Min(1, Math.Max(0, this.t));
                    }
                    else
                    {
                        return val;
                    }

                case JoyControls.Brake:
                    if (this.Cruising)
                    {
                        return Math.Min(1, Math.Max(0, this.b));
                    }
                    else
                    {
                        return val;
                    }

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

        public void TickControls()
        { }

        public void TickTelemetry(IDataMiner data)
        {
            this.Speed = data.Telemetry.Speed;

            if (Main.GetAxisIn(JoyControls.Brake) > 0.05)
            {
                this.Cruising = false;
            }

            // Any tracked car?
            if (dlDebugInfo.TrackedCar != null)
            {
                // Gap control?
                var distanceTarget = 20;
                var distanceError = distanceTarget - dlDebugInfo.TrackedCar.Distance;

                var speedBias = 9 * distanceError / distanceTarget; // 3m/s max decrement
                if (distanceError < 0)
                {
                    speedBias /= 6;
                }

                var targetSpeed = dlDebugInfo.TrackedCar.Speed - speedBias;

                if (targetSpeed >= this.SpeedCruise)
                {
                    targetSpeed = (float) this.SpeedCruise;
                }

                var speedErr = data.Telemetry.Speed - targetSpeed - 2;
                if (speedErr > 0)
                {
                    // too fast
                    this.t = 0;
                    if (speedErr > 1.5f)
                    {
                        this.b = (float) Math.Pow(speedErr - 1.5f, 4) * 0.015f;
                    }
                }
                else
                {
                    this.t = -speedErr * 0.2f;
                    this.b = 0;
                }
            }
            else
            {
                // Speed control
                var speedErr = data.Telemetry.Speed - (float) this.SpeedCruise;
                if (speedErr > 0)
                {
                    // too fast
                    this.t = 0;
                }
                else
                {
                    this.t = -speedErr * 0.4f;
                    this.b = 0;
                }
            }
        }
    }
}