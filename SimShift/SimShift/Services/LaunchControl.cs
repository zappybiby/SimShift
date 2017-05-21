using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using SimShift.Data.Common;
using SimShift.Entities;

namespace SimShift.Services
{
    public enum LaunchControlState
    {
        Inactive,

        Waiting,

        Revving,

        Pulling,

        Slipping
    }

    /// <summary>
    ///     Launch Control was developed for TDU2 in order to take off with least amount of wheelspin from the line.
    ///     The module needs a user procedure to activate (engage gear, press brake, activate LC, press throttle, release
    ///     brake).
    /// </summary>
    public class LaunchControl : IControlChainObj
    {
        private double _outClutch;

        private double _outThrottle;

        private double previousAcc = 0;

        private double previousSpeed = 0;

        private DateTime previousTime;

        private double pullThrottle = 0;

        private bool revvedUp = false;

        private LaunchControlState state;

        public LaunchControl()
        {
            this.LaunchRpm = 4000;
            Main.Data.CarChanged += new EventHandler(this.Data_CarChanged);
            this.PullingClutchProp = 1;
            this.PullingThrottleProp = 4;
            this.RevvingProp = 4;
            this.PeakAcceleration = 10;
        }

        public bool Active => this.state == LaunchControlState.Pulling;

        public bool Enabled => this.state != LaunchControlState.Waiting;

        public double LaunchRpm { get; private set; }

        public double PeakAcceleration { get; private set; }

        public double PullingClutchProp { get; private set; }

        public double PullingThrottleProp { get; private set; }

        public double RevvingProp { get; private set; }

        public IEnumerable<string> SimulatorsBan => new string[0];

        public IEnumerable<string> SimulatorsOnly => new string[0];

        public bool TemporaryLoadTc { get; private set; }

        protected bool LaunchControlActive { get; set; }

        private bool tcLoaded { get; set; }

        public double GetAxis(JoyControls c, double val)
        {
            switch (c)
            {
                case JoyControls.Throttle: return this._outThrottle;

                case JoyControls.Clutch: return this._outClutch;

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
                case JoyControls.Throttle:
                case JoyControls.Clutch: return this.LaunchControlActive;

                default: return false;
            }
        }

        public void TickControls()
        {
            var br = Main.GetAxisIn(JoyControls.Brake);
            var th = Main.GetAxisIn(JoyControls.Throttle);

            switch (this.state)
            {
                case LaunchControlState.Inactive:
                    if (Main.GetButtonIn(JoyControls.LaunchControl) && br > 0.05)
                    {
                        Debug.WriteLine("LC activated - waiting");
                        this.state = LaunchControlState.Waiting;
                    }

                    break;

                case LaunchControlState.Waiting:
                    if (th > 0.1 && br > 0.05)
                    {
                        Debug.WriteLine("Revving up engine, waiting for brake to be released");
                        this.state = LaunchControlState.Revving;
                    }
                    else if (br < 0.05)
                    {
                        Debug.WriteLine("Brake released,  stopped waiting");
                        this.state = LaunchControlState.Inactive;
                    }

                    break;

                case LaunchControlState.Revving:
                    if (this.revvedUp && br < 0.05)
                    {
                        // engine is ready & user lets go of brake
                        Debug.WriteLine("GO GO GO");
                        this.state = LaunchControlState.Pulling;
                    }

                    if (th < 0.1)
                    {
                        // user lets go of throttle
                        Debug.WriteLine("Back to idle");
                        this.state = LaunchControlState.Waiting;
                    }

                    break;

                case LaunchControlState.Pulling:
                case LaunchControlState.Slipping:
                    if (th < 0.1 || br > 0.05)
                    {
                        Debug.WriteLine("aborting");
                        this.state = LaunchControlState.Inactive; // abort
                    }

                    break;
            }
        }

