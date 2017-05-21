namespace SimShift.Entities
{
    public struct ConfigurableShiftPattern
    {
        public string Region { get; private set; }

        public string File { get; private set; }

        public ConfigurableShiftPattern(string region, string file)
            : this()
        {
            this.Region = region;
            this.File = file;
        }
    }
}