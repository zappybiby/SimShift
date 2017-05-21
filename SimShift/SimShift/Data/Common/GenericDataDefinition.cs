namespace SimShift.Data.Common
{
    public class GenericDataDefinition : IDataDefinition
    {
        public GenericDataDefinition(string car, float time, bool paused, int gear, int gears, float engineRpm, float fuel, float throttle, float brake, float speed)
        {
            this.Car = car;
            this.Time = time;
            this.Paused = paused;
            this.Gear = gear;
            this.Gears = gears;
            this.EngineRpm = engineRpm;
            this.Fuel = fuel;
            this.Throttle = throttle;
            this.Brake = brake;
            this.Speed = speed;
        }

        public float Brake { get; private set; }

        public string Car { get; set; }

        public float EngineRpm { get; private set; }

        public float Fuel { get; private set; }

        public int Gear { get; private set; }

        public int Gears { get; private set; }

        public bool Paused { get; private set; }

        public float Speed { get; private set; }

        public float Throttle { get; private set; }

        public float Time { get; private set; }
    }
}