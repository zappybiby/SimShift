using System;
using System.Collections.Generic;

using SimShift.Data.Common;
using SimShift.Entities;

namespace SimShift.Services
{
    /// <summary>
    ///     This module is a gimmick to simulate a torque limiter for the drive wheels. It practically limits the throttle in
    ///     lower gears, because it may exceed torque limit of the drivetrain.
    ///     /
    /// </summary>
    class WheelTorqueLimiter : IControlChainObj
    {
        private double TorqueLimit = 0;

        public bool Active { get; private set; }

        public bool Enabled { get; private set; }

        public IEnumerable<string> SimulatorsBan => new string[0];

        public IEnumerable<string> SimulatorsOnly => new string[0];

        public double GetAxis(JoyControls c, double val)
        {
            if (c == JoyControls.Throttle)
            {
                return val * this.TorqueLimit;
            }
            else
            {
                return val;
            }
        }

        public bool GetButton(JoyControls c, bool val)
        {
            return val;
        }

        public bool Requires(JoyControls c)
        {
            return c == JoyControls.Throttle;
        }

        public void TickControls()
        { }

        public void TickTelemetry(IDataMiner data)
        {
            this.Enabled = true;
            this.Active = true;

            var f = 1.0;
            if (Main.Drivetrain.GearRatios != null && data.Telemetry.Gear >= 1)
            {
                f = Main.Drivetrain.GearRatios[data.Telemetry.Gear - 1] / 5.5;
            }

            var throttle = Math.Max(Main.GetAxisIn(JoyControls.Throttle), data.Telemetry.Throttle);
            this.TorqueLimit = 1;
            var NotGood = false;
            do
            {
                var wheelTorque = Main.Drivetrain.CalculateTorqueP(data.Telemetry.EngineRpm, this.TorqueLimit * throttle) * f;
                if (wheelTorque > 20000)
                {
                    this.TorqueLimit *= 0.999f;
                    NotGood = true;
                }
                else
                {
                    NotGood = false;
                }

                if (this.TorqueLimit <= 0.2f)
                {
                    this.TorqueLimit = 0.2f;
                    break;
                }
            }
            while (NotGood);

            this.TorqueLimit = 1.0f;
        }
    }
}