namespace WildFarming.Ecosystem
{
    public class EcosystemConfig
    {
        public static EcosystemConfig Loaded { get; set; } = new EcosystemConfig();

        public bool EcosystemEnabled { get; set; } = true;

        public bool HarshWildPlants { get; set; } = true;

        /// <summary>Enforce worldgen minRain/maxRain and minForest/maxForest when spreading.</summary>
        public bool ApplyWorldgenRainForest { get; set; } = true;

        public int ReproduceRadius { get; set; } = 4;

        public int ReproduceVerticalSearch { get; set; } = 5;

        public float ReproduceChance { get; set; } = 0.25f;

        public float MinFitness { get; set; } = 0.5f;

        /// <summary>Legacy: hours between attempts when <see cref="UseCalendarScaledSpread"/> is false.</summary>
        public double ReproduceIntervalHours { get; set; } = 24;

        /// <summary>Spread attempts per in-game year at SpreadRate=1 (scales with DaysPerYear).</summary>
        public double ReproduceAttemptsPerYear { get; set; } = 36;

        /// <summary>Use calendar DaysPerYear/HoursPerDay instead of fixed hours.</summary>
        public bool UseCalendarScaledSpread { get; set; } = true;

        /// <summary>Per-species SpreadRate from ecology table scales interval and chance.</summary>
        public bool UseSpeciesSpreadRates { get; set; } = true;

        /// <summary>Min game-days between attempts (calendar mode). 0 = no floor.</summary>
        public double MinSpeciesReproduceIntervalDays { get; set; } = 0;

        /// <summary>Min hours between attempts (legacy mode only).</summary>
        public double MinSpeciesReproduceIntervalHours { get; set; } = 0;

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

        /// <summary>Minimum horizontal distance between spread plants.</summary>
        public bool PlantSpacingEnabled { get; set; } = true;

        /// <summary>Used when species table has SameSpeciesSpacing 0.</summary>
        public int DefaultSameSpeciesSpacing { get; set; } = 1;

        /// <summary>Used when species table has OtherSpeciesSpacing 0.</summary>
        public int DefaultOtherSpeciesSpacing { get; set; } = 1;

        /// <summary>±Y when scanning for nearby flowers for spacing.</summary>
        public int SpacingVerticalSearch { get; set; } = 2;
    }
}
