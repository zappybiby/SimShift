using System;
using System.Diagnostics;
using System.Timers;

using SimShift.Data.Common;

namespace SimShift.Data
{
    public class Tdu2DataMiner : IDataMiner
    {
        private MemoryReader _tdu2Reader;

        private Timer _updateTel;

        // Enable write operations?
        bool openedTduAsWriter;

        public Tdu2DataMiner()
        {
            this._updateTel = new Timer();
            this._updateTel.Interval = 25;
            this._updateTel.Elapsed += this._updateTel_Elapsed;

            this.Telemetry = default(GenericDataDefinition);
        }

        public Process ActiveProcess { get; set; }

        public string Application => "TestDrive2";

        public EventHandler DataReceived { get; set; }

        public bool EnableWeirdAntistall => false;

        public bool IsActive { get; set; }

        public string Name => "Test Drive Unlimited 2";

        public bool RunEvent { get; set; }

        public bool Running { get; set; }

        public bool SelectManually => false;

        public bool SupportsCar => true;

        public IDataDefinition Telemetry { get; private set; }

        public bool TransmissionSupportsRanges => false;

        public double Weight => 1500;

        public void EvtStart()
        {
            this.Telemetry = default(GenericDataDefinition);

            this._tdu2Reader = new MemoryReader();
            this._tdu2Reader.ReadProcess = this.ActiveProcess;
            this._tdu2Reader.Open();
            this.openedTduAsWriter = false;

            this._updateTel.Start();
        }

        public void EvtStop()
        {
            this._tdu2Reader.Close();
            this._tdu2Reader = null;

            this._updateTel.Stop();

            this.Telemetry = default(GenericDataDefinition);
        }

        public void Write<T>(TelemetryChannel channel, T i)
        {
            if (this.ActiveProcess == null)
            {
                return;
            }

            if (!this.openedTduAsWriter)
            {
                this._tdu2Reader.Close();
                this._tdu2Reader = new MemoryWriter();
                this._tdu2Reader.ReadProcess = this.ActiveProcess;
                this._tdu2Reader.Open();

                this.openedTduAsWriter = true;
            }

            var channelAddress = this.GetWriteAddress(channel);

            var writer = this._tdu2Reader as MemoryWriter;
            if (i is float)
            {
                writer.WriteFloat(channelAddress, float.Parse(i.ToString()));
            }
        }

        void _updateTel_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (this._tdu2Reader == null || this._updateTel.Enabled == false)
            {
                return;
            }

            try
            {
                var b = this.ActiveProcess.MainModule.BaseAddress;

                var car = this._tdu2Reader.ReadString(b + 0xC2DC30, 32);
                var gear = this._tdu2Reader.ReadInt32(b + 0xC2DAD0) - 1;
                var gears = 7;
                var speed = this._tdu2Reader.ReadFloat(b + 0xC2DB24) / 3.6f;
                var throttle = this._tdu2Reader.ReadFloat(b + 0xC2DB00);
                var brake = this._tdu2Reader.ReadFloat(b + 0xC2DB04);
                var time = (float) (DateTime.Now.Subtract(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0)).TotalMilliseconds / 1000.0);
                var paused = false;
                var engineRpm = this._tdu2Reader.ReadFloat(b + 0xC2DB18);
                var fuel = 0;

                this.Telemetry = new GenericDataDefinition(car, time, paused, gear, gears, engineRpm, fuel, throttle, brake, speed);

                if (this.DataReceived != null)
                {
                    this.DataReceived(this, new EventArgs());
                }
            }
            catch
            {
                Debug.WriteLine("Data abort error");
            }
        }

        private IntPtr GetWriteAddress(TelemetryChannel channel)
        {
            switch (channel)
            {
                case TelemetryChannel.CameraHorizon: return this.GetWriteAddress(TelemetryChannel.CameraViewBase) + 0x550;

                case TelemetryChannel.CameraViewBase: return (IntPtr) this._tdu2Reader.ReadInt32(this.ActiveProcess.MainModule.BaseAddress + 0xD95BF0);

                default: return this.ActiveProcess.MainModule.BaseAddress;
            }
        }
    }
}