﻿using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using SimShift.Data;
using SimShift.Services;

namespace SimShift.Dialogs
{
    public partial class dlDebugInfo : Form
    {
        private Ets2DataMiner data;

        private bool fitToSize = true;

        private Brush meColor = Brushes.White;

        private int targetH = 500;

        private int targetW = 500;

        public dlDebugInfo()
        {
            this.InitializeComponent();

            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);

            if (Main.Data.Active != null && Main.Data.Active.Application == "eurotrucks2")
            {
                this.data = (Ets2DataMiner) Main.Data.Active;
            }

            Main.Data.AppActive += (sender, args) =>
                {
                    if (Main.Data.Active != null && Main.Data.Active.Application == "eurotrucks2")
                    {
                        this.data = (Ets2DataMiner) Main.Data.Active;
                    }
                };

            var updateGfx = new Timer { Interval = 40 };
            updateGfx.Tick += (sender, args) => this.Invalidate();
            updateGfx.Start();

            this.FormClosing += (sender, args) => updateGfx.Stop();
        }

        public static Ets2Car TrackedCar { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (this.fitToSize)
            {
                // Measure size (this is with border)
                var w = e.ClipRectangle.Width;
                var h = e.ClipRectangle.Height;

                var dw = this.targetW - w;
                var dh = this.targetH - h;

                this.Size = new Size(this.Width + dw, this.Height + dh);
                this.fitToSize = false;
            }

            var g = e.Graphics;
            g.FillRectangle(Brushes.Black, e.ClipRectangle);

            if (this.data == null)
            {
                g.DrawString("NO ETS2 DATA MINER ACTIVE", new Font("Arial", 24.0f), Brushes.White, 5, 5);
                return;
            }

            // Render map
            float scale = 150; // p-p
            dlMap.RenderMap(e.ClipRectangle, g, true, ref scale);

            // ME:
            g.DrawEllipse(new Pen(this.meColor, 5.0f), this.targetW / 2 - 3, this.targetH / 2 - 3, 6, 6);

            var meHeading = Math.PI - this.data.MyTelemetry.Physics.RotationX * -2 * Math.PI;
            var meHeadingRadius = 25;
            g.DrawLine(new Pen(this.meColor, 3.0f), this.targetW / 2, this.targetH / 2, this.targetW / 2 + (float) Math.Sin(meHeading) * meHeadingRadius, this.targetH / 2 + (float) Math.Cos(meHeading) * meHeadingRadius);

            var centerX = this.data.MyTelemetry.Physics.CoordinateX;
            var centerY = this.data.MyTelemetry.Physics.CoordinateY;
            var centerZ = this.data.MyTelemetry.Physics.CoordinateZ;

            var steeringAngle = 35.0 / 180 * Math.PI * this.data.MyTelemetry.Controls.GameSteer;
            var wheelBase = 4;
            var saTan = (float) Math.Tan(Math.Abs(steeringAngle));
            var steerRadius = wheelBase / saTan;
            var steerCircumfere = steerRadius * 2 * Math.PI;
            var steerRadiusSc = steerRadius / scale * this.targetW;
            var offx = Math.Sin(meHeading);

            if (false && float.IsNaN(steerRadiusSc) == false && float.IsInfinity(steerRadiusSc) == false && Math.Abs(steerRadius) < 30000)
            {
                if (steeringAngle < 0)
                {
                    // left
                    var corrAngle = meHeading + Math.PI / 2;
                    g.DrawArc(new Pen(Brushes.Aqua, 2.0f), this.targetW / 2 - steerRadiusSc / 2 - (float) Math.Sin(corrAngle) * steerRadiusSc / 2, this.targetH / 2 - steerRadiusSc / 2 - (float) Math.Cos(corrAngle) * steerRadiusSc / 2, steerRadiusSc, steerRadiusSc, (float) (meHeading / Math.PI * 180 + 90), 360);
                }
                else
                {
                    var corrAngle = meHeading + Math.PI / 2;
                    g.DrawArc(new Pen(Brushes.Aqua, 2.0f), this.targetW / 2 - steerRadiusSc / 2 + (float) Math.Sin(corrAngle) * steerRadiusSc / 2, this.targetH / 2 - steerRadiusSc / 2 + (float) Math.Cos(corrAngle) * steerRadiusSc / 2, steerRadiusSc, steerRadiusSc, (float) (meHeading / Math.PI * 180 + 90), 360);
                }

                /*g.DrawEllipse(new Pen(Brushes.Turquoise, 2.0f), 
                                    targetW / 2 - (float)Math.Sin(meHeading) * steerRadiusSc,
                                    targetH / 2 - (float)Math.Cos(meHeading) * steerRadiusSc,
                                    steerRadiusSc, steerRadiusSc);*/
            }

            // Track this vehicle along curve:
            var mX = this.data.MyTelemetry.Physics.CoordinateX;
            var mY = this.data.MyTelemetry.Physics.CoordinateZ;

            var px = 0.0f;
            var py = 0.0f;

            var step = 0.05f;
            var heading = meHeading + Math.PI / 2;

            foreach (var car in this.data.Cars)
            {
                car.Tracked = false;
                car.Tick();
            }

            var pwr = Main.Drivetrain.CalculatePower(this.data.MyTelemetry.Drivetrain.EngineRpm, this.data.MyTelemetry.Controls.GameThrottle);
            var scanDistance = 2.0f + Math.Pow(1 - Math.Abs(this.data.MyTelemetry.Controls.GameSteer), 64) * 3.5f;
            for (float ts = 0.0f; ts < scanDistance && !this.data.Cars.Any(x => x.Tracked); ts += step)
            {
                // Interpolate the steer radius
                var ds = this.data.MyTelemetry.Drivetrain.Speed * ts;
                var spd = Math.Max(10, this.data.MyTelemetry.Drivetrain.Speed); // always scan at minimum of 36kmh
                var da = spd / steerCircumfere * 2 * Math.PI * step;

                var dix = Math.Sin(heading) - Math.Sin(heading + da);
                var diy = Math.Cos(heading) - Math.Cos(heading + da);

                if (steeringAngle < 0)
                {
                    heading -= da;
                }
                else
                {
                    heading += da;
                }

                px += (float) dix * steerRadius / 2;
                py += (float) diy * steerRadius / 2;

                // Rotated polygon
                var carL = 12.0f;
                var carW = 2.5f;
                PointF[] poly = new[] { new PointF(mX + px + carL / 2 * (float) Math.Cos(hg) - carW / 2 * (float) Math.Sin(hg), mY + py + carL / 2 * (float) Math.Sin(hg) + carW / 2 * (float) Math.Cos(hg)), new PointF(mX + px - carL / 2 * (float) Math.Cos(hg) - carW / 2 * (float) Math.Sin(hg), mY + py - carL / 2 * (float) Math.Sin(hg) + carW / 2 * (float) Math.Cos(hg)), new PointF(mX + px - carL / 2 * (float) Math.Cos(hg) + carW / 2 * (float) Math.Sin(hg), mY + py - carL / 2 * (float) Math.Sin(hg) - carW / 2 * (float) Math.Cos(hg)), new PointF(mX + px + carL / 2 * (float) Math.Cos(hg) + carW / 2 * (float) Math.Sin(hg), mY + py + carL / 2 * (float) Math.Sin(hg) - carW / 2 * (float) Math.Cos(hg)), };
                foreach (var car in this.data.Cars)
                {
                    if (car.Valid && this.IsPolygonsIntersecting(car.Box, poly))
                    {
                        car.Tracked = true;
                        break;
                    }
                }

                var drx = this.targetW / 2 + px / scale * this.targetW;
                var dry = this.targetH / 2 + py / scale * this.targetH;

                var polyToDraw = poly.Select(x => new PointF(this.targetW / 2 + (x.X - mX) / 2 / scale * this.targetW, this.targetH / 2 + (x.Y - mY) / 2 / scale * this.targetH)).ToArray();
                g.FillPolygon(Brushes.Tomato, polyToDraw);

                // g.DrawLine(new Pen(Color.DarkSalmon, 5.0f), drx, dry, drx + 1, dry);
            }

            g.FillRectangle(new SolidBrush(Color.FromArgb(25, 25, 25)), 0, 0, this.targetW, 32);
            var trafficColor = Brushes.BlueViolet;
            foreach (var car in this.data.Cars)
            {
                if (!car.Valid)
                {
                    continue;
                }

                var x = this.targetW / 2 + (car.X - centerX) / 2 / scale * this.targetW;
                var y = this.targetH / 2 + (car.Z - centerZ) / 2 / scale * this.targetH;

                var sz = 10 * scale;
                if (sz > 10)
                {
                    sz = 10;
                }

                var of = sz / 2;

                var dx = car.X - centerX;
                var dy = car.Z - centerZ;
                var dv = this.data.MyTelemetry.Drivetrain.Speed - car.Speed; // m/s
                var distance = (float) Math.Sqrt(dx * dx + dy * dy) - 12;
                if (distance < 0.1f)
                {
                    distance = 0.1f;
                }

                var tti = dv < 0 ? -1.0f : distance / dv;

                var tc = trafficColor;
                if (car.Tracked)
                {
                    g.DrawString("Track #" + car.ID + " SPD " + Math.Round(car.Speed * 3.6) + "km/h (d=" + Math.Round(car.Speed * 3.6 - this.data.MyTelemetry.Drivetrain.SpeedKmh, 1) + "); distance " + Math.Round(distance, 1) + "m TTI " + ((tti == -1.0f) ? "never" : Math.Round(tti, 2) + "s"), new Font("Arial", 11.0f, FontStyle.Bold), Brushes.White, 5, 5);
                    car.Distance = distance;
                    car.TTI = tti;
                    TrackedCar = car;

                    tc = Brushes.Turquoise;
                    if (tti < 10 && tti > 0)
                    {
                        tc = Brushes.CadetBlue;
                    }

                    if (tti < 5 && tti > 0)
                    {
                        tc = Brushes.DarkSalmon;
                    }

                    if (tti < 2 && tti > 0)
                    {
                        tc = Brushes.HotPink;
                    }
                }

                if (car.Box == null)
                {
                    continue;
                }

                var polyToDraw = car.Box.Select(d => new PointF(this.targetW / 2 + (d.X - centerX) / 2 / scale * this.targetW, this.targetH / 2 + (d.Y - centerZ) / 2 / scale * this.targetH)).ToArray();
                g.FillPolygon(tc, polyToDraw);

                // g.DrawEllipse(new Pen(tc, sz), x - of,  y - of, sz, sz);

                /*if (tti == -1337.0f)
                    g.DrawString(":-)", new Font("Arial", 7.0f), Brushes.White, x-8,y-4);
                else
                    g.DrawString(Math.Round(tti,1).ToString(), new Font("Arial", 7.0f), Brushes.White, x-8,y-4);*/
            }

            g.FillRectangle(new SolidBrush(Color.FromArgb(25, 25, 25)), 0, this.targetH - 32, this.targetW, this.targetH);
            g.DrawString("Looking " + Math.Round(scanDistance, 2) + "s ahead SPD " + Math.Round(this.data.MyTelemetry.Drivetrain.SpeedKmh) + "kmh PWR " + Math.Round(pwr) + "hp THR " + Math.Round(this.data.MyTelemetry.Controls.GameThrottle * 100, 1) + "% BKR " + Math.Round(this.data.MyTelemetry.Controls.GameBrake * 100, 2) + "%", new Font("Arial", 11.0f, FontStyle.Bold), Brushes.White, 2, this.targetH - 25);
            if (this.data.Cars.Any(x => x.Tracked) == false)
            {
                g.DrawString("No car being tracked (followed)", new Font("Arial", 12.0f, FontStyle.Bold), Brushes.White, 5, 5);
                TrackedCar = null;
            }
        }

        private bool IsPolygonsIntersecting(PointF[] a, PointF[] b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            foreach (var polygon in new[] { a, b })
            {
                for (int i1 = 0; i1 < polygon.Length; i1++)
                {
                    int i2 = (i1 + 1) % polygon.Length;
                    var p1 = polygon[i1];
                    var p2 = polygon[i2];

                    var normal = new PointF(p2.Y - p1.Y, p1.X - p2.X);

                    double? minA = null, maxA = null;
                    foreach (var p in a)
                    {
                        var projected = normal.X * p.X + normal.Y * p.Y;
                        if (minA == null || projected < minA)
                        {
                            minA = projected;
                        }

                        if (maxA == null || projected > maxA)
                        {
                            maxA = projected;
                        }
                    }

                    double? minB = null, maxB = null;
                    foreach (var p in b)
                    {
                        var projected = normal.X * p.X + normal.Y * p.Y;
                        if (minB == null || projected < minB)
                        {
                            minB = projected;
                        }

                        if (maxB == null || projected > maxB)
                        {
                            maxB = projected;
                        }
                    }

                    if (maxA < minB || maxB < minA)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}