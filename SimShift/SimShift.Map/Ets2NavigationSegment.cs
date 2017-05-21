using System;
using System.Collections.Generic;
using System.Linq;

using MathNet.Numerics.Statistics;

namespace SimShift.MapTool
{
    public class Ets2NavigationSegment
    {
        /*** NODE ***/
        public Ets2Node Entry;

        public Ets2Node Exit;

        public float Length;

        public List<Ets2NavigationSegmentOption> Options = new List<Ets2NavigationSegmentOption>();

        public Ets2Item Prefab;

        /*** SEGMENT ITEM ***/
        public List<Ets2Item> Roads = new List<Ets2Item>();

        public Ets2NavigationSegmentType Type;

        public float Weight;

        public Ets2NavigationSegment(Ets2Item prefab)
        {
            this.Type = Ets2NavigationSegmentType.Prefab;

            this.Prefab = prefab;
        }

        public Ets2NavigationSegment(IEnumerable<Ets2Item> roadPath, Ets2NavigationSegment prevSeg)
        {
            this.Type = Ets2NavigationSegmentType.Road;

            this.Roads = roadPath.ToList();

            // Generate entry/exit noads & road item order
            var firstRoad = this.Roads.FirstOrDefault();
            var lastRoad = this.Roads.LastOrDefault();

            if (prevSeg.Prefab.NodesList.ContainsValue(firstRoad.StartNode))
            {
                this.Entry = firstRoad.StartNode;
                this.Exit = lastRoad.EndNode;
                this.ReversedRoadChain = false;
                this.ReversedRoadElements = false;
            }
            else if (prevSeg.Prefab.NodesList.ContainsValue(firstRoad.EndNode))
            {
                this.Entry = firstRoad.EndNode;
                this.Exit = lastRoad.StartNode;
                this.ReversedRoadChain = false;
                this.ReversedRoadElements = true;
            }
            else if (prevSeg.Prefab.NodesList.ContainsValue(lastRoad.StartNode))
            {
                this.Entry = lastRoad.StartNode;
                this.Exit = firstRoad.EndNode;
                this.ReversedRoadChain = true;
                this.ReversedRoadElements = false;
            }
            else if (prevSeg.Prefab.NodesList.ContainsValue(lastRoad.EndNode))
            {
                this.Entry = lastRoad.EndNode;
                this.Exit = firstRoad.StartNode;
                this.ReversedRoadChain = true;
                this.ReversedRoadElements = true;
            }
            else
            { }
        }

        public bool ReversedRoadChain { get; set; }

        public bool ReversedRoadElements { get; set; }

        public IEnumerable<Ets2NavigationSegmentOption> Solutions
        {
            get
            {
                return this.Options.Where(x => x.Valid);
            }
        }

        public void GenerateHiRes(Ets2NavigationSegmentOption opt)
        {
            var pts = 512;
            if (this.Type == Ets2NavigationSegmentType.Road)
            {
                var curve1 = new List<Ets2Point>();
                foreach (var rd in this.Roads)
                {
                    var rdc = rd.GenerateRoadCurve(pts, opt.LeftLane, opt.EntryLane);
                    if (!curve1.Any())
                    {
                        if (!this.Entry.Point.CloseTo(rdc.FirstOrDefault()))
                        {
                            rdc = rdc.Reverse();
                        }
                    }
                    else
                    {
                        var lp = curve1.LastOrDefault();
                        if (!rdc.FirstOrDefault().CloseTo(lp))
                        {
                            rdc = rdc.Reverse();
                        }
                    }

                    curve1.AddRange(rdc);
                }

                var curve2 = new List<Ets2Point>();
                foreach (var rd in this.Roads)
                {
                    var rdc = rd.GenerateRoadCurve(pts, opt.LeftLane, opt.ExitLane);
                    if (!curve2.Any())
                    {
                        if (!this.Entry.Point.CloseTo(rdc.FirstOrDefault()))
                        {
                            rdc = rdc.Reverse();
                        }
                    }
                    else
                    {
                        var lp = curve2.LastOrDefault();
                        if (!rdc.FirstOrDefault().CloseTo(lp))
                        {
                            rdc = rdc.Reverse();
                        }
                    }

                    curve2.AddRange(rdc);
                }

                var curve = new List<Ets2Point>();
                curve.AddRange(curve1.Skip(0).Take(curve2.Count / 2));
                curve.AddRange(curve2.Skip(curve2.Count / 2).Take(curve2.Count / 2));
                if (this.ReversedRoadChain)
                {
                    curve.Reverse();
                }

                opt.HiResPoints = curve;
            }

            if (this.Type == Ets2NavigationSegmentType.Prefab)
            {
                opt.HiResPoints = opt.Points;
            }
        }