        public void TickTelemetry(IDataMiner data)
        {
            var acc = (data.Telemetry.Speed - this.previousSpeed) / (DateTime.Now.Subtract(this.previousTime).TotalMilliseconds / 1000);
            var pullSpeed = Main.Drivetrain.CalculateSpeedForRpm(data.Telemetry.Gear - 1, data.Telemetry.EngineRpm);

            this.LaunchControlActive = this.state != LaunchControlState.Inactive;

            switch (this.state)
            {
                case LaunchControlState.Inactive: break;

                case LaunchControlState.Waiting:
                    this._outThrottle = 0;
                    this._outClutch = 1;
                    break;

                case LaunchControlState.Revving:
                    this._outThrottle = this.RevvingProp - this.RevvingProp * data.Telemetry.EngineRpm / this.LaunchRpm;
                    this._outClutch = 1;
                    this.revvedUp = Math.Abs(data.Telemetry.EngineRpm - this.LaunchRpm) < 300;
                    break;

                case LaunchControlState.Pulling:
                    this._outThrottle = this.PullingThrottleProp - this.PullingThrottleProp * data.Telemetry.EngineRpm / this.LaunchRpm;
                    this._outClutch = 1 - this.PullingClutchProp * this.previousAcc / this.PeakAcceleration;
                    if (this._outClutch > 0.8)
                    {
                        this._outClutch = 0.8;
                    }

                    if (data.Telemetry.EngineRpm - 300 > this.LaunchRpm)
                    {
                        this.state = LaunchControlState.Slipping;
                    }

                    break;

                case LaunchControlState.Slipping:
                    // revving is less harder to do than pulling
                    // so we switch back to the revving settings, and when the wheelspin is over we go back.
                    this._outThrottle = this.RevvingProp - this.RevvingProp * data.Telemetry.EngineRpm / this.LaunchRpm;
                    this._outClutch = 1 - this.PullingClutchProp * this.previousAcc / this.PeakAcceleration;
                    if (this._outClutch > 0.8)
                    {
                        this._outClutch = 0.8;
                    }

                    if (data.Telemetry.EngineRpm < this.LaunchRpm)
                    {
                        this.state = LaunchControlState.Pulling;
                    }

                    break;
            }

            if (this.TemporaryLoadTc)
            {
                if (!this.tcLoaded && data.Telemetry.Gear == 1 && this.LaunchControlActive && Main.TractionControl.File.Contains("notc"))
                {
                    this.tcLoaded = true;
                    Main.Load(Main.TractionControl, "Settings/TractionControl/launch.ini");
                }

                if (this.tcLoaded && data.Telemetry.Gear != 1)
                {
                    this.tcLoaded = false;
                    Main.Load(Main.TractionControl, "Settings/TractionControl/notc.ini");
                }
            }

            if (this.LaunchControlActive && data.Telemetry.Speed > pullSpeed * 0.95)
            {
                Debug.WriteLine("Done pulling!");

                // We're done pulling
                this.state = LaunchControlState.Inactive;
            }

            if (this._outThrottle > 1)
            {
                this._outThrottle = 1;
            }

            if (this._outThrottle < 0)
            {
                this._outThrottle = 0;
            }

            if (this._outClutch > 1)
            {
                this._outClutch = 1;
            }

            if (this._outClutch < 0)
            {
                this._outClutch = 0;
            }

            this.previousSpeed = data.Telemetry.Speed;
            this.previousTime = DateTime.Now;
            this.previousAcc = acc * 0.25 + this.previousAcc * 0.75;

            // Debug.WriteLine(previousAcc);
        }

        void Data_CarChanged(object sender, EventArgs e)
        {
            this.LaunchRpm = Main.Drivetrain.MaximumRpm / 3 + 1000;
            this.LaunchRpm = Main.Drivetrain.StallRpm * 3;
            this.LaunchRpm = Main.Drivetrain.MaximumRpm - 500;
            this.LaunchRpm = 3000;
            if (this.LaunchRpm > Main.Drivetrain.MaximumRpm)
            {
                this.LaunchRpm = Main.Drivetrain.StallRpm * 2.5;
            }

            this.RevvingProp = this.LaunchRpm / 1000 - 2.25;
            this.PullingThrottleProp = this.LaunchRpm / 1000 - 1.75;
            if (this.RevvingProp < 1)
            {
                this.PullingThrottleProp++;
                this.RevvingProp = 1;
            }

            this.TemporaryLoadTc = true;
        }
    }
}