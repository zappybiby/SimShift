using System.Collections.Generic;
using System.Linq;

namespace SimShift.MapTool
{
    public class Ets2PrefabRoute
    {
        public IndicatorSignal Indicator;

        public Ets2PrefabRoute(List<Ets2PrefabCurve> route, Ets2PrefabNode entry, Ets2PrefabNode exit)
        {
            this.Route = route;

            this.Entry = entry;
            this.Exit = exit;

            // TODO: Interpret indicator signal
        }

        public int End => this.Route.LastOrDefault().Index;

        public Ets2PrefabNode Entry { get; private set; }

        public Ets2PrefabNode Exit { get; private set; }

        public List<Ets2PrefabCurve> Route { get; private set; }

        public int Start => this.Route.FirstOrDefault().Index;

        public override string ToString()
        {
            return "Prefab route " + this.Start + " to  " + this.End;
        }
    }
}