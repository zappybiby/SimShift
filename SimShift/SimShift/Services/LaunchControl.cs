﻿using System;
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
    /// Launch Control was developed for TDU2 in order to take off with least amount of wheelspin from the line.
    /// The module needs a user procedure to activate (engage gear, press brake, activate LC, press throttle, release brake).
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
            LaunchRpm = 4000;
            Main.Data.CarChanged += new EventHandler(Data_CarChanged);
            PullingClutchProp = 1;
            PullingThrottleProp = 4;
            RevvingProp = 4;
            PeakAcceleration = 10;
        }

        public bool Active
        {
            get
            {
                return state == LaunchControlState.Pulling;
            }
        }

        public bool Enabled
        {
            get
            {
                return state != LaunchControlState.Waiting;
            }
        }

        public double LaunchRpm { get; private set; }

        public double PeakAcceleration { get; private set; }

        public double PullingClutchProp { get; private set; }

        public double PullingThrottleProp { get; private set; }

        public double RevvingProp { get; private set; }

        public IEnumerable<string> SimulatorsBan
        {
            get
            {
                return new String[0];
            }
        }

        public IEnumerable<string> SimulatorsOnly
        {
            get
            {
                return new String[0];
            }
        }

        public bool TemporaryLoadTc { get; private set; }

        //
        protected bool LaunchControlActive { get; set; }

        private bool tcLoaded { get; set; }

        public double GetAxis(JoyControls c, double val)
        {
            switch (c)
            {
                case JoyControls.Throttle: return _outThrottle;

                case JoyControls.Clutch: return _outClutch;

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
                case JoyControls.Clutch: return LaunchControlActive;

                default: return false;
            }
        }

        public void TickControls()
        {
            var br = Main.GetAxisIn(JoyControls.Brake);
            var th = Main.GetAxisIn(JoyControls.Throttle);

            switch (state)
            {
                case LaunchControlState.Inactive:
                    if (Main.GetButtonIn(JoyControls.LaunchControl) && br > 0.05)
                    {
                        Debug.WriteLine("LC activated - waiting");
                        state = LaunchControlState.Waiting;
                    }
                    break;

                case LaunchControlState.Waiting:
                    if (th > 0.1 && br > 0.05)
                    {
                        Debug.WriteLine("Revving up engine, waiting for brake to be released");
                        state = LaunchControlState.Revving;
                    }
                    else if (br < 0.05)
                    {
                        Debug.WriteLine("Brake released,  stopped waiting");
                        state = LaunchControlState.Inactive;
                    }
                    break;

                case LaunchControlState.Revving:
                    if (revvedUp && br < 0.05) // engine is ready & user lets go of brake
                    {
                        Debug.WriteLine("GO GO GO");
                        state = LaunchControlState.Pulling;
                    }

                    if (th < 0.1) // user lets go of throttle
                    {
                        Debug.WriteLine("Back to idle");
                        state = LaunchControlState.Waiting;
                    }

                    break;

                case LaunchControlState.Pulling:
                case LaunchControlState.Slipping:
                    if (th < 0.1 || br > 0.05)
                    {
                        Debug.WriteLine("aborting");
                        state = LaunchControlState.Inactive; // abort
                    }
                    break;
            }
        }

        public void TickTelemetry(IDataMiner data)
        {
            var acc = (data.Telemetry.Speed - previousSpeed)
                      / (DateTime.Now.Subtract(previousTime).TotalMilliseconds / 1000);
            var pullSpeed = Main.Drivetrain.CalculateSpeedForRpm(data.Telemetry.Gear - 1, data.Telemetry.EngineRpm);

            LaunchControlActive = state != LaunchControlState.Inactive;

            switch (state)
            {
                case LaunchControlState.Inactive: break;

                case LaunchControlState.Waiting:
                    _outThrottle = 0;
                    _outClutch = 1;
                    break;

                case LaunchControlState.Revving:
                    _outThrottle = RevvingProp - RevvingProp * data.Telemetry.EngineRpm / LaunchRpm;
                    _outClutch = 1;
                    revvedUp = Math.Abs(data.Telemetry.EngineRpm - LaunchRpm) < 300;
                    break;

                case LaunchControlState.Pulling:
                    _outThrottle = PullingThrottleProp - PullingThrottleProp * data.Telemetry.EngineRpm / LaunchRpm;
                    _outClutch = 1 - PullingClutchProp * previousAcc / PeakAcceleration;
                    if (_outClutch > 0.8) _outClutch = 0.8;

                    if (data.Telemetry.EngineRpm - 300 > LaunchRpm) state = LaunchControlState.Slipping;
                    break;

                case LaunchControlState.Slipping:
                    // revving is less harder to do than pulling
                    // so we switch back to the revving settings, and when the wheelspin is over we go back.
                    _outThrottle = RevvingProp - RevvingProp * data.Telemetry.EngineRpm / LaunchRpm;
                    _outClutch = 1 - PullingClutchProp * previousAcc / PeakAcceleration;
                    if (_outClutch > 0.8) _outClutch = 0.8;

                    if (data.Telemetry.EngineRpm < LaunchRpm) state = LaunchControlState.Pulling;

                    break;
            }

            if (TemporaryLoadTc)
            {
                if (!tcLoaded && data.Telemetry.Gear == 1 && LaunchControlActive
                    && Main.TractionControl.File.Contains("notc"))
                {
                    tcLoaded = true;
                    Main.Load(Main.TractionControl, "Settings/TractionControl/launch.ini");
                }

                if (tcLoaded && data.Telemetry.Gear != 1)
                {
                    tcLoaded = false;
                    Main.Load(Main.TractionControl, "Settings/TractionControl/notc.ini");
                }
            }

            if (LaunchControlActive && data.Telemetry.Speed > pullSpeed * 0.95)
            {
                Debug.WriteLine("Done pulling!");
                // We're done pulling
                state = LaunchControlState.Inactive;
            }

            if (_outThrottle > 1) _outThrottle = 1;
            if (_outThrottle < 0) _outThrottle = 0;

            if (_outClutch > 1) _outClutch = 1;
            if (_outClutch < 0) _outClutch = 0;

            previousSpeed = data.Telemetry.Speed;
            previousTime = DateTime.Now;
            previousAcc = acc * 0.25 + previousAcc * 0.75;

            //Debug.WriteLine(previousAcc);
        }

        void Data_CarChanged(object sender, EventArgs e)
        {
            LaunchRpm = Main.Drivetrain.MaximumRpm / 3 + 1000;
            LaunchRpm = Main.Drivetrain.StallRpm * 3;
            LaunchRpm = Main.Drivetrain.MaximumRpm - 500;
            LaunchRpm = 3000;
            if (LaunchRpm > Main.Drivetrain.MaximumRpm) LaunchRpm = Main.Drivetrain.StallRpm * 2.5;
            RevvingProp = LaunchRpm / 1000 - 2.25;
            PullingThrottleProp = LaunchRpm / 1000 - 1.75;
            if (RevvingProp < 1)
            {
                PullingThrottleProp++;
                RevvingProp = 1;
            }

            TemporaryLoadTc = true;
        }
    }
}