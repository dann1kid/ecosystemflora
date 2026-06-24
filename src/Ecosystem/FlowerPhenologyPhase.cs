namespace WildFarming.Ecosystem
{
    /// <summary>Simulation phase for meadow flower phenology (block appearance follows phase).</summary>
    public enum FlowerPhenologyPhase : byte
    {
        Dormant = 0,
        Vegetative = 1,
        Bloom = 2,
        Dieback = 3,
    }
}
