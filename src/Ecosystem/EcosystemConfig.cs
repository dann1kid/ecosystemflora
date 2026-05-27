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

        public float ReproduceChance { get; set; } = 0.5f;

        public float MinFitness { get; set; } = 0.45f;

        /// <summary>Legacy: hours between attempts when <see cref="UseCalendarScaledSpread"/> is false.</summary>
        public double ReproduceIntervalHours { get; set; } = 24;

        /// <summary>Spread attempts per in-game year at SpreadRate=1 (scales with DaysPerYear).</summary>
        public double ReproduceAttemptsPerYear { get; set; } = 72;

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

        /// <summary>
        /// Master logging switch. When false, suppresses all Notification/Warning
        /// log lines except startup and errors. VS string formatting + I/O is
        /// expensive even for filtered log levels.
        /// </summary>
        public bool VerboseLogging { get; set; } = false;

        /// <summary>Max reproduction attempts per server tick (spreads CPU load).</summary>
        public int MaxReproduceAttemptsPerTick { get; set; } = 64;

        /// <summary>Chunk columns to scan per tick after load (deferred registration).</summary>
        public int MaxChunkColumnsScannedPerTick { get; set; } = 6;

        /// <summary>Cap flower registrations per tick while draining the chunk queue.</summary>
        public int MaxRegistrationsPerTick { get; set; } = 512;

        /// <summary>Max milliseconds per game tick for spread processing. 0 = no limit.</summary>
        public int TickBudgetMs { get; set; } = 30;

        /// <summary>Max milliseconds per stress tick. Defaults to <see cref="TickBudgetMs"/> when 0.</summary>
        public int StressBudgetMs { get; set; } = 0;

        /// <summary>Interval (ms) between stress-check ticks. Higher = less CPU for stress, slower die-off.</summary>
        public int StressTickIntervalMs { get; set; } = 6000;

        /// <summary>Random delay spread when registering (hours) to avoid tick spikes.</summary>
        public bool StaggerReproduceAttempts { get; set; } = true;

        /// <summary>If true, only register/tick plants within PlayerActivationRadiusBlocks of a player.</summary>
        public bool OnlyActivateNearPlayers { get; set; } = true;

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

        /// <summary>Challenger spreadScore must exceed incumbent holdScore × this (lower = more turnover).</summary>
        public float DisplacementHoldMargin { get; set; } = 1.18f;

        /// <summary>When <see cref="PreferSpreadToEmptyCells"/> is on, multiply empty-cell fitness by this (displacement still possible).</summary>
        public float EmptySpreadFitnessMultiplier { get; set; } = 2.5f;

        public bool EnableStressDeath { get; set; } = true;

        public double StressRecheckHours { get; set; } = 18;

        public int MaxStressChecksPerTick { get; set; } = 16;

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

        /// <summary>Empty farmland near wild plants slowly regains N/P/K (fallow restoration).</summary>
        public bool EnableFallowRestoration { get; set; } = true;

        /// <summary>Multiplier on fallow nutrient restoration speed.</summary>
        public float FallowRestorationStrength { get; set; } = 1f;

        /// <summary>Do not spread, displace, stress-remove, or change soil inside land claims.</summary>
        public bool RespectLandClaims { get; set; } = true;

        // --- Seasonal ecology (v2.3) ---

        /// <summary>Spread chance and interval follow <see cref="WildSpeciesSeason"/> by game season.</summary>
        public bool UseSeasonalEcology { get; set; } = true;

        /// <summary>Winter die-off and fall die-off via stress checks (terrestrial).</summary>
        public bool SeasonalStressEnabled { get; set; } = true;

        // --- Trampling (v2.6) ---

        /// <summary>Plants near frequently-visited positions accumulate trampling stress and die.</summary>
        public bool EnableTrampling { get; set; } = false;

        /// <summary>Horizontal distance (blocks) at which a player causes trampling exposure.</summary>
        public int TramplingRadius { get; set; } = 1;

        /// <summary>Cumulative near-player stress ticks before trampling counts as a failed survival check.</summary>
        public int TramplingStressThreshold { get; set; } = 8;

        /// <summary>Apply soil degradation when a plant is trampled to death.</summary>
        public bool TramplingSoilDegradation { get; set; } = false;

        // --- Flower drygrass drops (v2.5) ---

        /// <summary>Append drygrass drops (knife/scythe) to flowers without removing original drops.</summary>
        public bool EnableFlowerDrygrass { get; set; } = true;

        // --- Ecology inspect UI (client dialog, server snapshot) ---

        /// <summary>Allow inspect hotkey (default I) — opens dialog with live plant + area scan.</summary>
        public bool EnableEcologyInspect { get; set; } = true;

        /// <summary>Minimum seconds between inspect requests per player.</summary>
        public double EcologyInspectCooldownSeconds { get; set; } = 2.0;

        /// <summary>Horizontal radius (blocks) for nearby-species tally in inspect dialog.</summary>
        public int EcologyInspectScanRadius { get; set; } = 16;

        /// <summary>Include top species near crosshair plant in inspect report.</summary>
        public bool EnableEcologyAreaScan { get; set; } = true;

        // --- Berry spread (v3.0, VS 1.22+ fruiting bush BE) ---

        /// <summary>
        /// When true, wild berry spread copies the parent bush's genetic traits to the new bush
        /// (same path as maturing a cutting). When false, vanilla random wild traits apply.
        /// </summary>
        public bool CloneBerryTraits { get; set; } = true;

        // --- Third-party ecology (v3.1) ---

        /// <summary>
        /// When true, blocks may register using JSON attributes (<c>ecologyParticipant</c>, etc.)
        /// without matching hardcoded vanilla <c>game:</c> paths.
        /// </summary>
        public bool EnableThirdPartyParticipants { get; set; } = true;
    }
}
