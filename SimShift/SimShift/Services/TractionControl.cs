using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;

using SimShift.Data.Common;
using SimShift.Entities;
using SimShift.Utils;

namespace SimShift.Services
{
    /// <summary>
    ///     Traction Control limits the wheel slip via open-loop control by reducing throttle when a slip % exceeds
    ///     predetermined offset.
    ///     The aggressiveness is adjusted by slope.
    ///     Optionally a rattling sound can be played to alert the driver TC is overriding control.
    /// </summary>
    public class TractionControl : IControlChainObj, IConfigurable
    {
        private DateTime lastPlay = DateTime.Now;

        private double lastThrottle = 0;

        private SoundPlayer tcSound;

        public TractionControl()
        {
            this.tcSound = new SoundPlayer(@"..\..\\Resources\tractioncontrol.wav");

            // setVolume(0);
            this.SoundStopped = true;
            this.lastThrottle = 1;

            // setVolume(1);
            var updateSound = new Timer { Enabled = true, Interval = 10 };
            updateSound.Elapsed += (sender, args) =>
                {
                    // if (Main.Transmission.IsShifting || Antistall.Stalling || !Slipping) setVolume(0);
                    // else setVolume(1 - lastThrottle);
                };
            updateSound.Start();
        }

        public IEnumerable<string> AcceptsConfigs => new[] { "TractionControl" };

        public bool Active => this.Slipping;

        public double AllowedSlip { get; private set; }

        public bool CanPauseTrack => DateTime.Now > this.lastPlay;

        public bool Enabled => this.AllowedSlip < 30;

        public double EngineSpeed { get; private set; }

        public string File { get; set; }

        public IEnumerable<string> SimulatorsBan => new string[0];

        public IEnumerable<string> SimulatorsOnly => new string[0];

        public double SlipAngle { get; private set; }

        public bool Slipping { get; private set; }

        public double Slope { get; private set; }

        public bool SoundStopped { get; private set; }

        public double WheelSpeed { get; private set; }

        [DllImport("winmm.dll")]
        public static extern int waveOutGetVolume(IntPtr hwo, out uint dwVolume);

        [DllImport("winmm.dll")]
        public static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

        public void ApplyParameter(IniValueObject obj)
        {
            switch (obj.Key)
            {
                case "Slope":
                    this.Slope = obj.ReadAsFloat();
                    break;

                case "Slip":
                    this.AllowedSlip = obj.ReadAsFloat();
                    break;
            }
        }

        public IEnumerable<IniValueObject> ExportParameters()
        {
            List<IniValueObject> obj = new List<IniValueObject>();

            obj.Add(new IniValueObject(this.AcceptsConfigs, "Slope", this.Slope.ToString()));
            obj.Add(new IniValueObject(this.AcceptsConfigs, "Slip", this.AllowedSlip.ToString()));

            return obj;
        }

        public double GetAxis(JoyControls c, double val)
        {
            switch (c)
            {
                default: return val;

                case JoyControls.Throttle:
                    var t = 1 - (this.SlipAngle - 1 - this.AllowedSlip) / this.Slope;
                    if (t > 1)
                    {
                        t = 1;
                    }

                    if (t < 0)
                    {
                        t = 0;
                    }

                    this.lastThrottle = t * 0.05 + this.lastThrottle * 0.95;
                    return val * this.lastThrottle;
                    break;
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
                default: return false;

                case JoyControls.Throttle: return this.Slipping;
            }
        }

        public void ResetParameters()
        {
            this.AllowedSlip = 5;
            this.Slope = 5;
        }

        public void TickControls()
        { }

        public void TickTelemetry(IDataMiner data)
        {
            try
            {
                this.WheelSpeed = Main.Drivetrain.CalculateSpeedForRpm(data.Telemetry.Gear - 1, data.Telemetry.EngineRpm);
                this.EngineSpeed = data.Telemetry.Speed; // the actual speed we are going
                if (double.IsInfinity(this.WheelSpeed))
                {
                    this.WheelSpeed = 0;
                }

                if (Antistall.Stalling)
                {
                    this.WheelSpeed = 0;
                }

                this.SlipAngle = this.WheelSpeed / this.EngineSpeed;
                this.Slipping = this.SlipAngle - this.AllowedSlip > 1.0;
                if (Main.Drivetrain.CalculateSpeedForRpm(data.Telemetry.Gear - 1, (float) Main.Drivetrain.StallRpm * 1.2f) >= data.Telemetry.Speed)
                {
                    this.Slipping = false;
                }
            }
            catch
            { }
        }

        // waveOutSetVolume(IntPtr.Zero, vol_out);
        // //vol_out = 0xFFFFFFFF;
        // uint vol_out = vol_hex | (vol_hex << 16);

        // uint vol_hex = (uint) (vol * 0x7FFF);
        // }
        // }
        // SoundStopped = false;
        // tcSound.PlayLooping();
        // {
        // if (SoundStopped)
        // lastPlay = DateTime.Now.Add(new TimeSpan(0, 0, 0, 1));
        // {
        // else
        // }
        // }
        // SoundStopped = true;
        // tcSound.Stop();
        // vol = 1;
        // {
        // if (CanPauseTrack)
        // if (SoundStopped) return;
        // {
        // if (vol == 0)
        // {

        // private void setVolume(double vol)
    }
}
//}