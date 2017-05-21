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
            this.Mapper = mapper;

            var d = line.Split(",".ToCharArray());

            this.PrefabID = d[0];
            int.TryParse(d[1], out this.MinX);
            int.TryParse(d[2], out this.MinY);
            int.TryParse(d[3], out this.MaxX);
            int.TryParse(d[4], out this.MaxY);

            // find prefab obj
            this.Prefab = mapper.PrefabsLookup.FirstOrDefault(x => x.IDSII == this.PrefabID);

            if (this.Prefab != null)
            {
                this.Prefab.Company = this;
            }
        }

        public Ets2Mapper Mapper { get; private set; }

        public string PrefabID { get; private set; }
    }
}