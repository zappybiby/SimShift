using System.Linq;

namespace SimShift.MapTool
{
    public class Ets2Company
    {
        public int MaxX;

        public int MaxY;

        public int MinX;

        public int MinY;

        public Ets2Prefab Prefab;

        public Ets2Company(string line, Ets2Mapper mapper)
        {
            Mapper = mapper;

            var d = line.Split(",".ToCharArray());

            PrefabID = d[0];
            int.TryParse(d[1], out MinX);
            int.TryParse(d[2], out MinY);
            int.TryParse(d[3], out MaxX);
            int.TryParse(d[4], out MaxY);

            // find prefab obj
            Prefab = mapper.PrefabsLookup.FirstOrDefault(x => x.IDSII == PrefabID);

            if (Prefab != null) Prefab.Company = this;
        }

        public Ets2Mapper Mapper { get; private set; }

        public string PrefabID { get; private set; }
    }
}