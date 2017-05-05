using System.Collections.Generic;

namespace SimShift.MapTool
{
    public class Ets2PrefabCurve
    {
        public float EndRotationX;

        public float EndRotationY;

        public float EndRotationZ;

        public float EndX;

        public float EndY;

        public float EndZ;

        public float Length;

        public int[] Next;

        public int[] Prev;

        public float StartRotationX;

        public float StartRotationY;

        public float StartRotationZ;

        public float StartX;

        public float StartY;

        public float StartZ;

        public double EndYaw { get; set; }

        public int Index { get; set; }

        public List<Ets2PrefabCurve> NextCurve { get; set; }

        public List<Ets2PrefabCurve> PrevCurve { get; set; }

        public double StartYaw { get; set; }
    }
}