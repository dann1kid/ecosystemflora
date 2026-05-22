namespace WildFarming.Ecosystem
{
    public class EcosystemConfig
    {
        public static EcosystemConfig Loaded { get; set; } = new EcosystemConfig();

        public bool EcosystemEnabled { get; set; } = true;

        public bool HarshWildPlants { get; set; } = true;

        public int ReproduceRadius { get; set; } = 4;

        public int ReproduceVerticalSearch { get; set; } = 5;

        public float ReproduceChance { get; set; } = 0.25f;

        public float MinFitness { get; set; } = 0.5f;

        public double ReproduceIntervalHours { get; set; } = 24;

        public int MaxFailedSurvivalChecks { get; set; } = 5;

        public float GrowthHoursMultiplier { get; set; } = 1f;

        public bool ReproduceDebug { get; set; } = false;

        /// <summary>Max reproduction attempts per server tick (spreads CPU load).</summary>
        public int MaxReproduceAttemptsPerTick { get; set; } = 48;

        /// <summary>Chunk columns to scan per tick after load (deferred registration).</summary>
        public int MaxChunkColumnsScannedPerTick { get; set; } = 3;

        /// <summary>Cap flower registrations per tick while draining the chunk queue.</summary>
        public int MaxRegistrationsPerTick { get; set; } = 256;

        /// <summary>Random delay spread when registering (hours) to avoid tick spikes.</summary>
        public bool StaggerReproduceAttempts { get; set; } = true;

        /// <summary>If true, only register/tick plants within PlayerActivationRadiusBlocks of a player.</summary>
        public bool OnlyActivateNearPlayers { get; set; } = false;

        public int PlayerActivationRadiusBlocks { get; set; } = 192;
    }
}