        public void GenerateOptions(Ets2NavigationSegment prevSeg, Ets2NavigationSegment nextSeg)
        {
            if (this.Type == Ets2NavigationSegmentType.Prefab)
            {
                var entryNode = -1;
                var exitNode = -1;
                var i = 0;

                // find node id's
                foreach (var kvp in this.Prefab.NodesList)
                {
                    if (this.Entry != null && kvp.Value.NodeUID == this.Entry.NodeUID)
                    {
                        entryNode = i;
                    }

                    if (this.Exit != null && kvp.Value.NodeUID == this.Exit.NodeUID)
                    {
                        exitNode = i;
                    }

                    i++;
                }

                // var routes = Prefab.Prefab.GetRoute(entryNode, exitNode);
                var routes = this.Prefab.Prefab.GetAllRoutes();

                if (routes == null || !routes.Any())
                {
                    return;
                }

                // Create options (we do this by just saving paths)
                foreach (var route in routes)
                {
                    var option = new Ets2NavigationSegmentOption();
                    option.EntryLane = -1;
                    option.ExitLane = -1;
                    option.Points = this.Prefab.Prefab.GeneratePolygonForRoute(route, this.Prefab.NodesList.FirstOrDefault().Value, this.Prefab.Origin).ToList();

                    this.Options.Add(option);
                }
            }

            var curveSize = 32;

            if (this.Type == Ets2NavigationSegmentType.Road)
            {
                var firstRoad = this.Roads.FirstOrDefault();

                // TODO: support UK
                // We have x number of lanes
                for (int startLane = 0; startLane < firstRoad.RoadLook.LanesRight; startLane++)
                {
                    var curve1 = new List<Ets2Point>();
                    foreach (var rd in this.Roads)
                    {
                        var rdc = rd.GenerateRoadCurve(curveSize, false, startLane);

                        if (!curve1.Any())
                        {
                            if (!this.Entry.Point.CloseTo(rdc.FirstOrDefault()))
                            {
                                rdc = rdc.Reverse();
                            }
                        }
                        else
                        {
                            var lp = curve1.LastOrDefault();
                            if (!rdc.FirstOrDefault().CloseTo(lp))
                            {
                                rdc = rdc.Reverse();
                            }
                        }

                        curve1.AddRange(rdc);
                    }

                    for (int endLane = 0; endLane < firstRoad.RoadLook.LanesRight; endLane++)
                    {
                        var curve2 = new List<Ets2Point>();
                        foreach (var rd in this.Roads)
                        {
                            var rdc = rd.GenerateRoadCurve(curveSize, false, endLane);

                            if (!curve2.Any())
                            {
                                if (!this.Entry.Point.CloseTo(rdc.FirstOrDefault()))
                                {
                                    rdc = rdc.Reverse();
                                }
                            }
                            else
                            {
                                var lp = curve2.LastOrDefault();
                                if (!rdc.FirstOrDefault().CloseTo(lp))
                                {
                                    rdc = rdc.Reverse();
                                }
                            }

                            curve2.AddRange(rdc);
                        }

                        var curve = new List<Ets2Point>();
                        curve.AddRange(curve1.Skip(0).Take(curve2.Count / 2));
                        curve.AddRange(curve2.Skip(curve2.Count / 2).Take(curve2.Count / 2));
                        if (this.ReversedRoadChain)
                        {
                            curve.Reverse();
                        }

                        var option = new Ets2NavigationSegmentOption();
                        option.LeftLane = false;
                        option.EntryLane = startLane;
                        option.ExitLane = endLane;
                        option.Points = curve;
                        option.LaneCrossOver = startLane != endLane;

                        this.Options.Add(option);
                    }
                }

                for (int startLane = 0; startLane < firstRoad.RoadLook.LanesLeft; startLane++)
                {
                    var curve1 = new List<Ets2Point>();
                    foreach (var rd in this.Roads)
                    {
                        var rdc = rd.GenerateRoadCurve(curveSize, true, startLane);
                        if (!curve1.Any())
                        {
                            if (!this.Entry.Point.CloseTo(rdc.FirstOrDefault()))
                            {
                                rdc = rdc.Reverse();
                            }
                        }
                        else
                        {
                            var lp = curve1.LastOrDefault();
                            if (!rdc.FirstOrDefault().CloseTo(lp))
                            {
                                rdc = rdc.Reverse();
                            }
                        }

                        curve1.AddRange(rdc);
                    }

                    for (int endLane = 0; endLane < firstRoad.RoadLook.LanesLeft; endLane++)
                    {
                        var curve2 = new List<Ets2Point>();
                        foreach (var rd in this.Roads)
                        {
                            var rdc = rd.GenerateRoadCurve(curveSize, true, endLane);
                            if (!curve2.Any())
                            {
                                if (!this.Entry.Point.CloseTo(rdc.FirstOrDefault()))
                                {
                                    rdc = rdc.Reverse();
                                }
                            }
                            else
                            {
                                var lp = curve2.LastOrDefault();
                                if (!rdc.FirstOrDefault().CloseTo(lp))
                                {
                                    rdc = rdc.Reverse();
                                }
                            }

                            curve2.AddRange(rdc);
                        }

                        var curve = new List<Ets2Point>();
                        curve.AddRange(curve1.Skip(0).Take(curve2.Count / 2));
                        curve.AddRange(curve2.Skip(curve2.Count / 2).Take(curve2.Count / 2));
                        if (!this.ReversedRoadChain)
                        {
                            curve.Reverse();
                        }

                        var option = new Ets2NavigationSegmentOption();
                        option.LeftLane = true;
                        option.EntryLane = startLane;
                        option.ExitLane = endLane;
                        option.Points = curve;
                        option.LaneCrossOver = startLane != endLane;

                        this.Options.Add(option);
                    }
                }
            }
        }

