namespace SimShift.Entities
{
    public class ShifterTableLookupResult
    {
        public ShifterTableLookupResult(int gear, double thrScale, double usedSpeed, double usedLoad)
        {
            Gear = gear;
            ThrottleScale = thrScale;
            UsedSpeed = usedSpeed;
            UsedLoad = usedLoad;
        }

        public int Gear { get; private set; }

        public double ThrottleScale { get; private set; }

        public double UsedLoad { get; private set; }

        public double UsedSpeed { get; private set; }
    }
}