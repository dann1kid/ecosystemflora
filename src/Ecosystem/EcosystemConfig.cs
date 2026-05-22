namespace WildFarming.Ecosystem
{
    public class EcosystemConfig
    {
        public static EcosystemConfig Loaded { get; set; } = new EcosystemConfig();

        public bool EcosystemEnabled { get; set; } = true;

        public bool HarshWildPlants { get; set; } = true;

        /// <summary>Horizontal search radius for spontaneous reproduction.</summary>
        public int ReproduceRadius { get; set; } = 4;

        /// <summary>Search ±N blocks vertically for ground when spreading on slopes.</summary>
        public int ReproduceVerticalSearch { get; set; } = 5;

        /// <summary>Chance per reproduction attempt (0–1).</summary>
        public float ReproduceChance { get; set; } = 0.25f;

        /// <summary>Minimum suitability score required to place a juvenile plant.</summary>
        public float MinFitness { get; set; } = 0.5f;

        /// <summary>Hours between reproduction attempts per mature plant.</summary>
        public double ReproduceIntervalHours { get; set; } = 24;

        /// <summary>Failed checks (every 18h) in bad climate before the plant is removed. 5 ≈ 3.75 days.</summary>
        public int MaxFailedSurvivalChecks { get; set; } = 5;

        /// <summary>Dev/test: multiply grow hours (1 = normal).</summary>
        public float GrowthHoursMultiplier { get; set; } = 1f;

        /// <summary>Log reproduce register/spawn to server-main.log (for debugging).</summary>
        public bool ReproduceDebug { get; set; } = false;
    }
}
