using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SimShift.Dialogs
{
    public partial class ucPlotter : UserControl
    {
        public List<List<double>> values = new List<List<double>>();

        private int channels = 0;

        private Pen gridPen = new Pen(Color.DarkSeaGreen, 1.0f);

        private int gridsHorizontal = 20;

        private int gridsVertical = 20;

        private Pen[] pens = new[] { new Pen(Color.Yellow, 1.0f), new Pen(Color.Red, 1.0f), new Pen(Color.DeepSkyBlue, 1.0f), new Pen(Color.GreenYellow, 1.0f), new Pen(Color.Magenta, 3.0f) };

        private float samplesPerDiv = 10;

        private List<float> valueScale = new List<float>();

        public ucPlotter(int ch, float[] scale)
        {
            this.channels = ch;
            for (int k = 0; k < ch; k++)
            {
                this.values.Add(new List<double>());
                this.valueScale.Add(scale[k] / this.gridsVertical * 2);
            }

            var emptyList = new List<double>();
            for (int k = 0; k < ch; k++)
            {
                emptyList.Add(0);
            }

            for (int i = 0; i < 1000; i++)
            {
                this.Add(emptyList);
            }

            this.InitializeComponent();
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        }

        public int Frequency { get; set; }

        public void Add(List<double> v)
        {
            for (int k = 0; k < this.channels; k++)
            {
                this.values[k].Add(v[k]);
                while (this.values[k].Count > this.samplesPerDiv * this.gridsHorizontal)
                {
                    this.values[k].RemoveAt(0);
                }
            }

            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            this.samplesPerDiv = 100;
            base.OnPaint(e);

            var g = e.Graphics;

            g.FillRectangle(Brushes.Black, e.ClipRectangle);
            g.DrawString(this.Frequency.ToString("000"), new Font("Arial", 8), Brushes.White, 0, 0);
            var w = e.ClipRectangle.Width;
            var h = e.ClipRectangle.Height;

            var pxHor = w / this.gridsHorizontal;
            var pxVer = h / this.gridsVertical;

            for (int i = 0; i <= this.gridsHorizontal; i++)
            {
                g.DrawLine(this.gridPen, pxHor * i, 0, pxHor * i, h);
            }

            for (int i = 0; i <= this.gridsVertical; i++)
            {
                g.DrawLine(this.gridPen, 0, pxVer * i, w, pxVer * i);
            }

            // Display all values
            for (int chIndex = 0; chIndex < this.values.Count; chIndex++)
            {
                try
                {
                    if (this.pens.Length >= chIndex && this.values.Count >= chIndex && this.valueScale.Count >= chIndex)
                    { }
                    else
                    {
                        break;
                    }

                    var chPen = this.pens[chIndex];
                    var ch = this.values[chIndex];
                    var scale = this.valueScale[chIndex];

                    var lastX = 0.0f;
                    var lastY = (float) h / 2.0f;

                    for (int sampleIndex = 0; sampleIndex < ch.Count; sampleIndex++)
                    {
                        var v = ch[sampleIndex];
                        var curX = (float) (sampleIndex * w / ch.Count);
                        var curY = (float) (this.gridsVertical / 2 - v / scale) * pxVer;

                        g.DrawLine(chPen, lastX, lastY, curX, curY);

                        lastX = curX;
                        lastY = curY;
                    }
                }
                catch (Exception ex)
                { }
            }
        }
    }
}