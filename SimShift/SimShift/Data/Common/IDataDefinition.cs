namespace SimShift.Data.Common
{
    public interface IDataDefinition
    {
        float Brake { get; }

        string Car { get; set; }

        float EngineRpm { get; }

        float Fuel { get; }

        int Gear { get; }

        int Gears { get; }

        bool Paused { get; }

        float Speed { get; }

        float Throttle { get; }

        float Time { get; }
    }
}