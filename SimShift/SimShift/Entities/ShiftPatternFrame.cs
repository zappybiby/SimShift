namespace SimShift.Entities
{
    public class ShiftPatternFrame
    {
        public ShiftPatternFrame(double clutch, double throttle, bool useOldGear, bool useNewGear)
        {
            this.Clutch = clutch;
            this.Throttle = throttle;
            this.AbsoluteThrottle = false;
            this.UseOldGear = useOldGear;
            this.UseNewGear = useNewGear;
        }

        public ShiftPatternFrame(double clutch, double throttle, bool absThr, bool useOldGear, bool useNewGear)
        {
            this.Clutch = clutch;
            this.Throttle = throttle;
            this.AbsoluteThrottle = absThr;
            this.UseOldGear = useOldGear;
            this.UseNewGear = useNewGear;
        }

        public bool AbsoluteThrottle { get; private set; }

        public double Clutch { get; private set; }

        public double Throttle { get; private set; }

        public bool UseNewGear { get; private set; }

        public bool UseOldGear { get; private set; }
    }
}