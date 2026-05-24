namespace WildFarming.Ecosystem
{
    public class EcosystemConfig
    {
        public static EcosystemConfig Loaded { get; set; } = new EcosystemConfig();

        /// <summary>
        /// Spread tuning bundle: <c>natural</c>, <c>lush</c>, <c>sparse</c>, or <c>custom</c> (manual fields only).
        /// Applied on server start when not <c>custom</c>.
        /// </summary>
        public string BalancePreset { get; set; } = EcosystemBalancePresets.Natural;

        public bool EcosystemEnabled { get; set; } = true;

        public bool HarshWildPlants { get; set; } = true;

        /// <summary>Enforce worldgen minRain/maxRain when spreading. Forest uses <see cref="LocalForestCover"/> (neighbor trees), not worldgen.</summary>
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

        /// <summary>
        /// When false (default), aquatic and terrestrial plants do not enforce spacing against each other
        /// (shore flowers no longer block reed/lily spread toward valid muddy cells).
        /// </summary>
        public bool ApplyCrossHabitatSpacing { get; set; } = false;

        /// <summary>Used when species table has SameSpeciesSpacing 0.</summary>
        public int DefaultSameSpeciesSpacing { get; set; } = 1;

        /// <summary>Used when species table has OtherSpeciesSpacing 0.</summary>
        public int DefaultOtherSpeciesSpacing { get; set; } = 1;

        /// <summary>±Y when scanning for nearby flowers for spacing.</summary>
        public int SpacingVerticalSearch { get; set; } = 2;

        /// <summary>Checks per tick for mod-placed saplings that matured into log-grown.</summary>
        public int MaxPendingTreeChecksPerTick { get; set; } = 12;

        // --- Flora context (v2) ---

        public bool UseFloraContext { get; set; } = true;

        public int FloraContextNeighborRadius { get; set; } = 2;

        /// <summary>Forest neighbors at or above this count → <see cref="FloraContext.ForestInterior"/>.</summary>
        public int FloraContextInteriorThreshold { get; set; } = 4;

        public float FloraOpenInteriorPenalty { get; set; } = 0.35f;

        public double FloraContextCacheHours { get; set; } = 12;

        // --- Unified cell competition (v2.1) ---

        public bool UseCellDisplacement { get; set; } = true;

        /// <summary>When empty cells exist in spread radius, pick only among them (mowing / gap colonization).</summary>
        public bool PreferSpreadToEmptyCells { get; set; } = true;

        /// <summary>Challenger spread score must exceed incumbent hold × this margin.</summary>
        public float DisplacementHoldMargin { get; set; } = 1.25f;

        public bool EnableStressDeath { get; set; } = true;

        public double StressRecheckHours { get; set; } = 18;

        public int MaxStressChecksPerTick { get; set; } = 24;

        public bool EnableSymbiosis { get; set; } = true;

        public int SymbiosisCascadeRadius { get; set; } = 4;

        // --- Local niche (v2.2) ---

        public bool UseNicheContext { get; set; } = true;

        public double NicheCacheHours { get; set; } = 12;

        /// <summary>Below this niche multiplier, stress checks count as failed survival.</summary>
        public float NicheStressThreshold { get; set; } = 0.45f;

        // --- Soil succession (v2.2) ---

        public bool UseSoilSuccession { get; set; } = true;

        /// <summary>Multiplier on spread/death soil impact deltas.</summary>
        public float SoilSuccessionStrength { get; set; } = 1f;

        /// <summary>When soil is tilled, add N/P/K from dominant wild plant role + tier.</summary>
        public bool UseFarmlandNutrientBridge { get; set; } = true;

        /// <summary>Multiplier on till nutrient bonuses.</summary>
        public float FarmlandNutrientBridgeStrength { get; set; } = 1f;
    }
}
