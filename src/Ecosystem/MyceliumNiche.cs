namespace WildFarming.Ecosystem
{
    /// <summary>Wild mushroom habitat class for mycelium anchor stress (aligned with VS wiki groups).</summary>
    public enum MyceliumNiche
    {
        /// <summary>Needs canopy / forest cover and any tree host nearby.</summary>
        ForestAnyTree = 0,

        /// <summary>Deciduous log-grown / leaves nearby.</summary>
        ForestDeciduous = 1,

        /// <summary>Conifer log-grown / leaves nearby.</summary>
        ForestConifer = 2,

        /// <summary>Field / open meadow; stressed under dense forest.</summary>
        MeadowOpen = 3,

        /// <summary>BE on a living tree trunk (polypore).</summary>
        TrunkPolypore = 4,
    }
}
