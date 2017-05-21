using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

using SimShift.Entities;
using SimShift.Models;
using SimShift.Services;

namespace SimShift.Dialogs
{
    public partial class dlGearboxShifterTable : Form
    {
        private ShifterTableConfiguration activeConfiguration = new ShifterTableConfiguration(ShifterTableConfigurationDefault.PowerEfficiency, new Ets2Drivetrain(), 1, 0);

        private int dataGridOverheadB = 0;

        private int dataGridOverheadR = 0;

        private int simGraphOverheadB = 0;

        public dlGearboxShifterTable()
        {
            var myEngine = new Ets2Drivetrain();
            Main.Load(myEngine, "Settings/Drivetrain/eurotrucks2.iveco.hiway.ini");
            this.activeConfiguration = Main.Running ? Main.Transmission.configuration : new ShifterTableConfiguration(ShifterTableConfigurationDefault.Efficiency, myEngine, 19, 25000);

            string headline = "RPM";
            for (int k = 0; k <= 10; k++)
            {
                headline = headline + ",Ratio " + k;
            }

            // ",Fuel " + k + ",Power " + k +
            headline = headline + "\r\n";

            List<string> fuelStats = new List<string>();
            for (float rpm = 0; rpm < 2500; rpm += 100)
            {
                string l = rpm + string.Empty;
                for (int load = 0; load <= 10; load++)
                {
                    float throttle = load / 20.0f;
                    var fuelConsumption = this.activeConfiguration.Drivetrain.CalculateFuelConsumption(rpm, throttle);
                    var power = this.activeConfiguration.Drivetrain.CalculatePower(rpm, throttle);
                    var fuel2 = (power / fuelConsumption) / rpm;

                    // "," + fuelConsumption + "," + power + 
                    l = l + "," + fuel2;
                }

                fuelStats.Add(l);
            }

            File.WriteAllText("./fuelstats.csv", headline + string.Join("\r\n", fuelStats));

            // sim
            this.sim = new ucGearboxShifterGraph(this.activeConfiguration);
            this.sim.Location = new Point(12, 283);
            this.sim.Name = "sim";
            this.sim.Size = new Size(854, 224);
            this.sim.TabIndex = 2;
            this.Controls.Add(this.sim);

            this.SizeChanged += new EventHandler(this.dlGearboxShifterTable_SizeChanged);

            this.InitializeComponent();
            this.LoadTable();

            this.shifterTable.SelectionChanged += new EventHandler(this.shifterTable_SelectionChanged);
            this.dataGridOverheadR = this.Width - this.shifterTable.Width;
            this.dataGridOverheadB = this.sim.Location.Y - this.shifterTable.Location.Y - this.shifterTable.Height;
            this.simGraphOverheadB = this.Height - this.sim.Height;
        }

        // Given H,S,L in range of 0-1
        // Returns a Color (RGB struct) in range of 0-255
        public static Color HSL2RGB(double h, double sl, double l)
        {
            h = 1 - h;
            double v;
            double r, g, b;

            r = l; // default to gray
            g = l;
            b = l;
            v = (l <= 0.5) ? (l * (1.0 + sl)) : (l + sl - l * sl);
            if (v > 0)
            {
                double m;
                double sv;
                int sextant;
                double fract, vsf, mid1, mid2;

                m = l + l - v;
                sv = (v - m) / v;
                h *= 4.0;
                sextant = (int) h;
                fract = h - sextant;
                vsf = v * sv * fract;
                mid1 = m + vsf;
                mid2 = v - vsf;
                switch (sextant)
                {
                    case 0:
                        r = v;
                        g = mid1;
                        b = m;
                        break;
                    case 1:
                        r = mid2;
                        g = v;
                        b = m;
                        break;
                    case 2:
                        r = m;
                        g = v;
                        b = mid1;
                        break;
                    case 3:
                        r = m;
                        g = mid2;
                        b = v;
                        break;
                    case 4:
                        r = mid1;
                        g = m;
                        b = v;
                        break;
                    case 5:
                        r = v;
                        g = m;
                        b = mid2;
                        break;
                }
            }

            Color rgb = Color.FromArgb(Convert.ToByte(r * 255.0f), Convert.ToByte(g * 255.0f), Convert.ToByte(b * 255.0f));
            return rgb;
        }

        void dlGearboxShifterTable_SizeChanged(object sender, EventArgs e)
        {
            this.sim.Location = new Point(this.sim.Location.X, this.Height - this.simGraphOverheadB);
            this.sim.Size = new Size(this.Width - this.dataGridOverheadR, this.sim.Height);
            this.shifterTable.Size = new Size(this.Width - this.dataGridOverheadR, this.sim.Location.Y - this.shifterTable.Location.Y - this.dataGridOverheadB);
        }

        private void LoadTable()
        {
            this.shifterTable.Rows.Clear();
            this.shifterTable.Columns.Clear();

            this.shifterTable.Columns.Add("Load", "Load");

            List<Color> gearColors = new List<Color>();
            gearColors.Add(Color.White);
            for (int gear = 0; gear < this.activeConfiguration.Drivetrain.Gears; gear++)
            {
                gearColors.Add(HSL2RGB(gear / 1.0 / this.activeConfiguration.Drivetrain.Gears, 0.5, 0.5));
            }

            var spdBins = 0;
            var spdBinsData = new List<double>();
            foreach (var spd in this.activeConfiguration.tableThrottle.Keys)
            {
                if (spd % 3 == 0)
                {
                    this.shifterTable.Columns.Add(spd.ToString(), spd.ToString());

                    spdBinsData.Add(spd);
                    spdBins++;
                }
            }

            foreach (var load in this.activeConfiguration.tableThrottle[0].Keys)
            {
                var data = new object[spdBins + 1];
                data[0] = Math.Round(load * 100).ToString();
                for (int i = 0; i < spdBins; i++)
                {
                    data[i + 1] = this.activeConfiguration.Lookup(spdBinsData[i], load).Gear;
                }

                this.shifterTable.Rows.Add(data);
            }

            for (int col = 0; col < this.shifterTable.Columns.Count; col++)
            {
                this.shifterTable.Columns[col].Width = 33;
            }

            for (int row = 0; row < this.shifterTable.Rows.Count; row++)
            {
                for (int spd = 1; spd <= spdBins; spd++)
                {
                    if (this.shifterTable.Rows[row].Cells[spd].Value != null)
                    {
                        this.shifterTable.Rows[row].Cells[spd].Style.BackColor = gearColors[int.Parse(this.shifterTable.Rows[row].Cells[spd].Value.ToString())];
                    }
                }
            }
        }

        void shifterTable_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                // Get which rows are selected.
                int sel = this.shifterTable.SelectedCells.Count;
                List<double> loads = new List<double>();
                for (int i = 0; i < sel; i++)
                {
                    loads.Add(double.Parse(this.shifterTable.SelectedCells[i].Value.ToString()));
                }

                this.sim.Update(loads);
            }
            catch (Exception aad)
            { }
        }
    }
}