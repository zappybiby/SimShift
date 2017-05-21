using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using SimShift.Data.Common;

namespace SimShift.Data
{
    public class WorldMapper
    {
        public List<WorldMapCell> cells;

        private Ets2DataMiner source;

        public WorldMapper(IDataMiner dataSource)
        {
            if (dataSource.Application == "eurotrucks2" && !dataSource.SelectManually)
            {
                this.source = dataSource as Ets2DataMiner;
                this.source.DataReceived += this.OnDataReceived;

                this.cells = new List<WorldMapCell>();

                this.Import();
            }
        }

        public void Export()
        {
            if (this.cells == null)
            {
                return;
            }

            StringBuilder export = new StringBuilder();

            foreach (var c in this.cells)
            {
                export.AppendLine(string.Format("[Cell_{0}_{1}_]", c.X, c.Z));
                foreach (var p in c.points)
                {
                    export.AppendLine(string.Format("{0},{1},{2}", p.x, p.y, p.z));
                }
            }

            File.WriteAllText("map.ini", export.ToString());
        }

        public void Import()
        {
            string[] l = File.ReadAllLines("map.ini");

            WorldMapCell active = default(WorldMapCell);
            foreach (var li in l)
            {
                if (li.Substring(0, 5) == "[Cell")
                {
                    string[] cellData = li.Split("_".ToCharArray());
                    active = new WorldMapCell(int.Parse(cellData[1]), int.Parse(cellData[2]));
                    lock (this.cells)
                    {
                        this.cells.Add(active);
                    }
                }
                else
                {
                    string[] pointData = li.Split(",".ToCharArray());
                    lock (active.points)
                    {
                        active.points.Add(new WorldMapPoint(float.Parse(pointData[0]), float.Parse(pointData[1]), float.Parse(pointData[2])));
                    }
                }
            }
        }

        public WorldMapCell LookupCell(float x, float z)
        {
            WorldMapCell c = default(WorldMapCell);
            var cellX = (int) (x / 512);
            var cellZ = (int) (z / 512);
            lock (this.cells)
            {
                if (this.cells.Any(d => d.X == cellX && d.Z == cellZ))
                {
                    return this.cells.FirstOrDefault(d => d.X == cellX && d.Z == cellZ);
                }

                Debug.WriteLine("Created cell " + cellX + "," + cellZ);
                c = new WorldMapCell(cellX, cellZ);
                this.cells.Add(c);
            }

            return c;
        }

        private void OnDataReceived(object sender, EventArgs args)
        {
            var x = this.source.MyTelemetry.Physics.CoordinateX;
            var y = this.source.MyTelemetry.Physics.CoordinateY;
            var z = this.source.MyTelemetry.Physics.CoordinateZ;

            var activeCell = this.LookupCell(x, z);

            if (activeCell == null)
            {
                return;
            }

            lock (activeCell.points)
            {
                if (activeCell.points.Any(d => Math.Abs(d.x - x) < 1 || Math.Abs(d.z - z) < 1))
                {
                    return;
                }

                activeCell.points.Add(new WorldMapPoint(x, y, z));
            }
        }
    }

    public class WorldMapCell
    {
        public List<WorldMapPoint> points;

        public WorldMapCell(int cellX, int cellZ)
        {
            this.points = new List<WorldMapPoint>();

            this.X = cellX;
            this.Z = cellZ;
        }

        public int X { get; private set; }

        public int Z { get; private set; }
    }

    public class WorldMapPoint
    {
        public float x;

        public float y;

        public float z;

        public WorldMapPoint(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
}