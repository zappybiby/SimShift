using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimShift.MapTool
{
    public class Ets2Node
    {
        public float X;

        public float Y;

        public float Z;

        public Ets2Node(byte[] stream, int position)
        {
            this.NodeUID = BitConverter.ToUInt64(stream, position);
            this.ForwardItemUID = BitConverter.ToUInt64(stream, position + 44);
            this.BackwardItemUID = BitConverter.ToUInt64(stream, position + 44 - 8);

            this.X = BitConverter.ToInt32(stream, position + 8) / 256.0f;
            this.Y = BitConverter.ToInt32(stream, position + 12) / 256.0f;
            this.Z = BitConverter.ToInt32(stream, position + 16) / 256.0f;

            var rX = BitConverter.ToSingle(stream, position + 20);
            var rY = BitConverter.ToSingle(stream, position + 24);
            var rZ = BitConverter.ToSingle(stream, position + 28);

            this.Yaw = (float) Math.PI - (float) Math.Atan2(rZ, rX);
            this.Yaw = this.Yaw % (float) Math.PI * 2;

            // X,Y,Z is position of NodeUID
            // ForwardItemUID = Forward item
            // BackwardItemUID = Backward item

            // Console.WriteLine(position.ToString("X4") + " | " + NodeUID.ToString("X16") + " " + ForwardItemUID.ToString("X16") + " " + BackwardItemUID.ToString("X16") + " @ " + string.Format("{0} {1} {2} {3} {4} {5} {6} {7}de", X, Y, Z, rX,rY,rZ,Yaw,Yaw/Math.PI*180));
            this.Yaw = (float) Math.PI * 0.5f - this.Yaw;
        }

        public Ets2Item BackwardItem { get; set; }

        public ulong BackwardItemUID { get; private set; }

        public Ets2Item ForwardItem { get; set; }

        public ulong ForwardItemUID { get; private set; }

        public ulong NodeUID { get; private set; }

        public float Pitch { get; private set; }

        public Ets2Point Point => new Ets2Point(this.X, this.Y, this.Z, this.Yaw);

        public float Roll { get; private set; }

        public float Yaw { get; private set; }

        public IEnumerable<Ets2Item> GetItems()
        {
            if (this.ForwardItem != null)
            {
                yield return this.ForwardItem;
            }

            if (this.BackwardItem != null)
            {
                yield return this.BackwardItem;
            }
        }

        public override string ToString()
        {
            return "Node #" + this.NodeUID.ToString("X16") + " (" + this.X + "," + this.Y + "," + this.Z + ")";
        }
    }
}