namespace WildFarming.Ecosystem
{
    /// <summary>Calendar senescence stage after species lifespan (one stage advanced per game year).</summary>
    public enum TreeSenescencePhase : byte
    {
        None = 0,
        /// <summary>Leaf fluff stripped; spread and growth off; seasonal bud blocked.</summary>
        Declining = 1,
        /// <summary>Branchy crown skeleton removed.</summary>
        DeadCrown = 2,
        /// <summary>Short standing trunk (snag); still in registry until final year.</summary>
        Snag = 3,
    }
}
