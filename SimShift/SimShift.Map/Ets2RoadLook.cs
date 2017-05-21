using System;
using System.IO;

namespace SimShift.MapTool
{
    public class Ets2RoadLook
    {
        public int LanesLeft;

        public int LanesRight;

        public float Offset;

        public float ShoulderLeft;

        public float ShoulderRight;

        public float SizeLeft;

        public float SizeRight;

        private Ets2Mapper Mapper;

        public Ets2RoadLook(string look, Ets2Mapper mapper)
        {
            this.LookID = look;
            this.Mapper = mapper;

            var roadLookData = mapper.LUTFolder + "-roadlook.sii";
            var fileData = File.ReadAllLines(roadLookData);

            var found = false;
            foreach (var k in fileData)
            {
                if (!found)
                {
                    if (k.StartsWith("road_look") && k.Contains(this.LookID))
                    {
                        found = true;
                    }
                }
                else
                {
                    // value:
                    if (k.Contains(":"))
                    {
                        var key = k;
                        var data = key.Substring(key.IndexOf(":") + 1).Trim();
                        key = key.Substring(0, key.IndexOf(":")).Trim();

                        switch (key)
                        {
                            case "road_size_left":
                                float.TryParse(data, out this.SizeLeft);
                                break;

                            case "road_size_right":
                                float.TryParse(data, out this.SizeRight);
                                break;

                            case "shoulder_size_right":
                                float.TryParse(data, out this.ShoulderLeft);
                                break;

                            case "shoulder_size_left":
                                float.TryParse(data, out this.ShoulderRight);
                                break;

                            case "road_offset":
                                float.TryParse(data, out this.Offset);
                                break;
                            case "lanes_left[]":
                                this.LanesLeft++;
                                this.IsLocal = data == "traffic_lane.road.local";
                                this.IsExpress = data == "traffic_lane.road.expressway";
                                this.IsHighway = data == "traffic_lane.road.motorway";

                                break;

                            case "lanes_right[]":
                                this.LanesRight++;
                                break;
                        }
                    }

                    if (k.Trim() == "}")
                    {
                        break;
                    }
                }
            }
        }

        public bool IsExpress { get; private set; }

        public bool IsHighway { get; private set; }

        public bool IsLocal { get; private set; }

        public string LookID { get; private set; }
    }
}