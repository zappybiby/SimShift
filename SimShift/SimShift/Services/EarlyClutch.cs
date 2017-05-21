using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SimShift.Data.Common;
using SimShift.Entities;

namespace SimShift.Services
{
    /// <summary>
    ///     This module is a gimmick where the clutch is engaged when dropping below 35km/h when the vehicle was driving faster
    ///     than that (55km/h+)
    /// </summary>
    class EarlyClutch : IControlChainObj
    {
        private bool clutchctrl = false;

        private bool clutching = false;

        private bool triggered = false;

        public bool Active { get; private set; }

        public bool Enabled { get; private set; }

        public IEnumerable<string> SimulatorsBan => new string[0];

        public IEnumerable<string> SimulatorsOnly => new string[0];

        public double GetAxis(JoyControls c, double val)
        {
            switch (c)
            {
                case JoyControls.Clutch: return this.clutching ? 1 : val;
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
                case JoyControls.Clutch: return this.clutching;
                default: return false;
            }
        }

        public void TickControls()
        {
            // clutching = Main.GetAxisIn(JoyControls.Throttle) < 0.1 && clutchctrl;
            this.clutching = false;
        }

        public void TickTelemetry(IDataMiner data)
        {
            if (data.Telemetry.Speed * 3.6 > 55)
            {
                this.triggered = true;
            }

            if (this.triggered && data.Telemetry.Speed * 3.6 < 35)
            {
                this.clutchctrl = true;
            }
            else if (data.Telemetry.Speed * 3.6 > 35)
            {
                this.clutchctrl = false;
            }

            if (data.Telemetry.Speed * 3.6 < 10 && Main.GetAxisIn(JoyControls.Throttle) > 0.1)
            {
                this.clutchctrl = false;
                this.triggered = false;
            }
        }
    }
}