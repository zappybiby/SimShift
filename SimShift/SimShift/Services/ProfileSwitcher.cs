using System;
using System.Collections.Generic;

using SimShift.Data;
using SimShift.Data.Common;
using SimShift.Entities;

namespace SimShift.Services
{
    /// <summary>
    ///     This module monitors user inputs to switch driving profiles.
    ///     Driving profiles contain a set of settings for all different driving modules, e.g. transmission, speedlimiter,
    ///     traction control, etc.
    /// </summary>
    public class ProfileSwitcher : IControlChainObj
    {
        private bool TrailerAttached = false;

        public bool Active => this.ProfileSwitchFrozen;

        public bool Enabled => true;

        public bool ProfileSwitchFrozen => this.ProfileSwitchTimeout > DateTime.Now;

        public DateTime ProfileSwitchTimeout { get; private set; }

        public IEnumerable<string> SimulatorsBan => new string[0];

        public IEnumerable<string> SimulatorsOnly => new string[0];

        public bool TransmissionReverseFrozen => this.TransmissionReverseTimeout > DateTime.Now;

        public DateTime TransmissionReverseTimeout { get; private set; }

        public double GetAxis(JoyControls c, double val)
        {
            return val;
        }

        public bool GetButton(JoyControls c, bool val)
        {
            switch (c)
            {
                case JoyControls.GearUp:
                    if (val && !this.ProfileSwitchFrozen)
                    {
                        this.ProfileSwitchTimeout = DateTime.Now.Add(new TimeSpan(0, 0, 0, 1));
                        if (Main.Data.Active.Application == "eurotrucks2")
                        {
                            var ets2miner = (Ets2DataMiner) Main.Data.Active;
                            var ets2telemetry = ets2miner.MyTelemetry;
                            Main.LoadNextProfile(ets2telemetry.Job.Mass);
                        }
                        else
                        {
                            Main.LoadNextProfile(10000);
                        }
                    }

                    return false;
                    break;
                case JoyControls.GearDown:
                    if (val && !this.TransmissionReverseFrozen)
                    {
                        this.TransmissionReverseTimeout = DateTime.Now.Add(new TimeSpan(0, 0, 0, 1));
                        Transmission.InReverse = !Transmission.InReverse;
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
                case JoyControls.GearUp:
                case JoyControls.GearDown: return true;

                default: return false;
            }
        }

        public void TickControls()
        { }

        public void TickTelemetry(IDataMiner data)
        {
            if (data.Application == "eurotrucks2")
            {
                var ets2miner = (Ets2DataMiner) data;
                var ets2telemetry = ets2miner.MyTelemetry;
                var trailerAttached = ets2telemetry.Job.TrailerAttached;
                if (trailerAttached != this.TrailerAttached)
                {
                    this.TrailerAttached = trailerAttached;
                    Main.ReloadProfile(trailerAttached ? ets2telemetry.Job.Mass : 0);
                }
            }
        }
    }
}