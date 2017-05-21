using System;
using System.Collections.Generic;

using SimShift.Data;
using SimShift.Data.Common;
using SimShift.Entities;

namespace SimShift.Services
{
    /// <summary>
    ///     This is an budget TrackIR feature for TDU2, designed to look side-ways when a look-around stick is moved.
    ///     The look side-ways auto centers. Control of camera angle is done with ProcessMemoryWrite access.
    /// </summary>
    public class CameraHorizon : IControlChainObj
    {
        public double CameraAngle = 0;

        public bool Active => Math.Abs(this.CameraAngle) > 0.05;

        public bool CameraHackEnabled { get; set; }

        public bool Enabled { get; private set; }

        public IEnumerable<string> SimulatorsBan => new string[0];

        public IEnumerable<string> SimulatorsOnly => new[] { "TestDrive2" };

        public double GetAxis(JoyControls c, double val)
        {
            return val;
        }

        public bool GetButton(JoyControls c, bool val)
        {
            return val;
        }

        public bool Requires(JoyControls c)
        {
            switch (c)
            {
                case JoyControls.CameraHorizon: return true;

                default: return false;
            }
        }

        public void TickControls()
        {
            this.CameraAngle = Main.GetAxisIn(JoyControls.CameraHorizon) * 0.1 + this.CameraAngle * 0.9;
        }

        public void TickTelemetry(IDataMiner data)
        {
            // TODO: Only supports TDU2.
            if (Main.Data.Active.Application != "TestDrive2")
            {
                this.Enabled = false;
                return;
            }
            else
            {
                this.Enabled = true;
            }

            if (this.CameraHackEnabled)
            {
                data.Write(TelemetryChannel.CameraHorizon, (float) (this.CameraAngle * this.CameraAngle * this.CameraAngle * -25));
            }
            else if (this.CameraAngle != 0)
            {
                this.CameraAngle = 0;
                data.Write(TelemetryChannel.CameraHorizon, 0.0f);
            }
        }
    }
}