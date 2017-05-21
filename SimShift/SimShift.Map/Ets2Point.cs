using System;
using System.Drawing;

namespace SimShift.MapTool
{
    public class Ets2Point
    {
        public float Heading;

        public float X;

        public float Y;

        public float Z;

        public Ets2Point(float x, float y, float z, float heading)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.Heading = heading;
        }

        public Ets2Point(PointF fr)
        {
            this.X = fr.X;
            this.Y = 0;
            this.Z = fr.Y;
            this.Heading = 0;
        }

        public bool CloseTo(Ets2Point pt)
        {
            return this.DistanceTo(pt) <= 2f;
        }

        public float DistanceTo(Ets2Point pt)
        {
            if (pt == null)
            {
                return float.MaxValue;
            }

            var dx = pt.X - this.X;
            var dy = pt.Z - this.Z;

            var dst = (float) Math.Sqrt(dx * dx + dy * dy);

            return dst;
        }

        public PointF ToPoint()
        {
            return new PointF(this.X, this.Z);
        }

        public override string ToString()
        {
            return "P " + Math.Round(this.X, 2) + "," + Math.Round(this.Z, 2) + " / " + Math.Round(this.Heading / Math.PI * 180, 1) + "deg (" + Math.Round(this.Heading, 3) + ")";
        }
    }
}