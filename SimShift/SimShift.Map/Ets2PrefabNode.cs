using System.Collections.Generic;

namespace SimShift.MapTool
{
    public class Ets2PrefabNode
    {
        public List<Ets2PrefabCurve> InputCurve;

        public List<Ets2PrefabCurve> OutputCurve;

        public float RotationX;

        public float RotationY;

        public float RotationZ;

        public float X;

        public float Y;

        public float Z;

        public int Node { get; set; }

        public double Yaw { get; set; }
    }
}