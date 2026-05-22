namespace WildFarming.Ecosystem
{
    public class EcosystemConfig
    {
        public static EcosystemConfig Loaded { get; set; } = new EcosystemConfig();

        public bool EcosystemEnabled { get; set; } = true;

        public bool HarshWildPlants { get; set; } = true;

        /// <summary>Horizontal search radius for spontaneous reproduction.</summary>
        public int ReproduceRadius { get; set; } = 4;

        /// <summary>Chance per reproduction attempt (0–1).</summary>
        public float ReproduceChance { get; set; } = 0.08f;

        /// <summary>Minimum suitability score required to place a juvenile plant.</summary>
        public float MinFitness { get; set; } = 0.65f;

        /// <summary>Hours between reproduction attempts per mature plant.</summary>
        public double ReproduceIntervalHours { get; set; } = 48;

        /// <summary>Failed growth checks (×18h) in bad climate before the juvenile plant dies.</summary>
        public int MaxFailedSurvivalChecks { get; set; } = 5;

        /// <summary>Dev/test: multiply grow hours (1 = normal).</summary>
        public float GrowthHoursMultiplier { get; set; } = 1f;
    }
}
