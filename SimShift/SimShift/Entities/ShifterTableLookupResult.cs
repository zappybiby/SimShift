namespace SimShift.Entities
{
    public class ShifterTableLookupResult
    {
        public ShifterTableLookupResult(int gear, double thrScale, double usedSpeed, double usedLoad)
        {
            this.Gear = gear;
            this.ThrottleScale = thrScale;
            this.UsedSpeed = usedSpeed;
            this.UsedLoad = usedLoad;
        }

        public int Gear { get; private set; }

        public double ThrottleScale { get; private set; }

        public double UsedLoad { get; private set; }

        public double UsedSpeed { get; private set; }
    }
}