        public bool Match(int mySolution, Ets2NavigationSegment prefabSegment)
        {
            throw new NotImplementedException();
        }

        public bool MatchEntry(int solI, Ets2NavigationSegment prev)
        {
            if (this.Type == Ets2NavigationSegmentType.Road)
            {
                var entryPoint = this.Options[solI].Points.FirstOrDefault();
                bool res = false;
                foreach (var route in prev.Options)
                {
                    var last = route.Points.LastOrDefault();
                    if (last.CloseTo(entryPoint))
                    {
                        route.EntryLane = this.Options[solI].ExitLane;
                        if (route.ExitLane >= 0 && route.EntryLane >= 0)
                        {
                            route.Valid = true;
                        }

                        res = true;
                    }
                }

                return res;
            }
            else
            {
                return false;
            }
        }

        public bool MatchExit(int solI, Ets2NavigationSegment next)
        {
            if (this.Type == Ets2NavigationSegmentType.Road)
            {
                var exitPoint = this.Options[solI].Points.LastOrDefault();
                bool res = false;
                foreach (var route in next.Options)
                {
                    var first = route.Points.FirstOrDefault();
                    if (first.CloseTo(exitPoint))
                    {
                        route.ExitLane = this.Options[solI].EntryLane;
                        if (route.ExitLane >= 0 && route.EntryLane >= 0)
                        {
                            route.Valid = true;
                        }

                        res = true;
                    }
                }

                return res;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            return this.Solutions.Count() + " NAVSEG " + this.Type.ToString() + " " + ((this.Type == Ets2NavigationSegmentType.Road) ? (this.Roads.Count() + " roads / " + this.Entry.NodeUID.ToString("X16") + " > " + this.Exit.NodeUID.ToString("X16")) : this.Prefab.ItemUID.ToString("X16"));
        }

        /*** SOLUTIONS ***/
        public class Ets2NavigationSegmentOption
        {
            public int EntryLane;

            public int ExitLane;

            public List<Ets2Point> HiResPoints = new List<Ets2Point>();

            // only valid for road segments
            public bool LaneCrossOver;

            public List<Ets2Point> Points = new List<Ets2Point>();

            public bool Valid = false;

            public bool LeftLane { get; set; }

            public override string ToString()
            {
                return "NAVSEGOPT " + this.EntryLane + " > " + this.ExitLane + " (Valid: " + this.Valid + ")";
            }
        }
    }
}