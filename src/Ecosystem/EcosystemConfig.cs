using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    public class EcosystemConfig
    {
        public const string ConfigFileName = "ecosystemflora.json";

        public static EcosystemConfig Loaded { get; set; } = new EcosystemConfig();

        /// <summary>
        /// Load ModConfig/ecosystemflora.json on server and client.
        /// After load, missing keys get C# defaults and the file is rewritten so new options appear on disk.
        /// </summary>
        public static void TryLoadFromDisk(ICoreAPI api, bool createDefaultIfMissing)
        {
            if (api == null) return;

            EcosystemConfig fromDisk = null;
            try
            {
                fromDisk = api.LoadModConfig<EcosystemConfig>(ConfigFileName);
            }
            catch
            {
                if (!createDefaultIfMissing) return;

                Loaded = new EcosystemConfig();
                ApplyBalancePreset(Loaded);
                api.StoreModConfig(Loaded, ConfigFileName);
                return;
            }

            if (fromDisk != null)
            {
                Loaded = fromDisk;
            }
            else
            {
                Loaded = new EcosystemConfig();
            }

            EcosystemBalancePresets.TryLoadFilePresets(api);
            ApplyBalancePreset(Loaded);

            if (ShouldPersistConfig(createDefaultIfMissing, fromDisk != null))
            {
                api.StoreModConfig(Loaded, ConfigFileName);
            }
        }

        static void ApplyBalancePreset(EcosystemConfig cfg)
        {
            if (cfg == null) return;
            if (EcosystemBalancePresets.IsKnownPreset(cfg.BalancePreset))
            {
                EcosystemBalancePresets.Apply(cfg, cfg.BalancePreset);
            }
        }

        internal static bool ShouldPersistConfig(bool createDefaultIfMissing, bool fileExisted) =>
            fileExisted || createDefaultIfMissing;

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

        /// <summary>Reeds/tule/papyrus spread only from mat edge, one orthogonal cell (rhizome front).</summary>
        public bool UseRhizomeSpreadForReeds { get; set; } = true;

        /// <summary>Rare wind/water seed or stem-fragment jump for rhizome reeds.</summary>
        public bool RhizomeSeedDispersalEnabled { get; set; } = true;

        /// <summary>Multiplier on per-species seed dispersal chance.</summary>
        public float RhizomeSeedDispersalChanceScale { get; set; } = 1f;

        /// <summary>Fitness multiplier for distant seed/fragment candidates (harder establishment).</summary>
        public float RhizomeSeedDispersalFitnessScale { get; set; } = 0.25f;

        /// <summary>Water lily spreads as a floating pad mat (eight-connected edge), not reed rhizome.</summary>
        public bool UseSurfaceMatSpreadForLilies { get; set; } = true;

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

        /// <summary>Spread colonizer flowers as small juvenile blocks that mature before reproducing.</summary>
        public bool EnableFlowerSpreadMaturation { get; set; } = true;

        /// <summary>Calendar pause on flower parents after each spread attempt (independent of juvenile maturation).</summary>
        public bool EnableFlowerSpreadAttemptCooldown { get; set; } = true;

        /// <summary>Scales per-species post-spread cooldown hours for meadow flowers (higher = shorter pause).</summary>
        public float FlowerSpreadCooldownHoursMultiplier { get; set; } = 1f;

        /// <summary>Juvenile → mature checks per reproduce tick.</summary>
        public int MaxPendingFlowerMaturationChecksPerTick { get; set; } = 32;

        /// <summary>Meadow flower phenology: energy, phases, bloom-gated spread, block sync.</summary>
        public bool EnableFlowerPhenology { get; set; } = true;

        /// <summary>Min local °C to enter or sustain bloom.</summary>
        public float FlowerBloomMinTemperature { get; set; } = 5f;

        /// <summary>Local °C above which flowers enter dieback.</summary>
        public float FlowerBloomMaxTemperature { get; set; } = 32f;

        /// <summary>Vegetative energy required before bloom (arbitrary units).</summary>
        public float FlowerBloomEnergyThreshold { get; set; } = 1f;

        /// <summary>Vegetative energy gained per game day at season activity 1.0.</summary>
        public float FlowerPhenologyEnergyGainPerDay { get; set; } = 0.15f;

        /// <summary>Phenology state advances per reproduce tick (round-robin).</summary>
        public int MaxFlowerPhenologyChecksPerTick { get; set; } = 48;

        /// <summary>Spread tallgrass as veryshort; register for spread only after vanilla growth reaches short+.</summary>
        public bool EnableTallgrassSpreadMaturation { get; set; } = true;

        /// <summary>Establishing tallgrass promotion checks per reproduce tick.</summary>
        public int MaxPendingTallgrassPromotionChecksPerTick { get; set; } = 32;

        /// <summary>Ground ferns spread one orthogonal step from patch edge (rhizome mat).</summary>
        public bool EnableFernRhizomeSpread { get; set; } = true;

        /// <summary>Spread ferns as small juvenile blocks that mature before reproducing.</summary>
        public bool EnableFernSpreadMaturation { get; set; } = true;

        /// <summary>Calendar pause on fern parents after each spread attempt.</summary>
        public bool EnableFernSpreadAttemptCooldown { get; set; } = true;

        /// <summary>Scales per-species post-spread cooldown hours for ferns.</summary>
        public float FernSpreadCooldownHoursMultiplier { get; set; } = 1f;

        /// <summary>Fern spread only during active sporulation season (season curve threshold).</summary>
        public bool EnableFernSporulationGate { get; set; } = true;

        /// <summary>Juvenile fern → mature checks per reproduce tick.</summary>
        public int MaxPendingFernMaturationChecksPerTick { get; set; } = 24;

        /// <summary>Ground fern phenology: dormant/sporulating/dieback blocks and spread gates.</summary>
        public bool EnableFernPhenology { get; set; } = true;

        /// <summary>Fern phenology state advances per reproduce tick (round-robin).</summary>
        public int MaxFernPhenologyChecksPerTick { get; set; } = 32;

        /// <summary>Tallgrass phenology: winter dormant and stress dieback visuals.</summary>
        public bool EnableTallgrassPhenology { get; set; } = true;

        /// <summary>Tallgrass phenology checks per reproduce tick.</summary>
        public int MaxTallgrassPhenologyChecksPerTick { get; set; } = 32;

        /// <summary>Wild berry colony mat (rhizome/runner edge + optional seed jumps).</summary>
        public bool EnableBerryColonySpread { get; set; } = true;

        /// <summary>Shore sedges spread from mat edge with optional seed jumps (vegetative + seed mix).</summary>
        public bool EnableShoreSedgeMatSpread { get; set; } = true;

        /// <summary>Wild berry bushes reset to cutting state on spread; register when mature.</summary>
        public bool EnableBerrySpreadMaturation { get; set; } = true;

        /// <summary>Pending berry maturation checks per reproduce tick.</summary>
        public int MaxPendingBerryMaturationChecksPerTick { get; set; } = 24;

        /// <summary>Vanilla stumps from senescent snag collapse decay after calendar years.</summary>
        public bool EnableStumpDecay { get; set; } = true;

        /// <summary>Game years before a scheduled stump block is removed.</summary>
        public double StumpDecayYears { get; set; } = 10;

        /// <summary>Stump decay checks per reproduce tick.</summary>
        public int MaxStumpDecayChecksPerTick { get; set; } = 16;

        /// <summary>Lightweight history hint (H key / throttled look-at).</summary>
        public bool EnableEcologyHistoryHint { get; set; } = true;

        /// <summary>Event wake pulls NextAttemptHours forward by at most this many game hours (after spawn cooldown).</summary>
        public double EventWakeRetryHours { get; set; } = 6;

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
        public int MaxChunkColumnsScannedPerTick { get; set; } = 16;

        /// <summary>Cap flower registrations per tick while draining the chunk queue.</summary>
        public int MaxRegistrationsPerTick { get; set; } = 2048;

        /// <summary>Drain player-vicinity chunk scans before the background registration queue.</summary>
        public bool EnablePlayerPriorityRegistration { get; set; } = true;

        /// <summary>Complete nearby chunk registration in one load callback (burst ms budget).</summary>
        public bool EnableBurstRegistrationNearPlayers { get; set; } = true;

        /// <summary>Horizontal block radius for priority/burst registration near a player.</summary>
        public int PlayerRegistrationPriorityRadiusBlocks { get; set; } = 16;

        /// <summary>Extra chunk scan passes per tick for the priority registration queue.</summary>
        public int MaxPriorityChunkScansPerTick { get; set; } = 48;

        /// <summary>Registration cap per tick for the priority queue (separate from background).</summary>
        public int MaxPriorityRegistrationsPerTick { get; set; } = 8192;

        /// <summary>Per-pass ms budget for priority registration scans.</summary>
        public int PriorityRegistrationBudgetMs { get; set; } = 80;

        /// <summary>Total ms budget to finish one chunk on load near a player.</summary>
        public int BurstRegistrationBudgetMs { get; set; } = 80;

        /// <summary>Max registrations while completing one burst chunk near a player.</summary>
        public int MaxBurstRegistrationsPerChunk { get; set; } = 4096;

        /// <summary>Registry applies per chunk-scan tick from the pending registration queue.</summary>
        public int MaxRegistryAppliesPerTick { get; set; } = 512;

        /// <summary>Max registry inserts per chunk per drain pass (fairness cap within one tick).</summary>
        public int MaxRegistryAppliesPerChunkPerTick { get; set; } = 256;

        /// <summary>Background column-classification workers (snapshot + SetBlock stay on main thread).</summary>
        public int RegistrationWorkerCount { get; set; } = 0;

        /// <summary>Extra pending applies per tick for player-priority chunks before background drain.</summary>
        public int MaxPriorityRegistryAppliesPerTick { get; set; } = 2048;

        /// <summary>Capture block ids on main; column classification runs on a background thread.</summary>
        public bool EnableBackgroundRegistrationScan { get; set; } = true;

        /// <summary>Capture spread env snapshots on main; fitness scoring runs on background threads.</summary>
        public bool EnableBackgroundSpreadSolve { get; set; } = true;

        /// <summary>Background spread scoring workers (snapshot + SetBlock stay on main thread).</summary>
        public int SpreadWorkerCount { get; set; } = 0;

        /// <summary>Block cells copied into a chunk snapshot per main-thread tick (background scan).</summary>
        public int MaxRegistrationSnapshotCellsPerTick { get; set; } = 8192;

        public int ResolvePriorityRegistrationBudgetMs() =>
            PriorityRegistrationBudgetMs > 0 ? PriorityRegistrationBudgetMs : ResolveRegistrationBudgetMs();

        /// <summary>Max milliseconds per game tick for spread processing. 0 = no limit.</summary>
        public int TickBudgetMs { get; set; } = 30;

        /// <summary>Spread attempt budget ms/tick. 0 = use <see cref="TickBudgetMs"/>.</summary>
        public int SpreadBudgetMs { get; set; } = 30;

        /// <summary>Chunk registration scan budget ms/tick. 0 = use <see cref="TickBudgetMs"/>.</summary>
        public int RegistrationBudgetMs { get; set; } = 25;

        /// <summary>Max milliseconds per stress tick. Defaults to <see cref="TickBudgetMs"/> when 0.</summary>
        public int StressBudgetMs { get; set; } = 0;

        public int ResolveSpreadBudgetMs() => SpreadBudgetMs > 0 ? SpreadBudgetMs : TickBudgetMs;

        public int ResolveRegistrationBudgetMs() => RegistrationBudgetMs > 0 ? RegistrationBudgetMs : TickBudgetMs;

        /// <summary>Log reproduce-tick phase timings when registry is large (server diagnostics).</summary>
        public bool EnableReproduceTickProfiling { get; set; } = false;

        /// <summary>Minimum registry size before profiling logs emit.</summary>
        public int ReproduceTickProfilingMinRegistry { get; set; } = 2000;

        /// <summary>Minimum real-time ms between profiling log lines.</summary>
        public int ReproduceTickProfilingIntervalMs { get; set; } = 30000;

        /// <summary>Interval (ms) between stress-check ticks. Higher = less CPU for stress, slower die-off.</summary>
        public int StressTickIntervalMs { get; set; } = 5500;

        /// <summary>Real-time ms between spread / foliage / tree-growth ticks.</summary>
        public int ReproduceTickIntervalMs { get; set; } = 2000;

        /// <summary>
        /// Real-time ms between deferred chunk-registration ticks.
        /// Use a value not divisible by <see cref="ReproduceTickIntervalMs"/> to avoid aligned CPU spikes.
        /// </summary>
        public int ChunkScanTickIntervalMs { get; set; } = 2300;

        /// <summary>Random delay spread when registering (hours) to avoid tick spikes.</summary>
        public bool StaggerReproduceAttempts { get; set; } = true;

        /// <summary>
        /// Playtest / perf shortcut: when true, spread, stress, tree aging, and chunk scans only run near players.
        /// Default false — normal play processes all plants in loaded chunks (registry scope).
        /// </summary>
        public bool OnlyActivateNearPlayers { get; set; } = false;

        /// <summary>
        /// When true (and <see cref="OnlyActivateNearPlayers"/> is false), spread, stress, and tree/ferntree aging
        /// run only in registry chunks within <see cref="PlayerActivationRadiusBlocks"/> of a player.
        /// Chunk registration scans are unchanged. Use for large loaded areas without full playtest mode.
        /// </summary>
        public bool LimitSpreadNearPlayers { get; set; } = false;

        /// <summary>Round-robin spread across all registry chunks (Phase 6.1).</summary>
        public bool EnableChunkFairSpread { get; set; } = true;

        /// <summary>Max spread attempts per registry chunk per reproduce tick when chunk-fair spread is on.</summary>
        public int MaxSpreadAttemptsPerChunkPerTick { get; set; } = 2;

        /// <summary>How many registry chunks to visit per reproduce tick when chunk-fair spread is on.</summary>
        public int MaxSpreadChunksVisitedPerTick { get; set; } = 32;

        /// <summary>Wake nearby reproducers on ecology-relevant block changes (Phase 6.3).</summary>
        public bool EnableEventDrivenSpread { get; set; } = true;

        /// <summary>Wake seasonal reproducers once per in-game month (Phase 6.6).</summary>
        public bool EnableSeasonCoarseWake { get; set; } = true;

        /// <summary>Horizontal wake radius in blocks. 0 = derive from spread radius, spacing, and flora context.</summary>
        public int EcologyWakeRadiusBlocks { get; set; } = 0;

        /// <summary>Cache spread cell snapshots (blocks + rain + forest cover) on the spread hot path (Phase 6.4).</summary>
        public bool EnableEcologyColumnCache { get; set; } = true;

        /// <summary>Evaluate spread into a pending queue; SetBlock runs in a chunk-fair commit pass (Phase 6.5).</summary>
        public bool EnableTwoPhaseSpreadPlacement { get; set; } = true;

        /// <summary>Max SetBlock commits per reproduce tick when two-phase spread is on. 0 = use MaxReproduceAttemptsPerTick.</summary>
        public int MaxSpreadCommitsPerTick { get; set; } = 0;

        /// <summary>Max target chunks visited per commit pass. 0 = use MaxSpreadChunksVisitedPerTick.</summary>
        public int MaxSpreadCommitChunksVisitedPerTick { get; set; } = 0;

        /// <summary>Max commits per target chunk per tick when two-phase spread is on. 0 = use MaxSpreadAttemptsPerChunkPerTick.</summary>
        public int MaxSpreadCommitsPerChunkPerTick { get; set; } = 0;

        public int PlayerActivationRadiusBlocks { get; set; } = 192;

        /// <summary>Minimum horizontal distance between spread plants.</summary>
        public bool PlantSpacingEnabled { get; set; } = true;

        /// <summary>
        /// When false (default), aquatic and terrestrial plants do not enforce spacing against each other
        /// (shore flowers no longer block reed/lily spread toward valid muddy cells).
        /// </summary>
        public bool ApplyCrossHabitatSpacing { get; set; } = true;

        /// <summary>Used when species table has SameSpeciesSpacing 0.</summary>
        public int DefaultSameSpeciesSpacing { get; set; } = 1;

        /// <summary>Used when species table has OtherSpeciesSpacing 0.</summary>
        public int DefaultOtherSpeciesSpacing { get; set; } = 1;

        /// <summary>±Y when scanning for nearby flowers for spacing.</summary>
        public int SpacingVerticalSearch { get; set; } = 2;

        /// <summary>Checks per tick for mod-placed saplings that matured into log-grown.</summary>
        public int MaxPendingTreeChecksPerTick { get; set; } = 12;

        /// <summary>Round-robin column scan for log-grown trunks that appeared after chunk load.</summary>
        public bool EnableCyclicTreeDiscovery { get; set; } = true;

        /// <summary>Chunk columns scanned per tick for cyclic tree discovery (TreeTrunkDiscovery only).</summary>
        public int MaxTreeRescanColumnsPerTick { get; set; } = 16;

        /// <summary>Round-robin live scan for wild flora parents that appeared after chunk load.</summary>
        public bool EnableCyclicFloraDiscovery { get; set; } = true;

        /// <summary>Chunk columns scanned per tick for cyclic flora discovery.</summary>
        public int MaxFloraRescanColumnsPerTick { get; set; } = 32;

        // --- Wild tree aging (v3.6) — see docs/TREE_AGING.md ---

        /// <summary>Once per game year, extend trunk height and crown spread with grown blocks.</summary>
        public bool EnableTreeAging { get; set; } = true;

        /// <summary>Wild trees processed per reproduce tick (global round-robin; filtered to player radius when <see cref="OnlyActivateNearPlayers"/> or <see cref="LimitSpreadNearPlayers"/>).</summary>
        public int MaxTreeGrowthAttemptsPerTick { get; set; } = 6;

        /// <summary>Multiplier on growth pace vs reference size (higher = faster maturation).</summary>
        public float TreeGrowthActivityScale { get; set; } = 1f;

        /// <summary>When calendar age reaches species senescence horizon, phased natural death begins.</summary>
        public bool EnableTreeSenescence { get; set; } = true;

        /// <summary>Pioneer/mid/climax spread curves vs local forest cover at sapling cells.</summary>
        public bool EnableTreeSeralSuccession { get; set; } = true;

        /// <summary>Log-grown blocks left standing during snag phase (final year collapses to remains or air).</summary>
        public int TreeSenescenceSnagBlocks { get; set; } = 3;

        /// <summary>Leave vanilla stump + fallen logs when snag collapses (not log-grown).</summary>
        public bool EnableTreeSenescenceRemains { get; set; } = true;

        /// <summary>Horizontal debarked logs scattered near stump on final senescence year (0 = stump only).</summary>
        public int TreeSenescenceFallenLogCount { get; set; } = 3;

        /// <summary>Register and spread vanilla ferntree-normal columns (tropical arborescent fern).</summary>
        public bool EnableFerntreeEcology { get; set; } = true;

        /// <summary>Wild vine tips spread downward and along vertical surfaces.</summary>
        public bool EnableWildVineEcology { get; set; } = true;

        /// <summary>Horizontal scan radius when capturing adjacent wall faces.</summary>
        public int WildVineWallCaptureRadius { get; set; } = 4;

        /// <summary>Vertical scan span when capturing adjacent wall faces.</summary>
        public int WildVineWallCaptureHeight { get; set; } = 6;

        /// <summary>Trunk segments left during ferntree snag senescence year.</summary>
        public int FerntreeSenescenceSnagSegments { get; set; } = 2;

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

        /// <summary>When empty cells exist in spread radius, pick only among them (displacement still runs if none).</summary>
        public bool EnableEmptyFirstSpreadCollect { get; set; } = true;

        /// <summary>Skip spread columns known to hold ecology plants (from spacing index) on empty-first pass.</summary>
        public bool EnableSpreadColumnOccupancyHint { get; set; } = true;

        /// <summary>When <see cref="PreferSpreadToEmptyCells"/> is on, multiply empty-cell fitness by this (displacement still possible).</summary>
        public float EmptySpreadFitnessMultiplier { get; set; } = 2.5f;

        public bool EnableStressDeath { get; set; } = true;

        public double StressRecheckHours { get; set; } = 18;

        public int MaxStressChecksPerTick { get; set; } = 16;

        public bool EnableSymbiosis { get; set; } = true;

        /// <summary>Radius for host-cache invalidation and ecology wake when a symbiosis host is removed.</summary>
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

        /// <summary>Do not swap soil blocks when slabs or other builds occupy the column above ground.</summary>
        public bool SoilSuccessionSkipWhenBuiltAbove { get; set; } = true;

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

        // --- Mycelium niche (v3.1.12) ---

        /// <summary>
        /// Meadow spread penalty and forest understory bonus near active vanilla mycelium anchors
        /// (aligned with <c>BlockEntityMycelium</c> growRange).
        /// </summary>
        public bool EnableMyceliumNiche { get; set; } = true;

        /// <summary>Horizontal Chebyshev scan radius; vanilla growRange is 7.</summary>
        public int MyceliumZoneRadius { get; set; } = 7;

        /// <summary>Meadow-role spread fitness at the mycelium anchor cell (linear taper to 1.0 at radius).</summary>
        public float MyceliumMeadowSpreadPenalty { get; set; } = 0.35f;

        /// <summary>Forest-role spread fitness at the anchor (linear taper to 1.0 at radius).</summary>
        public float MyceliumForestSpreadBonus { get; set; } = 1.22f;

        /// <summary>Skip soil succession and fallow drip on blocks with active mycelium BE.</summary>
        public bool MyceliumSkipSoilSuccession { get; set; } = true;

        /// <summary>Register vanilla BlockEntityMycelium anchors for stress ecology (network spread — later).</summary>
        public bool EnableMyceliumEcology { get; set; } = true;

        /// <summary>Horizontal tree-host search radius for forest mycelium survival.</summary>
        public int MyceliumTreeHostRadius { get; set; } = 4;

        /// <summary>Below this local forest cover, forest mycelium accumulates stress in open context.</summary>
        public float MyceliumForestMinForestCover { get; set; } = 0.12f;

        /// <summary>Above this cover, meadow mycelium accumulates stress.</summary>
        public float MyceliumMeadowMaxForestCover { get; set; } = 0.45f;

        /// <summary>Slow orthogonal spread of vanilla mycelium anchors (phase 3).</summary>
        public bool EnableMyceliumNetworkSpread { get; set; } = true;

        /// <summary>Scales mycelium spread interval (lower = slower).</summary>
        public float MyceliumSpreadRate { get; set; } = 0.12f;

        /// <summary>Network spread attempts per in-game year at <see cref="MyceliumSpreadRate"/> = 1.</summary>
        public double MyceliumSpreadAttemptsPerYear { get; set; } = 4;

        /// <summary>Minimum fitness to colonize or displace a neighbor anchor cell.</summary>
        public float MyceliumSpreadMinFitness { get; set; } = 0.35f;

        // --- Seasonal ecology (v2.3) ---

        /// <summary>Spread chance and interval follow <see cref="WildSpeciesSeason"/> by game season.</summary>
        public bool UseSeasonalEcology { get; set; } = true;

        /// <summary>Winter die-off and fall die-off via stress checks (terrestrial).</summary>
        public bool SeasonalStressEnabled { get; set; } = true;

        // --- Canopy foliage v3.3 — see docs/CANOPY_PHENOLOGY.md ---

        /// <summary>Per-cell seasonal foliage on deciduous log-grown / branchy / leaves-grown.</summary>
        public bool EnableSeasonalFoliage { get; set; } = true;

        /// <summary>Random foliage cells ticked per reproduce pass (hybrid/random modes; 0 = off).</summary>
        public int MaxFoliageCellsTickedPerTick { get; set; } = 0;

        /// <summary>Wall-time cap for foliage random-tick per reproduce pass (0 = no extra cap).</summary>
        public int FoliageBudgetMs { get; set; } = 10;

        /// <summary>chunk = column sync on load (v3.4); hybrid = chunk + random tick; random = legacy v3.3.</summary>
        public string FoliageSyncMode { get; set; } = "chunk";

        /// <summary>Wall-time budget per chunk-sync drain pass (ms).</summary>
        public int FoliageChunkSyncBudgetMs { get; set; } = 12;

        /// <summary>Max chunks resumed per chunk-scan tick.</summary>
        public int FoliageChunkWorkPerTick { get; set; } = 4;

        /// <summary>On chunk scan, catch up foliage to current season (autumn strip + spring bud).</summary>
        public bool FoliageCatchUpOnChunkLoad { get; set; } = true;

        /// <summary>Max catch-up ops (strip + bud) per chunk per scan pass (0 = unlimited).</summary>
        public int MaxFoliageCatchUpPerChunk { get; set; } = 2048;

        /// <summary>Column scan depth above rain heightmap (0 = full world height).</summary>
        public int FoliageColumnScanHeightAboveSurface { get; set; } = 0;

        /// <summary>Peak autumn activity before optional branchy strip (0 = keep branchy skeleton).</summary>
        public float FoliagePeakAutumnBranchyStripActivity { get; set; } = 0.35f;

        /// <summary>Drop vanilla loose sticks when branchy crown foliage is stripped in autumn.</summary>
        public bool EnableCanopyFallenSticks { get; set; } = true;

        /// <summary>Base chance multiplier for fallen sticks at peak autumn activity.</summary>
        public float CanopyFallenStickChance { get; set; } = 0.42f;

        /// <summary>Scale spring branchy bud attempts by wild tree calendar age.</summary>
        public bool EnableSpringBranchyAgeBoost { get; set; } = true;

        /// <summary>Calendar years at trunk base to reach max spring branchy boost.</summary>
        public float SpringBranchyAgeBoostYearsToMax { get; set; } = 60f;

        /// <summary>Max spring branchy bud multiplier from age (1 = disabled scaling).</summary>
        public float SpringBranchyAgeBoostMax { get; set; } = 1.5f;

        /// <summary>Place branchy leaves on log-grown when crown was stripped bare (pillar repair).</summary>
        public bool FoliageRestoreBareSkeleton { get; set; } = true;

        /// <summary>Legacy alias — use <see cref="MaxFoliageCellsTickedPerTick"/>.</summary>
        public int MaxCanopyUpdateOpsPerTick
        {
            get => MaxFoliageCellsTickedPerTick;
            set => MaxFoliageCellsTickedPerTick = value;
        }

        /// <summary>Multiplier on per-wood defol/bud monthly curves.</summary>
        public float CanopyActivityScale { get; set; } = 1f;

        /// <summary>Minimum °C at trunk base before spring bud attempts (vanilla sapling gate).</summary>
        public float CanopyBudMinTemperature { get; set; } = 5f;

        /// <summary>0 disables latitude modifier; higher = stronger polar slowdown.</summary>
        public float CanopyLatitudeInfluence { get; set; } = 0.35f;

        // --- Canopy ambience particles (v3.5, client-only) — see docs/CANOPY_AMBIENCE.md ---

        /// <summary>Green motes and autumn leaf drift under tall deciduous canopy (client only).</summary>
        public bool EnableCanopyAmbience { get; set; } = true;

        /// <summary>Minimum foliage height above player feet before ambience activates.</summary>
        public int CanopyAmbienceMinHeightBlocks { get; set; } = 2;

        /// <summary>Multiplier on green mote spawn rate.</summary>
        public float CanopyAmbienceMoteRate { get; set; } = 1f;

        /// <summary>Multiplier on autumn leaf drift spawn rate.</summary>
        public float CanopyAmbienceLeafDriftRate { get; set; } = 1f;

        /// <summary>Seconds between canopy density re-samples.</summary>
        public double CanopyAmbienceSampleIntervalSeconds { get; set; } = 2.0;

        /// <summary>Suppress motes and drift while live precipitation is high.</summary>
        public bool CanopyAmbienceSuppressInRain { get; set; } = true;

        /// <summary>Legacy alias — use <see cref="FoliageBudgetMs"/>.</summary>
        public int CanopyBudgetMs
        {
            get => FoliageBudgetMs;
            set => FoliageBudgetMs = value;
        }

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

        /// <summary>Knife/scythe → drygrass; flowers drop in world; tallgrass breaks with no loot.</summary>
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

        /// <summary>
        /// Chance (0..1) that a berry offspring loses one random trait during spread cloning.
        /// Default 0 = no mutations.
        /// </summary>
        public double BerryTraitMutationChance { get; set; } = 0.0;

        // --- Third-party ecology (v3.1) ---

        /// <summary>
        /// When true, blocks may register using JSON attributes (<c>ecologyParticipant</c>, etc.)
        /// without matching hardcoded vanilla <c>game:</c> paths.
        /// </summary>
        public bool EnableThirdPartyParticipants { get; set; } = true;

        /// <summary>Deterministic, synchronous settings for in-process integration tests.</summary>
        public static EcosystemConfig ForIntegrationTests()
        {
            return new EcosystemConfig
            {
                EcosystemEnabled = true,
                UseSeasonalEcology = false,
                RespectLandClaims = false,
                OnlyActivateNearPlayers = false,
                LimitSpreadNearPlayers = false,
                EnablePlayerPriorityRegistration = false,
                EnableBurstRegistrationNearPlayers = false,
                EnableBackgroundRegistrationScan = false,
                EnableBackgroundSpreadSolve = false,
                EnableSeasonalFoliage = false,
                EnableCyclicFloraDiscovery = false,
                EnableFlowerPhenology = false,
                EnableFernPhenology = false,
                EnableSymbiosis = false,
                ApplyWorldgenRainForest = false,
                StaggerReproduceAttempts = false,
                ReproduceChance = 1f,
                MinFitness = 0.1f,
                HarshWildPlants = false,
                EnableFlowerSpreadMaturation = true,
                EnableTallgrassSpreadMaturation = true,
                EnableFlowerSpreadAttemptCooldown = true,
                EnableTwoPhaseSpreadPlacement = true,
                EnableEventDrivenSpread = false,
                MaxRegistrationsPerTick = 9999,
                MaxRegistryAppliesPerTick = 9999,
                MaxRegistryAppliesPerChunkPerTick = 9999,
                MaxReproduceAttemptsPerTick = 9999,
                MaxSpreadCommitsPerTick = 9999,
                MaxChunkColumnsScannedPerTick = 9999,
                TickBudgetMs = 0,
                RegistrationBudgetMs = 0,
                SpreadBudgetMs = 0,
                VerboseLogging = false,
                ReproduceDebug = false,
            };
        }
    }
}
