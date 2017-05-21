using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace SimShift.MapTool
{
    public class Ets2Item
    {
        public int Origin = 0;

        public Ets2Item(ulong uid, Ets2Sector sector, int offset)
        {
            this.ItemUID = uid;

            this.Navigation = new Dictionary<Ets2Item, Tuple<float, float, IEnumerable<Ets2Item>>>();

            this.Sector = sector;
            this.FileOffset = offset;
            this.FilePath = sector.FilePath;

            this.NodesList = new Dictionary<ulong, Ets2Node>();

            this.Type = (Ets2ItemType) BitConverter.ToUInt32(sector.Stream, offset);

            int nodeCount;

            switch (this.Type)
            {
                case Ets2ItemType.Road:
                    this.StartNodeUID = BitConverter.ToUInt64(sector.Stream, offset + 141);
                    this.EndNodeUID = BitConverter.ToUInt64(sector.Stream, offset + 149);

                    var lookId = BitConverter.ToUInt32(sector.Stream, offset + 61); // unique UINT32 ID with road look
                    this.RoadLook = this.Sector.Mapper.LookupRoadLookID(lookId);

                    // Need to create LUT to translate road_look.sii <> ID
                    // Then we can parse highway routes etc.
                    this.HideUI = (sector.Stream[offset + 0x37] & 0x02) != 0;

                    // Make sure these UID's exist in the world.
                    if ((this.StartNodeUID != 0 && sector.Mapper.Nodes.ContainsKey(this.StartNodeUID)) || (this.EndNodeUID != 0 && sector.Mapper.Nodes.ContainsKey(this.EndNodeUID)))
                    {
                        this.Valid = true;

                        var stamps = BitConverter.ToInt32(sector.Stream, offset + 433);
                        this.BlockSize = 437 + stamps * 24;
                    }
                    else
                    {
                        this.Valid = false;
                    }

                    break;

                case Ets2ItemType.Prefab:
                    if (uid == 0x2935de9c700704)
                    { }

                    nodeCount = BitConverter.ToInt32(sector.Stream, offset + 81);
                    this.HideUI = (sector.Stream[offset + 0x36] & 0x02) != 0;

                    if (nodeCount > 0x20)
                    {
                        this.Valid = false;
                        return;
                    }

                    var somethingOffset = offset + 85 + 8 * nodeCount;
                    if (somethingOffset < offset || somethingOffset > sector.Stream.Length)
                    {
                        this.Valid = false;
                        return;
                    }

                    var something = BitConverter.ToInt32(sector.Stream, somethingOffset);

                    if (something < 0 || something > 32)
                    {
                        this.Valid = false;
                        return;
                    }

                    var OriginOffset = offset + 0x61 + nodeCount * 8 + something * 8;
                    if (OriginOffset < offset || OriginOffset > sector.Stream.Length)
                    {
                        this.Valid = false;
                        return;
                    }

                    this.Origin = sector.Stream[OriginOffset] & 0x03;

                    // Console.WriteLine("PREFAB @ " + uid.ToString("X16") + " origin: " + Origin);
                    var prefabId = (int) BitConverter.ToUInt32(sector.Stream, offset + 57);

                    // Invalidate unreasonable amount of node counts..
                    if (nodeCount < 0x20 && nodeCount != 0)
                    {
                        this.Valid = true;
                        for (int i = 0; i < nodeCount; i++)
                        {
                            var nodeUid = BitConverter.ToUInt64(sector.Stream, offset + 81 + 4 + i * 8);

                            // Console.WriteLine("prefab node link " + i + ": " + nodeUid.ToString("X16"));
                            // TODO: if node is in other sector..
                            if (this.AddNodeUID(nodeUid) == false)
                            {
                                // Console.WriteLine("Could not add prefab node " + nodeUid.ToString("X16") + " for item " + uid.ToString("X16"));
                                break;
                            }
                        }

                        this.PrefabNodeUID = this.NodesList.Keys.FirstOrDefault();
                    }

                    // Console.WriteLine("PREFAB ID: " + prefabId.ToString("X8"));
                    this.Prefab = sector.Mapper.LookupPrefab(prefabId);
                    if (this.Prefab == null)
                    {
                        // Console.WriteLine("Prefab ID: " + uid.ToString("X16") + " / " + prefabId.ToString("X") +
                        // " not found");
                    }

                    break;

                case Ets2ItemType.Company:
                    this.Valid = true;

                    // There are 3 nodes subtracted from found in sector:
                    // 1) The node of company itself
                    // 2) The node of loading area
                    // 3) The node of job 
                    nodeCount = this.Sector.Nodes.Count(x => x.ForwardItemUID == uid) - 2;
                    this.BlockSize = nodeCount * 8 + 109;

                    // Invalidate unreasonable amount of node counts..
                    if (nodeCount < 0x20)
                    {
                        var prefabItemUid = BitConverter.ToUInt64(sector.Stream, offset + 73);
                        var loadAreaNodeUid = BitConverter.ToUInt64(sector.Stream, offset + 93);
                        var jobAreaNodeUid = BitConverter.ToUInt64(sector.Stream, offset + 81);

                        if (this.AddNodeUID(loadAreaNodeUid) == false)
                        {
                            // Console.WriteLine("Could not add loading area node " + loadAreaNodeUid.ToString("X16"));
                        }
                        else if (this.AddNodeUID(jobAreaNodeUid) == false)
                        {
                            // Console.WriteLine("Could not add job area node" + jobAreaNodeUid.ToString("X16"));
                        }
                        else
                        {
                            for (int i = 0; i < nodeCount; i++)
                            {
                                var nodeUid = BitConverter.ToUInt64(sector.Stream, offset + 113 + i * 8);

                                // Console.WriteLine("company node link " + i + ": " + nodeUid.ToString("X16"));
                                if (this.AddNodeUID(nodeUid) == false)
                                {
                                    // Console.WriteLine("Could not add cargo area node " + nodeUid.ToString("X16") + " for item " + uid.ToString("X16"));
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        this.Valid = false;
                    }

                    break;

                case Ets2ItemType.Building:
                    var buildingNodeUid1 = BitConverter.ToUInt64(sector.Stream, offset + 73);
                    var buildingNodeUid2 = BitConverter.ToUInt64(sector.Stream, offset + 65);
                    this.Valid = this.AddNodeUID(buildingNodeUid1) && this.AddNodeUID(buildingNodeUid2);
                    this.BlockSize = 97;
                    break;

                case Ets2ItemType.Sign:
                    var signNodeUid = BitConverter.ToUInt64(sector.Stream, offset + 65);
                    this.BlockSize = 153;
                    this.Valid = this.AddNodeUID(signNodeUid);
                    break;

                case Ets2ItemType.Model:
                    var modelNodeUid = BitConverter.ToUInt64(sector.Stream, offset + 81);
                    this.BlockSize = 101;
                    this.Valid = this.AddNodeUID(modelNodeUid);
                    break;

                case Ets2ItemType.MapOverlay:
                    var mapOverlayNodeUid = BitConverter.ToUInt64(sector.Stream, offset + 65);
                    this.BlockSize = 73;
                    this.Valid = this.AddNodeUID(mapOverlayNodeUid);
                    break;

                case Ets2ItemType.Ferry:
                    var ferryNodeUid = BitConverter.ToUInt64(sector.Stream, offset + 73);
                    this.BlockSize = 93;
                    this.Valid = this.AddNodeUID(ferryNodeUid);
                    break;

                case Ets2ItemType.CutPlane:

                    nodeCount = BitConverter.ToInt32(sector.Stream, offset + 57);

                    // Invalidate unreasonable amount of node counts..
                    if (nodeCount < 0x20)
                    {
                        this.Valid = true;
                        for (int i = 0; i < nodeCount; i++)
                        {
                            var nodeUid = BitConverter.ToUInt64(sector.Stream, offset + 57 + 4 + i * 8);

                            // Console.WriteLine("cut plane node " + i + ": " + nodeUid.ToString("X16"));
                            if (this.AddNodeUID(nodeUid) == false)
                            {
                                // Console.WriteLine("Could not add cut plane node " + nodeUid.ToString("X16") + " for item " + uid.ToString("X16"));
                                break;
                            }
                        }
                    }

                    this.BlockSize = 61 + 8 * nodeCount;
                    break;

                case Ets2ItemType.TrafficRule:
                    nodeCount = BitConverter.ToInt32(sector.Stream, offset + 57);

                    // Invalidate unreasonable amount of node counts..
                    if (nodeCount < 0x20)
                    {
                        this.Valid = true;
                        for (int i = 0; i < nodeCount; i++)
                        {
                            var nodeUid = BitConverter.ToUInt64(sector.Stream, offset + 57 + 4 + i * 8);

                            // Console.WriteLine("traffic area node " + i + ": " + nodeUid.ToString("X16"));
                            if (this.AddNodeUID(nodeUid) == false)
                            {
                                // Console.WriteLine("Could not add traffic area node " + nodeUid.ToString("X16") + " for item " + uid.ToString("X16"));
                                break;
                            }
                        }
                    }

                    this.BlockSize = 73 + 8 * nodeCount;
                    break;

                case Ets2ItemType.Trigger:
                    nodeCount = BitConverter.ToInt32(sector.Stream, offset + 57);

                    // Invalidate unreasonable amount of node counts..
                    if (nodeCount < 0x20)
                    {
                        this.Valid = true;
                        for (int i = 0; i < nodeCount; i++)
                        {
                            var nodeUid = BitConverter.ToUInt64(sector.Stream, offset + 57 + 4 + i * 8);

                            // Console.WriteLine("trigger node " + i + ": " + nodeUid.ToString("X16"));
                            if (this.AddNodeUID(nodeUid) == false)
                            {
                                // Console.WriteLine("Could not add trigger node " + nodeUid.ToString("X16") + " for item " + uid.ToString("X16"));
                                break;
                            }
                        }
                    }

                    this.BlockSize = 117 + 8 * nodeCount;
                    break;

                case Ets2ItemType.BusStop:
                    var busStopUid = BitConverter.ToUInt64(sector.Stream, offset + 73);
                    this.BlockSize = 81;
                    this.Valid = this.AddNodeUID(busStopUid);
                    break;

                case Ets2ItemType.Garage:
                    // TODO: at offset 65 there is a int '1' value.. is it a list?
                    var garageUid = BitConverter.ToUInt64(sector.Stream, offset + 69);
                    this.BlockSize = 85;
                    this.Valid = this.AddNodeUID(garageUid);
                    break;

                case Ets2ItemType.FuelPump:
                    var dunno2Uid = BitConverter.ToUInt64(sector.Stream, offset + 57);
                    this.BlockSize = 73;
                    this.Valid = this.AddNodeUID(dunno2Uid);
                    break;

                case Ets2ItemType.Dunno:
                    this.Valid = true;
                    break;

                case Ets2ItemType.Service:
                    var locationNodeUid = BitConverter.ToUInt64(sector.Stream, offset + 57);
                    this.Valid = this.AddNodeUID(locationNodeUid);
                    this.BlockSize = 73;
                    break;

                case Ets2ItemType.City:
                    var CityID = BitConverter.ToUInt64(sector.Stream, offset + 57);
                    var NodeID = BitConverter.ToUInt64(sector.Stream, offset + 73);

                    if ((CityID >> 56) != 0)
                    {
                        break;
                    }

                    this.City = this.Sector.Mapper.LookupCityID(CityID);
                    this.Valid = this.City != string.Empty && NodeID != 0 && sector.Mapper.Nodes.ContainsKey(NodeID);
                    if (!this.Valid)
                    {
                        Console.WriteLine("Unknown city ID " + CityID.ToString("X16") + " at " + this.ItemUID.ToString("X16"));
                    }
                    else
                    {
                        this.StartNodeUID = NodeID;

                        // Console.WriteLine(CityID.ToString("X16") + " === " + City);
                    }

                    this.BlockSize = 81;
                    break;

                default:
                    this.Valid = false;
                    break;
            }

            // if (Valid)
            // Console.WriteLine("Item " + uid.ToString("X16") + " (" + Type.ToString() + ") is found at " + offset.ToString("X"));
        }

        public int BlockSize { get; private set; }

        // City/company info
        public string City { get; private set; }

        public string Company { get; set; }

        public Ets2Node EndNode { get; private set; }

        public ulong EndNodeUID { get; private set; }

        public int FileOffset { get; set; }

        public string FilePath { get; set; }

        public bool HideUI { get; private set; }

        public ulong ItemUID { get; private set; }

        // Dictionary <Prefab> , <NavigationWeight, Length, RoadList>>
        public Dictionary<Ets2Item, Tuple<float, float, IEnumerable<Ets2Item>>> Navigation { get; private set; }

        public Dictionary<ulong, Ets2Node> NodesList { get; private set; }

        /** Item specific values/interpretations **/

        // Prefab type
        public Ets2Prefab Prefab { get; private set; }

        public Ets2Node PrefabNode { get; set; }

        public ulong PrefabNodeUID { get; set; }

        // Road info
        public Ets2RoadLook RoadLook { get; private set; }

        public IEnumerable<PointF> RoadPolygons { get; private set; }

        public Ets2Sector Sector { get; private set; }

        public Ets2Node StartNode { get; private set; }

        public ulong StartNodeUID { get; private set; }

        public Ets2ItemType Type { get; set; }

        public bool Valid { get; private set; }

        public bool Apply(Ets2Node node)
        {
            if (node.NodeUID == this.PrefabNodeUID)
            {
                this.PrefabNode = node;
            }

            if (node.NodeUID == this.StartNodeUID)
            {
                this.StartNode = node;
                return true;
            }
            else if (node.NodeUID == this.EndNodeUID)
            {
                this.EndNode = node;
                return true;
            }
            else if (this.NodesList.ContainsKey(node.NodeUID))
            {
                this.NodesList[node.NodeUID] = node;
                return true;
            }
            else
            {
                // Console.WriteLine("Could not apply node " + node.NodeUID.ToString("X16") + " to item " + ItemUID.ToString("X16"));
                return false;
            }
        }

        /// <summary>
        ///     Generate road curves for a specific lane. The curve is generated with [steps]
        ///     nodes and positioned left or right from the road's middle point.
        ///     Additionally, each extra lane is shifted 4.5 game units outward.
        /// </summary>
        /// <param name="steps"></param>
        /// <param name="leftlane"></param>
        /// <param name="lane"></param>
        /// <returns></returns>
        public IEnumerable<Ets2Point> GenerateRoadCurve(int steps, bool leftlane, int lane)
        {
            var ps = new Ets2Point[steps];

            var sx = this.StartNode.X;
            var ex = this.EndNode.X;
            var sz = this.StartNode.Z;
            var ez = this.EndNode.Z;

            if (steps == 2)
            {
                sx += (float) Math.Sin(-this.StartNode.Yaw) * (leftlane ? -1 : 1) * (this.RoadLook.Offset + (0.5f + lane) * 4.5f);
                sz += (float) Math.Cos(-this.StartNode.Yaw) * (leftlane ? -1 : 1) * (this.RoadLook.Offset + (0.5f + lane) * 4.5f);

                ex += (float) Math.Sin(-this.EndNode.Yaw) * (leftlane ? -1 : 1) * (this.RoadLook.Offset + (0.5f + lane) * 4.5f);
                ez += (float) Math.Cos(-this.EndNode.Yaw) * (leftlane ? -1 : 1) * (this.RoadLook.Offset + (0.5f + lane) * 4.5f);

                ps[0] = new Ets2Point(sx, 0, sz, this.StartNode.Yaw);
                ps[1] = new Ets2Point(ex, 0, ez, this.EndNode.Yaw);
                return ps;
            }

            var radius = (float) Math.Sqrt((sx - ex) * (sx - ex) + (sz - ez) * (sz - ez));

            var tangentSX = (float) Math.Cos(-this.StartNode.Yaw) * radius;
            var tangentEX = (float) Math.Cos(-this.EndNode.Yaw) * radius;
            var tangentSZ = (float) Math.Sin(-this.StartNode.Yaw) * radius;
            var tangentEZ = (float) Math.Sin(-this.EndNode.Yaw) * radius;

            for (int k = 0; k < steps; k++)
            {
                var s = (float) k / (float) (steps - 1);
                var x = (float) Ets2CurveHelper.Hermite(s, sx, ex, tangentSX, tangentEX);
                var z = (float) Ets2CurveHelper.Hermite(s, sz, ez, tangentSZ, tangentEZ);
                var tx = (float) Ets2CurveHelper.HermiteTangent(s, sx, ex, tangentSX, tangentEX);
                var ty = (float) Ets2CurveHelper.HermiteTangent(s, sz, ez, tangentSZ, tangentEZ);
                var yaw = (float) Math.Atan2(ty, tx);
                x += (float) Math.Sin(-yaw) * (leftlane ? -1 : 1) * (this.RoadLook.Offset + (0.5f + lane) * 4.5f);
                z += (float) Math.Cos(-yaw) * (leftlane ? -1 : 1) * (this.RoadLook.Offset + (0.5f + lane) * 4.5f);
                ps[k] = new Ets2Point(x, 0, z, yaw);
            }

            return ps;
        }

        public void GenerateRoadPolygon(int steps)
        {
            if (this.RoadPolygons == null)
            {
                this.RoadPolygons = new PointF[0];
            }

            if (this.RoadPolygons != null && this.RoadPolygons.Count() == steps)
            {
                return;
            }

            if (this.StartNode == null || this.EndNode == null)
            {
                return;
            }

            if (this.Type != Ets2ItemType.Road)
            {
                return;
            }

            var ps = new PointF[steps];

            var sx = this.StartNode.X;
            var ex = this.EndNode.X;
            var sy = this.StartNode.Z;
            var ey = this.EndNode.Z;

            var radius = (float) Math.Sqrt((sx - ex) * (sx - ex) + (sy - ey) * (sy - ey));

            var tangentSX = (float) Math.Cos(-this.StartNode.Yaw) * radius;
            var tangentEX = (float) Math.Cos(-this.EndNode.Yaw) * radius;
            var tangentSY = (float) Math.Sin(-this.StartNode.Yaw) * radius;
            var tangentEY = (float) Math.Sin(-this.EndNode.Yaw) * radius;

            for (int k = 0; k < steps; k++)
            {
                var s = (float) k / (float) (steps - 1);
                var x = (float) Ets2CurveHelper.Hermite(s, sx, ex, tangentSX, tangentEX);
                var y = (float) Ets2CurveHelper.Hermite(s, sy, ey, tangentSY, tangentEY);
                ps[k] = new PointF(x, y);
            }

            this.RoadPolygons = ps;
        }

        public override string ToString()
        {
            return "Item #" + this.ItemUID.ToString("X16") + " (" + this.Type.ToString() + ")";
        }

        private bool AddNodeUID(ulong nodeUid)
        {
            if (nodeUid == 0 || this.Sector.Mapper.Nodes.ContainsKey(nodeUid) == false)
            {
                this.Valid = false;
                return false;
            }
            else
            {
                this.NodesList.Add(nodeUid, null);
                return true;
            }
        }
    }
}