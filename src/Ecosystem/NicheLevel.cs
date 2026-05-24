namespace WildFarming.Ecosystem
{
    /// <summary>Local soil moisture at a spread/stress cell (block + neighbors).</summary>
    public enum MoistureLevel
    {
        Dry = 0,
        Mesic = 1,
        Shoreline = 2,
        Wet = 3,
    }

    /// <summary>Sunlight at plant cell (OnlySunLight 0–24).</summary>
    public enum LightLevel
    {
        DeepShade = 0,
        Shade = 1,
        Partial = 2,
        Open = 3,
    }

    public struct LocalNiche
    {
        public MoistureLevel Moisture;
        public LightLevel Light;

        public LocalNiche(MoistureLevel moisture, LightLevel light)
        {
            Moisture = moisture;
            Light = light;
        }
    }
}
