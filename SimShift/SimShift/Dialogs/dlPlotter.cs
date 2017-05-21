using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SimShift.Data;
using SimShift.Entities;
using SimShift.Services;

namespace SimShift.Dialogs
{
    public partial class dlPlotter : Form
    {
        private double acc;

        private double dj;

        private int hz = 0;

        private ucPlotter plot;

        private double prevAcc;

        private double prevAccT;

        private double prevSpeed;

        private double prevTime;

        private Timer updater;

        public dlPlotter()
        {
            this.InitializeComponent();

            this.plot = new ucPlotter(
                5,
                new float[]
                    {
                        // 1,
                        // 1,
                        1, 1, 2500, 150, 5
                    });
            this.plot.Dock = DockStyle.Fill;
            this.Controls.Add(this.plot);

            Main.Data.DataReceived += this.Data_DataReceived;
            var tmr = new Timer { Enabled = true, Interval = 1000 };
            tmr.Tick += this.tmr_Tick;
            tmr.Start();
        }

        private void Data_DataReceived(object sender, EventArgs e)
        {
            var miner = Main.Data.Active as Ets2DataMiner;
            var tel = Main.Data.Telemetry; // miner.MyTelemetry;
            var dt = tel.Time - this.prevTime;
            var dv = tel.Speed - this.prevSpeed;

            var dt2 = tel.Time - this.prevAccT;
            if (dt2 > 0.05)
            {
                var acc = dv / dt;
                var da = acc - this.prevAcc;
                this.dj = Math.Abs(da) >= 0.001f ? da / dt2 / 10.0f : 0;
                this.prevAcc = acc;
                this.prevAccT = tel.Time;
            }

            if (dt > 0.0001)
            {
                this.hz++;
                var data = new[] { Main.GetAxisOut(JoyControls.Throttle), Main.GetAxisOut(JoyControls.Clutch), tel.EngineRpm - 2500, tel.Speed * 3.6, this.prevAcc };

                this.plot.Add(data.ToList());
            }

            this.prevSpeed = tel.Speed;
            this.prevTime = tel.Time;
        }

        private void tmr_Tick(object sender, EventArgs e)
        {
            this.plot.Frequency = this.hz;
            this.hz = 0;
        }
    }
}