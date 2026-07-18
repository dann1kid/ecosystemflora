using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using WildFarming.Ecosystem.SpeciesEcology;

// Export-only C# tables: fallback when SpeciesEcologyRegistry is not loaded.
#pragma warning disable CS0618

namespace WildFarming.Ecosystem
{
    public partial class EcosystemSystem
    {
        public static EcosystemSystem Instance { get; private set; }

        ICoreAPI api;
        readonly ReproducerRegistry registry = new ReproducerRegistry();
        readonly RegistrationScanQueue registrationScanQueue = new RegistrationScanQueue();
        readonly PendingRegistrationQueue pendingRegistrations = new PendingRegistrationQueue();
        readonly BackgroundRegistrationPipeline backgroundRegistration = new BackgroundRegistrationPipeline();
        readonly BackgroundSpreadPipeline backgroundSpread = new BackgroundSpreadPipeline();

        /// <summary>Cap inactive-chunk rotations per tick (avoids O(queue) spin when queue is huge).</summary>
        const int MaxChunkScanDequeAttemptsPerTick = 128;

        /// <summary>
        /// Cadence for re-enqueuing each player's immediate chunks into the (fast) background
        /// registration scan. The load-time scan marks a chunk complete and nothing re-runs the fast
        /// pass afterwards, so flora added without a <c>DidPlaceBlock</c> event (worldedit / fill /
        /// other mods) used to wait for the slow cyclic column crawler to round-robin back to it —
        /// minutes at default activation radius. This bounded re-enqueue keeps near-player edits
        /// registering within a couple seconds. Idempotent: already-registered flora is skipped on drain.
        /// </summary>
        const int PlayerVicinityRescanIntervalMs = 2500;
        long nextPlayerVicinityRescanMs;

        long reproduceListenerId;
        long stressListenerId;
        long chunkScanListenerId;
        ChunkColumnLoadedDelegate chunkLoadedHandler;
        ChunkColumnUnloadDelegate chunkUnloadedHandler;
        readonly MaturationQueueSet maturationQueues = new MaturationQueueSet();
        readonly CyclicTreeTrunkScanner cyclicTreeScanner = new CyclicTreeTrunkScanner();
        readonly CyclicFloraScanner cyclicFloraScanner = new CyclicFloraScanner();
        readonly TreeGrowthScheduler treeGrowthScheduler = new TreeGrowthScheduler();
        readonly FerntreeGrowthScheduler ferntreeGrowthScheduler = new FerntreeGrowthScheduler();
        readonly FlowerPhenologyScheduler flowerPhenologyScheduler = new FlowerPhenologyScheduler();
        readonly FernPhenologyScheduler fernPhenologyScheduler = new FernPhenologyScheduler();
        readonly TallgrassPhenologyScheduler tallgrassPhenologyScheduler = new TallgrassPhenologyScheduler();
        readonly PlantSnowCoverScheduler plantSnowCoverScheduler = new PlantSnowCoverScheduler();
        readonly StumpDecayScheduler stumpDecayScheduler = new StumpDecayScheduler();
        readonly TreeCalendarAgeStore treeCalendarAgeStore = new TreeCalendarAgeStore();
        readonly FlowerPhenologyLifeStore flowerPhenologyLifeStore = new FlowerPhenologyLifeStore();

        internal FlowerPhenologyLifeStore FlowerPhenologyLife => flowerPhenologyLifeStore;
        readonly ColumnTrafficStore columnTrafficStore = new ColumnTrafficStore();
        /// <summary>Dedup vine support checks while draining pending chunk-load registrations.</summary>
        readonly HashSet<(int x, int z, string facing, bool tropical)> vineLoadSupportChecked =
            new HashSet<(int x, int z, string facing, bool tropical)>();
        FootTrafficService footTraffic;
        readonly FoliageCellScheduler foliageCells = new FoliageCellScheduler();
        readonly FoliagePlayerVacancySuppressor foliagePlayerVacancies = new FoliagePlayerVacancySuppressor();
        bool calendarDebugLogged;
        bool deferredTreeBootstrapDone;
        int foliageBootstrapPasses;
        bool foliageStartupLogged;
        internal FoliageCellScheduler FoliageCells => foliageCells;
        internal StumpDecayScheduler StumpDecay => stumpDecayScheduler;
        internal FoliagePlayerVacancySuppressor FoliagePlayerVacancies => foliagePlayerVacancies;
        internal ColumnTrafficStore ColumnTraffic => columnTrafficStore;
        internal FloraContextSampler FloraContext { get; private set; }
        internal NicheSampler Niche { get; private set; }
        internal EcologySpacingIndex SpacingIndex { get; private set; }
        internal EnvironmentalColumnCache ColumnCache { get; private set; }
        internal EcologyColumnState EcologyColumns { get; private set; }
        readonly List<Vec2i> activeChunkScratch = new List<Vec2i>();
        SpreadCooldownService spreadCooldown;
        readonly Dictionary<BlockPos, MatSpreadCollectMode> spreadMatModeScratch = new Dictionary<BlockPos, MatSpreadCollectMode>();
        readonly Stopwatch tickBudgetWatch = new Stopwatch();
        readonly PendingSpreadQueue pendingSpreadQueue = new PendingSpreadQueue();
        internal PendingSpreadQueue PendingSpreads => pendingSpreadQueue;
        int lastSeasonWakeMonth = -1;

        BlockBrokenDelegate didBreakBlockHandler;
        BlockPlacedDelegate didPlaceBlockHandler;
        BlockUsedDelegate didUseBlockHandler;
        PlayerDelegate playerJoinHandler;

        public void InitPre(ICoreAPI api)
        {
            if (api.Side != EnumAppSide.Server) return;

            this.api = api;
            Instance = this;

            EcosystemConfig.TryLoadFromDisk(api, createDefaultIfMissing: true);
        }

        public void Init(ICoreAPI api)
        {
            if (api.Side != EnumAppSide.Server) return;

            this.api = api;
            Instance = this;

            if (!EcosystemConfig.Loaded.EcosystemEnabled) return;

            FloraContext = new FloraContextSampler();
            Niche = new NicheSampler();
            SpacingIndex = new EcologySpacingIndex();
            ColumnCache = new EnvironmentalColumnCache();
            EcologyColumns = new EcologyColumnState();
            spreadCooldown = new SpreadCooldownService(api, registry);

            EcosystemConfig tickCfg = EcosystemConfig.Loaded;
            if (tickCfg.EnableBackgroundRegistrationScan)
            {
                backgroundRegistration.Start(api.World.Blocks, tickCfg.RegistrationWorkerCount);
            }

            if (tickCfg.EnableBackgroundSpreadSolve)
            {
                backgroundSpread.Start(api.World.Blocks, tickCfg.SpreadWorkerCount);
            }

            int reproduceInterval = tickCfg.ReproduceTickIntervalMs > 0
                ? tickCfg.ReproduceTickIntervalMs : 2000;
            int stressInterval = tickCfg.StressTickIntervalMs > 0
                ? tickCfg.StressTickIntervalMs : 5500;
            int chunkScanInterval = tickCfg.ChunkScanTickIntervalMs > 0
                ? tickCfg.ChunkScanTickIntervalMs : 2300;

            // Timelapse leftover in custom JSON (25ms) freezes the time slider when calendar jumps.
            // Explicit BalancePreset=timelapse keeps the fast cadence.
            bool timelapsePreset = !string.IsNullOrEmpty(tickCfg.BalancePreset)
                && tickCfg.BalancePreset.Equals(
                    EcosystemBalancePresets.Timelapse,
                    System.StringComparison.OrdinalIgnoreCase);

            if (!timelapsePreset && reproduceInterval < 100)
            {
                api.Logger.Warning(
                    "[ecosystemflora] ReproduceTickIntervalMs={0} is timelapse-tier under preset '{1}'; clamping to 2000 for play.",
                    reproduceInterval,
                    tickCfg.BalancePreset);
                reproduceInterval = 2000;
            }

            if (!timelapsePreset && chunkScanInterval < 100)
            {
                api.Logger.Warning(
                    "[ecosystemflora] ChunkScanTickIntervalMs={0} is timelapse-tier under preset '{1}'; clamping to 1000 for play.",
                    chunkScanInterval,
                    tickCfg.BalancePreset);
                chunkScanInterval = 1000;
            }

            reproduceListenerId = api.Event.RegisterGameTickListener(OnReproduceTick, reproduceInterval);
            stressListenerId = api.Event.RegisterGameTickListener(OnStressTick, stressInterval);
            chunkScanListenerId = api.Event.RegisterGameTickListener(OnChunkScanTick, chunkScanInterval);

            footTraffic = new FootTrafficService(columnTrafficStore);

            foliageCells.RequestEcologyScan = coord => EnqueueChunkScan(coord);

            if (api is ICoreServerAPI sapi)
            {
                chunkLoadedHandler = OnChunkColumnLoaded;
                chunkUnloadedHandler = OnChunkColumnUnloaded;
                sapi.Event.ChunkColumnLoaded += chunkLoadedHandler;
                sapi.Event.ChunkColumnUnloaded += chunkUnloadedHandler;

                didBreakBlockHandler = OnDidBreakBlock;
                sapi.Event.DidBreakBlock += didBreakBlockHandler;

                didPlaceBlockHandler = OnDidPlaceBlock;
                sapi.Event.DidPlaceBlock += didPlaceBlockHandler;

                didUseBlockHandler = OnDidUseBlock;
                sapi.Event.DidUseBlock += didUseBlockHandler;

                playerJoinHandler = OnPlayerJoin;
                sapi.Event.PlayerJoin += playerJoinHandler;

                treeCalendarAgeStore.Bind(sapi, registry);
                flowerPhenologyLifeStore.Bind(sapi);
                stumpDecayScheduler.Bind(sapi);
                columnTrafficStore.Bind(sapi);
                footTraffic.Bind(sapi, this);
            }

            SpeciesEcologyLegacyAccess.LogMissingContractSpecies(api);
        }

        void OnPlayerJoin(IPlayer player)
        {
            deferredTreeBootstrapDone = false;
            foliageBootstrapPasses = 0;
            foliageStartupLogged = false;
            FoliageDiagnostics.ResetOnPlayerJoin();
        }

        void TryDeferredTreeBootstrap()
        {
            if (deferredTreeBootstrapDone || api == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            int bootstrapRadius = cfg.EnablePlayerPriorityRegistration
                ? cfg.PlayerRegistrationPriorityRadiusBlocks
                : cfg.PlayerActivationRadiusBlocks;
            HashSet<long> chunks = PlayerProximity.BuildActivePlayerChunks(api, bootstrapRadius);
            if (chunks.Count == 0) return;

            deferredTreeBootstrapDone = true;
            BootstrapTreeSpreadForLoadedChunks();
            BootstrapFoliageIndexForLoadedChunks();
            EnqueueLoadedChunksNearPlayersForScan(chunks);
        }

        void TryDeferredFoliageBootstrap()
        {
            if (api == null || !EcosystemConfig.Loaded.EnableSeasonalFoliage) return;
            if (foliageStartupLogged && foliageCells.PendingSyncChunks == 0
                && foliageCells.TrackedChunkCount > 0) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            HashSet<long> chunks = PlayerProximity.BuildActivePlayerChunks(api, cfg.PlayerActivationRadiusBlocks);
            if (chunks.Count == 0) return;

            IBlockAccessor acc = api.World.BlockAccessor;
            if (foliageCells.Index.TotalCells == 0
                && foliageCells.PendingSyncChunks == 0
                && foliageBootstrapPasses < 20)
            {
                foliageBootstrapPasses++;
                foreach (long key in chunks)
                {
                    int cx = (int)(key >> 32);
                    int cz = (int)(key & 0xFFFFFFFF);
                    if (acc.GetMapChunk(cx, cz) == null) continue;
                    foliageCells.ScanChunkImmediate(api, new Vec2i(cx, cz));
                }
            }

            if (!foliageStartupLogged && (foliageCells.GetDiagnosticsIndexedCount() > 0
                || foliageCells.PendingSyncChunks > 0
                || foliageBootstrapPasses >= 3))
            {
                FoliageDiagnostics.LogStartupSummary(api, foliageCells);
                foliageStartupLogged = true;
            }
        }

        void EnqueueLoadedChunksNearPlayersForScan(HashSet<long> chunkKeys)
        {
            if (api?.World?.BlockAccessor == null || chunkKeys == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            IBlockAccessor acc = api.World.BlockAccessor;
            foreach (long key in chunkKeys)
            {
                int cx = (int)(key >> 32);
                int cz = (int)(key & 0xFFFFFFFF);
                if (acc.GetMapChunk(cx, cz) == null) continue;

                var coord = new Vec2i(cx, cz);
                EnqueueChunkScan(coord, highPriority: cfg.EnablePlayerPriorityRegistration);
                foliageCells.ScheduleChunkSync(api, coord);
            }
        }

        /// <summary>
        /// Throttled re-enqueue of the chunks immediately around each online player into the fast
        /// background registration scan. Catches flora that appeared after a chunk's load-time scan
        /// completed but bypassed <c>OnDidPlaceBlock</c> (bulk/worldedit placement). Scoped to the
        /// player-priority radius (a 3×3-ish chunk window at defaults) and gated by
        /// <see cref="PlayerVicinityRescanIntervalMs"/> so it cannot outrun scan completion. The fresh
        /// re-enqueue is deduped by <see cref="RegistrationScanQueue"/> until the prior scan finishes.
        /// </summary>
        void RescanPlayerVicinity(EcosystemConfig cfg, IBlockAccessor acc)
        {
            if (acc == null || !cfg.EnableCyclicFloraDiscovery) return;
            if (!(api is ICoreServerAPI sapi)) return;

            long nowMs = api.World.ElapsedMilliseconds;
            if (nowMs < nextPlayerVicinityRescanMs) return;
            nextPlayerVicinityRescanMs = nowMs + PlayerVicinityRescanIntervalMs;

            int cs = GlobalConstants.ChunkSize;
            int chunkRadius = cfg.PlayerRegistrationPriorityRadiusBlocks / cs + 1;
            bool highPriority = cfg.EnablePlayerPriorityRegistration;

            foreach (IPlayer player in sapi.World.AllOnlinePlayers)
            {
                var ppos = player?.Entity?.Pos;
                if (ppos == null) continue;

                int pcx = (int)ppos.X / cs;
                int pcz = (int)ppos.Z / cs;
                for (int dx = -chunkRadius; dx <= chunkRadius; dx++)
                {
                    for (int dz = -chunkRadius; dz <= chunkRadius; dz++)
                    {
                        int cx = pcx + dx;
                        int cz = pcz + dz;
                        if (acc.GetMapChunk(cx, cz) == null) continue;
                        EnqueueChunkScan(new Vec2i(cx, cz), highPriority: highPriority);
                    }
                }
            }
        }

        void BootstrapTreeSpreadForLoadedChunks()
        {
            if (api == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            HashSet<long> chunks = PlayerProximity.BuildActivePlayerChunks(api, cfg.PlayerActivationRadiusBlocks);
            IBlockAccessor acc = api.World.BlockAccessor;
            int left = cfg.EffectiveMaxRegistrationsPerTick() * 4;
            foreach (long key in chunks)
            {
                if (left <= 0) break;
                int cx = (int)(key >> 32);
                int cz = (int)(key & 0xFFFFFFFF);
                if (acc.GetMapChunk(cx, cz) == null) continue;

                TreeTrunkDiscovery.ScanChunk(
                    acc,
                    new Vec2i(cx, cz),
                    (basePos, wood) => TryRegisterDiscoveredTree(acc, basePos, ref left),
                    left);
            }
        }

        void BootstrapFoliageIndexForLoadedChunks()
        {
            if (api == null || !EcosystemConfig.Loaded.EnableSeasonalFoliage) return;

            HashSet<long> chunks = PlayerProximity.BuildActivePlayerChunks(
                api, EcosystemConfig.Loaded.PlayerActivationRadiusBlocks);
            IBlockAccessor acc = api.World.BlockAccessor;
            foreach (long key in chunks)
            {
                int cx = (int)(key >> 32);
                int cz = (int)(key & 0xFFFFFFFF);
                if (acc.GetMapChunk(cx, cz) != null)
                {
                    foliageCells.ScheduleChunkSync(api, new Vec2i(cx, cz));
                }
            }
        }

        void TryLogCalendarDebugOnce()
        {
            if (calendarDebugLogged || api == null || !EcosystemConfig.Loaded.ReproduceDebug) return;

            IGameCalendar cal = api.World?.Calendar;
            if (cal == null) return;

            calendarDebugLogged = true;
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            double attemptsPerYear = cfg.ReproduceAttemptsPerYear;
            if (attemptsPerYear <= 0)
            {
                attemptsPerYear = (cal.DaysPerYear * cal.HoursPerDay)
                    / System.Math.Max(0.01, cfg.ReproduceIntervalHours);
            }

            api.Logger.Notification(
                "[ecosystemflora] Calendar: {0} days/year, {1} h/day, speed {2:0.###}×; spread base {3} attempts/year (calendar-scaled={4})",
                cal.DaysPerYear,
                cal.HoursPerDay,
                CalendarSpeedHelper.GetSpeedMultiplier(cal),
                attemptsPerYear,
                cfg.UseCalendarScaledSpread);
        }

        public void Dispose()
        {
            if (api is ICoreServerAPI sapi)
            {
                treeCalendarAgeStore.Unbind(sapi);
                treeCalendarAgeStore.Clear();
                flowerPhenologyLifeStore.Unbind(sapi);
                flowerPhenologyLifeStore.Clear();
                stumpDecayScheduler.Unbind(sapi);
                columnTrafficStore.Unbind(sapi);
                columnTrafficStore.Clear();
                footTraffic?.Unbind();
                footTraffic?.Clear();

                if (chunkLoadedHandler != null) sapi.Event.ChunkColumnLoaded -= chunkLoadedHandler;
                if (chunkUnloadedHandler != null) sapi.Event.ChunkColumnUnloaded -= chunkUnloadedHandler;
                if (didBreakBlockHandler != null) sapi.Event.DidBreakBlock -= didBreakBlockHandler;
                if (didPlaceBlockHandler != null) sapi.Event.DidPlaceBlock -= didPlaceBlockHandler;
                if (didUseBlockHandler != null) sapi.Event.DidUseBlock -= didUseBlockHandler;
                if (playerJoinHandler != null) sapi.Event.PlayerJoin -= playerJoinHandler;
            }

            if (api != null)
            {
                if (reproduceListenerId != 0) api.Event.UnregisterGameTickListener(reproduceListenerId);
                if (stressListenerId != 0) api.Event.UnregisterGameTickListener(stressListenerId);
                if (chunkScanListenerId != 0) api.Event.UnregisterGameTickListener(chunkScanListenerId);
            }

            registrationScanQueue.Clear();
            backgroundRegistration.Dispose();
            backgroundSpread.Dispose();
            reproduceListenerId = 0;
            stressListenerId = 0;
            chunkScanListenerId = 0;
            chunkLoadedHandler = null;
            chunkUnloadedHandler = null;
            didBreakBlockHandler = null;
            didPlaceBlockHandler = null;
            didUseBlockHandler = null;
            calendarDebugLogged = false;
            deferredTreeBootstrapDone = false;
            playerJoinHandler = null;
            FloraContext?.Clear();
            FloraContext = null;
            Niche?.Clear();
            Niche = null;
            SpacingIndex?.Clear();
            SpacingIndex = null;
            ColumnCache?.Clear();
            ColumnCache = null;
            activeChunkScratch.Clear();
            foliageCells.Clear();
            foliagePlayerVacancies.Clear();
            treeGrowthScheduler.Clear();
            ferntreeGrowthScheduler.Clear();
            cyclicTreeScanner.Clear();
            cyclicFloraScanner.Clear();
            Instance = null;
            api = null;
        }

        public EnvironmentalContext Sample(BlockPos plantPos)
        {
            return EnvironmentalContext.SampleForSurvival(api, plantPos, null, ColumnCache);
        }

        internal void InvalidateEnvironmentAround(BlockPos pos)
        {
            if (pos == null) return;
            ColumnCache?.InvalidateAround(pos, 1);
            Niche?.InvalidateAround(pos, 2);
            FloraContext?.InvalidateAround(pos, 2);
            EcologyColumns?.InvalidateAround(pos, 2);
        }

        public void WakeEcologyAround(BlockPos pos)
        {
            if (!EcosystemConfig.Loaded.EcosystemEnabled || api == null || pos == null) return;
            if (!EcosystemConfig.Loaded.EnableEventDrivenSpread) return;

            registry.WakeAround(
                pos,
                EcologyWake.ResolveRadiusBlocks(EcosystemConfig.Loaded),
                api.World.Calendar.TotalHours,
                EcosystemConfig.Loaded,
                api.World.Calendar);
        }

        public bool CanSurviveAt(BlockPos plantPos, PlantRequirements requirements)
        {
            return SuitabilityEvaluator.MeetsSurvivalRequirements(
                requirements, Sample(plantPos), EcosystemConfig.Loaded.HarshWildPlants);
        }

        public string DescribeSurvivalAt(BlockPos plantPos, PlantRequirements requirements)
        {
            return SuitabilityEvaluator.DescribeSurvivalFailure(
                requirements, Sample(plantPos), EcosystemConfig.Loaded.HarshWildPlants);
        }

        public bool RegistryContains(BlockPos pos)
        {
            if (pos == null) return false;
            if (registry.Contains(pos)) return true;
            if (TryResolveTreeRegistryPos(pos, out BlockPos basePos) && registry.Contains(basePos)) return true;
            return TryResolveFerntreeRegistryPos(pos, out BlockPos ferntreeBase) && registry.Contains(ferntreeBase);
        }

        public bool TryGetReproducer(BlockPos pos, out ReproducerEntry entry)
        {
            entry = null;
            if (pos == null) return false;

            BlockPos lookup = pos;
            if (TryResolveTreeRegistryPos(pos, out BlockPos basePos))
            {
                lookup = basePos;
            }
            else if (TryResolveFerntreeRegistryPos(pos, out BlockPos ferntreeBase))
            {
                lookup = ferntreeBase;
            }

            return registry.TryGetEntry(lookup, out entry);
        }

        public bool TryGetFlowerMaturationHoursLeft(BlockPos pos, out double hoursLeft)
        {
            hoursLeft = 0;
            if (api?.World?.Calendar == null || pos == null) return false;
            return maturationQueues.TryGetFlowerHoursLeft(
                pos,
                api.World.Calendar.TotalHours,
                out hoursLeft);
        }

        public bool TryGetFernMaturationHoursLeft(BlockPos pos, out double hoursLeft)
        {
            hoursLeft = 0;
            if (api?.World?.Calendar == null || pos == null) return false;
            return maturationQueues.TryGetFernHoursLeft(
                pos,
                api.World.Calendar.TotalHours,
                out hoursLeft);
        }

        public bool TryGetShoreSedgeMaturationHoursLeft(BlockPos pos, out double hoursLeft)
        {
            hoursLeft = 0;
            if (api?.World?.Calendar == null || pos == null) return false;
            return maturationQueues.TryGetShoreSedgeHoursLeft(
                pos,
                api.World.Calendar.TotalHours,
                out hoursLeft);
        }

        internal bool TryGetTallgrassPromotionState(
            BlockPos pos,
            out int targetStageIndex,
            out double nextAdvanceAtHours)
        {
            targetStageIndex = -1;
            nextAdvanceAtHours = 0;
            return maturationQueues.TryGetTallgrassPromotionState(
                pos,
                out targetStageIndex,
                out nextAdvanceAtHours);
        }

        internal void NotifySpreadSolveNoWinners(BlockPos origin, PlantRequirements requirements)
        {
            if (origin == null || requirements == null) return;
            spreadCooldown.ApplyPostSpreadAttemptOnce(origin, requirements);

            if (!registry.TryGetEntry(origin, out ReproducerEntry entry)) return;

            spreadMatModeScratch.TryGetValue(origin, out MatSpreadCollectMode matMode);
            SpreadAttemptInspect.Record(
                api,
                entry,
                matMode,
                placed: false,
                failureReason: "No qualifying cells");
        }

        public void RelocateVineTip(BlockPos oldPos, BlockPos newPos)
        {
            if (oldPos == null || newPos == null || oldPos.Equals(newPos)) return;
            if (!registry.TryGetEntry(oldPos, out ReproducerEntry entry)) return;

            var replacement = new ReproducerEntry(
                newPos.Copy(),
                entry.JuvenileBlockCode.Clone(),
                entry.MatureBlockCode,
                entry.Requirements,
                entry.NextAttemptHours)
            {
                EstablishedAtHours = entry.EstablishedAtHours,
                NextStressCheckAt = entry.NextStressCheckAt,
                FailedSurvivalChecks = entry.FailedSurvivalChecks,
                TramplingExposure = entry.TramplingExposure,
            };

            registry.Remove(oldPos);
            registry.Add(replacement);
        }

        bool TryResolveFerntreeRegistryPos(BlockPos pos, out BlockPos basePos)
        {
            basePos = pos;
            if (api?.World?.BlockAccessor == null) return false;
            if (!EcosystemConfig.Loaded.EnableFerntreeEcology) return false;

            IBlockAccessor acc = api.World.BlockAccessor;
            Block block = acc.GetBlock(pos);
            if (!PlantCodeHelper.IsFerntreeEcologyBlock(block)) return false;

            basePos = FerntreeStructure.GetTrunkBase(acc, pos);
            return true;
        }

        bool TryResolveTreeRegistryPos(BlockPos pos, out BlockPos basePos)
        {
            basePos = pos;
            if (api?.World?.BlockAccessor == null) return false;

            IBlockAccessor acc = api.World.BlockAccessor;
            Block block = acc.GetBlock(pos);
            if (!PlantCodeHelper.IsTreeLogGrownBlock(block)) return false;

            basePos = PlantCodeHelper.GetTreeTrunkBase(acc, pos);
            return true;
        }

        internal ICoreAPI ServerApi => api;

        internal void CompleteTreeSenescenceRemoval(TreeSenescence.PendingRemoval removal)
        {
            if (api == null || removal.TrunkBase == null) return;

            BlockPos trunkBase = removal.TrunkBase;
            registry.Remove(trunkBase);
            SpacingIndex?.Remove(trunkBase);
            treeCalendarAgeStore.Remove(trunkBase);
            FloraContext?.InvalidateAround(trunkBase, 4);
            InvalidateEnvironmentAround(trunkBase);

            if (EcosystemConfig.Loaded.UseSoilSuccession && !string.IsNullOrEmpty(removal.Wood))
            {
                SoilSuccessionApplier.Apply(api, trunkBase, removal.Wood, SoilSuccessionEvent.Death);
            }

            if (EcosystemConfig.Loaded.VerboseLogging
                && EcosystemConfig.Loaded.ReproduceDebug
                && removal.BlocksRemoved > 0)
            {
                api.Logger.Notification(
                    "[ecosystemflora] Senescence registry cleared for {0} at {1} ({2} blocks)",
                    removal.Wood,
                    trunkBase,
                    removal.BlocksRemoved);
            }
        }

        public void RemoveEcologyPlant(BlockPos pos, bool cascadeSymbiosis, string reason,
            SoilSuccessionEvent soilEvent = SoilSuccessionEvent.Death)
        {
            if (api == null || pos == null) return;
            if (!LandClaimGuard.AllowsEcologyChange(api, pos)) return;

            IBlockAccessor acc = api.World.BlockAccessor;
            Block block = acc.GetBlock(pos);
            if (block.Id == 0) return;

            if (cascadeSymbiosis && EcosystemConfig.Loaded.EnableSymbiosis)
            {
                FloraSymbiosis.NotifyHostRemoved(api, pos, block);
            }

            string species = PlantCodeHelper.ResolveEcologySpecies(block);
            if (!string.IsNullOrEmpty(species))
            {
                SoilSuccessionApplier.Apply(api, pos, species, soilEvent);
            }

            registry.Remove(pos);
            SpacingIndex?.Remove(pos);
            flowerPhenologyLifeStore.Remove(pos);
            acc.SetBlock(0, pos);
            acc.MarkBlockDirty(pos);
            FloraContext?.InvalidateAround(pos, 2);
            InvalidateEnvironmentAround(pos);

            if (EcosystemConfig.Loaded.EnableWildVineEcology)
            {
                OnWildVineHostChanged(pos);
            }

            if (EcosystemConfig.Loaded.VerboseLogging && EcosystemConfig.Loaded.ReproduceDebug && !string.IsNullOrEmpty(reason))
            {
                api.Logger.Notification(
                    "[ecosystemflora] Removed {0} at {1} ({2})",
                    PlantCodeHelper.ResolveEcologySpecies(block) ?? block.Code?.Path,
                    pos,
                    reason);
            }
        }

        public bool RegisterReproducer(
            BlockPos origin,
            IEcosystemParticipant participant,
            bool spawnBurst = false,
            bool playerPlaced = false,
            bool ignorePlayerProximity = false)
        {
            if (participant == null || api == null) return false;

            BlockPos anchor = PlantCodeHelper.GetReproduceAnchor(
                api.World.BlockAccessor, origin, participant.BlockCode);

            return RegisterReproducer(
                anchor,
                participant.SpreadBlockCode,
                participant.MatureBlockCode,
                participant.Requirements,
                spawnBurst,
                playerPlaced,
                flowerSpreadEstablished: false,
                ignorePlayerProximity);
        }

        public bool RegisterReproducer(
            BlockPos origin,
            AssetLocation spreadBlockCode,
            AssetLocation matureBlockCode,
            PlantRequirements requirements,
            bool spawnBurst = false,
            bool playerPlaced = false,
            bool flowerSpreadEstablished = false,
            bool ignorePlayerProximity = false)
        {
            if (api == null || api.Side != EnumAppSide.Server) return false;
            if (!EcosystemConfig.Loaded.EcosystemEnabled) return false;
            if (spreadBlockCode == null || matureBlockCode == null || requirements == null) return false;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (requirements.Habitat == EcologyHabitat.Ferntree && !cfg.EnableFerntreeEcology) return false;
            if (requirements.Habitat == EcologyHabitat.WildVine && !cfg.EnableWildVineEcology) return false;
            if (!ignorePlayerProximity
                && cfg.OnlyActivateNearPlayers
                && !PlayerProximity.IsNearAnyPlayer(api, origin, cfg.PlayerActivationRadiusBlocks))
            {
                return false;
            }

            try
            {
                Block matureBlock = api.World.BlockAccessor.GetBlock(origin);
                if (!RegistrationParticipantResolver.TryFromLiveBlock(
                        api,
                        origin,
                        matureBlock,
                        ref requirements,
                        ref spreadBlockCode,
                        ref matureBlockCode))
                {
                    if (cfg.ReproduceDebug && requirements.Habitat == EcologyHabitat.TerrestrialTree)
                    {
                        api.Logger.Warning(
                            "[ecosystemflora] Tree not registrable at {0}: block={1}",
                            origin,
                            matureBlock?.Code);
                    }
                    else if (cfg.ReproduceDebug && EcologyFernSpecies.IsKnown(requirements?.Species))
                    {
                        api.Logger.Warning(
                            "[ecosystemflora] Fern not registrable at {0}: live={1} spread={2}",
                            origin,
                            matureBlock?.Code,
                            spreadBlockCode);
                    }

                    return false;
                }

                matureBlock = api.World.BlockAccessor.GetBlock(origin);
                Block spreadBlock = EcologySpreadBlockResolver.Resolve(api, spreadBlockCode, origin, matureBlock);
                if (spreadBlock == null || spreadBlock.Id == 0)
                {
                    if (cfg.ReproduceDebug && requirements.Habitat == EcologyHabitat.TerrestrialTree)
                    {
                        api.Logger.Warning(
                            "[ecosystemflora] Tree spread block missing: {0} (origin {1})",
                            spreadBlockCode,
                            origin);
                    }
                    else if (cfg.ReproduceDebug && PlantCodeHelper.IsThirdPartyEcologyBlock(matureBlock))
                    {
                        api.Logger.Warning(
                            "[ecosystemflora] Third-party spread block missing: {0} (origin {1}, mature {2})",
                            spreadBlockCode,
                            origin,
                            matureBlock?.Code);
                    }
                    else if (cfg.ReproduceDebug && EcologyFernSpecies.IsKnown(requirements.Species))
                    {
                        api.Logger.Warning(
                            "[ecosystemflora] Fern spread block missing: {0} (origin {1}, live {2})",
                            spreadBlockCode,
                            origin,
                            matureBlock?.Code);
                    }

                    return false;
                }

                spreadBlockCode = spreadBlock.Code.Clone();

                if (requirements.Species == "tallgrass"
                    && TallgrassSpreadMaturation.UsesMaturation(cfg)
                    && !TallgrassSpreadMaturation.CanReproduceFrom(matureBlock, api, origin))
                {
                    if (!playerPlaced && TallgrassEstablishment.ShouldQueueAfterPlacement(api, origin, matureBlock))
                    {
                        maturationQueues.AddTallgrassPromotion(api, origin);
                    }

                    return false;
                }

                double now = api.World.Calendar.TotalHours;
                double spreadInterval = SpeciesSpread.EffectiveIntervalHours(api, origin, cfg, requirements);
                double nextAttempt = now;
                if (cfg.StaggerReproduceAttempts)
                {
                    nextAttempt = now + api.World.Rand.NextDouble() * spreadInterval;
                }
                else if (!playerPlaced && spreadInterval > 0)
                {
                    // Spread offspring wait one interval before their first attempt so expanding
                    // frontiers do not monopolize ticks over older nearby colonies.
                    nextAttempt = now + spreadInterval;
                }

                double stressDelay = cfg.StressRecheckHours > 0 ? cfg.StressRecheckHours : 18;
                if (cfg.StaggerReproduceAttempts)
                {
                    stressDelay = api.World.Rand.NextDouble() * stressDelay;
                }

                var entry = new ReproducerEntry(
                    origin.Copy(),
                    spreadBlockCode.Clone(),
                    matureBlockCode,
                    requirements,
                    nextAttempt)
                {
                    EstablishedAtHours = now,
                    NextStressCheckAt = now + stressDelay,
                };

                if (!playerPlaced && spreadInterval > 0)
                {
                    SpreadWakeThrottle.ApplyCalendarCadenceFloor(entry, now, spreadInterval);
                }

                registry.Add(entry);
                SpacingIndex?.AddOrUpdate(api.World.BlockAccessor, origin);
                if (!playerPlaced)
                    SoilSuccessionApplier.Apply(api, origin, requirements.Species, SoilSuccessionEvent.Spread);

                FlowerPhenology.InitializeOnRegister(api, entry, cfg, flowerSpreadEstablished);
                FernPhenology.InitializeOnRegister(api, entry, cfg);
                TallgrassPhenology.InitializeOnRegister(api, entry, cfg);

                if (requirements.Habitat == EcologyHabitat.TerrestrialTree
                    && PlantCodeHelper.IsTreeLogGrownBlock(matureBlock))
                {
                    string wood = PlantCodeHelper.GetTreeWood(matureBlock);
                    int gameYear = CanopyEcology.GameYear(api.World.Calendar);
                    if (treeCalendarAgeStore.TryRestore(entry, origin, wood)
                        && !TreeRegistrationAge.ShouldRejectRestoredAge(
                            api.World.BlockAccessor, origin, wood, entry))
                    {
                        // Clamp impossible "years since world start" lags from defaulted saves.
                        entry.LastTreeGrowthYear = TreeCalendarCatchUp.NormalizeLastGrowthYear(
                            entry.LastTreeGrowthYear,
                            gameYear,
                            entry.TreeAgeYears,
                            cfg.MaxTreeGrowthCatchUpYearsPerTick);
                    }
                    else
                    {
                        // Calendar age starts at registration — never invent lifespan from crown size.
                        entry.TreeAgeYears = 0;
                        entry.TreeSenescencePhase = TreeSenescencePhase.None;
                        // One year behind so the next growth tick can run (including catch-up after time skip).
                        entry.LastTreeGrowthYear = gameYear - 1;
                    }

                    treeCalendarAgeStore.Capture(entry, wood);
                    foliageCells.OnBlockAdded(origin);
                }
                else if (requirements.Habitat == EcologyHabitat.Ferntree
                         && PlantCodeHelper.IsFerntreeTrunkBlock(matureBlock)
                         && cfg.EnableFerntreeEcology)
                {
                    if (!treeCalendarAgeStore.TryRestore(entry, origin, EcologyFerntreeSpecies.Ferntree))
                    {
                        int gameYear = CanopyEcology.GameYear(api.World.Calendar);
                        entry.TreeAgeYears = 0;
                        entry.LastTreeGrowthYear = gameYear - 1;
                    }

                    treeCalendarAgeStore.Capture(entry, EcologyFerntreeSpecies.Ferntree);
                }


                if (cfg.VerboseLogging && cfg.ReproduceDebug)
                {
                    api.Logger.Notification(
                        "[ecosystemflora] Registered {0} at {1} spreadRate={2:0.##} interval={3:0.#}h chance={4:0.##} (registry {5})",
                        spreadBlockCode,
                        origin,
                        requirements.SpreadRate,
                        SpeciesSpread.EffectiveIntervalHours(api, origin, cfg, requirements),
                        SpeciesSpread.EffectiveChance(api, origin, cfg, requirements),
                        registry.Count);
                }

                if (spawnBurst)
                {
                    TrySpawnOffspring(entry, skipChanceRoll: true, maxSpawns: 3);
                }

                return true;
            }
            catch (System.Exception ex)
            {
                api.Logger.Error("[ecosystemflora] RegisterReproducer failed at {0}: {1}", origin, ex);
                return false;
            }
        }

        public void RegisterMyceliumAnchor(
            BlockPos groundPos,
            AssetLocation mushroomCode,
            PlantRequirements requirements)
        {
            if (api == null || api.Side != EnumAppSide.Server) return;
            if (!EcosystemConfig.Loaded.EcosystemEnabled) return;
            if (!EcosystemConfig.Loaded.EnableMyceliumEcology) return;
            if (groundPos == null || mushroomCode == null || requirements == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (cfg.OnlyActivateNearPlayers
                && !PlayerProximity.IsNearAnyPlayer(api, groundPos, cfg.PlayerActivationRadiusBlocks))
            {
                return;
            }

            IBlockAccessor acc = api.World.BlockAccessor;
            if (!WildSoilGroundRules.HasActiveMycelium(acc, groundPos)) return;

            try
            {
                requirements.SpreadMode = SpreadMode.MyceliumNetwork;
                requirements.SpreadRate = cfg.MyceliumSpreadRate > 0f ? cfg.MyceliumSpreadRate : 0.12f;

                double now = api.World.Calendar.TotalHours;
                double stressDelay = cfg.StressRecheckHours > 0 ? cfg.StressRecheckHours : 18;
                if (cfg.StaggerReproduceAttempts)
                {
                    stressDelay = api.World.Rand.NextDouble() * stressDelay;
                }

                double nextAttempt = now;
                if (cfg.StaggerReproduceAttempts)
                {
                    double interval = MyceliumSpreadTiming.EffectiveIntervalHours(api, cfg, requirements);
                    nextAttempt = now + api.World.Rand.NextDouble() * interval;
                }

                var entry = new ReproducerEntry(
                    groundPos.Copy(),
                    mushroomCode.Clone(),
                    mushroomCode.Clone(),
                    requirements,
                    nextAttemptHours: nextAttempt)
                {
                    EstablishedAtHours = now,
                    NextStressCheckAt = now + stressDelay,
                };

                registry.Add(entry);

                if (cfg.VerboseLogging && cfg.ReproduceDebug)
                {
                    api.Logger.Notification(
                        "[ecosystemflora] Registered mycelium {0} at {1} (registry {2})",
                        mushroomCode,
                        groundPos,
                        registry.Count);
                }
            }
            catch (System.Exception ex)
            {
                api.Logger.Error("[ecosystemflora] RegisterMyceliumAnchor failed at {0}: {1}", groundPos, ex);
            }
        }

        public void RemoveMyceliumAnchor(BlockPos groundPos, string reason)
        {
            if (api == null || groundPos == null) return;
            if (!LandClaimGuard.AllowsEcologyChange(api, groundPos)) return;

            IBlockAccessor acc = api.World.BlockAccessor;
            if (WildSoilGroundRules.HasActiveMycelium(acc, groundPos))
            {
                acc.RemoveBlockEntity(groundPos);
            }

            registry.Remove(groundPos);
            InvalidateEnvironmentAround(groundPos);

            if (EcosystemConfig.Loaded.VerboseLogging && EcosystemConfig.Loaded.ReproduceDebug)
            {
                api.Logger.Notification(
                    "[ecosystemflora] Mycelium anchor removed at {0} ({1})",
                    groundPos,
                    reason ?? "removed");
            }
        }

        void OnChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks)
        {
            if (api?.World?.BlockAccessor == null) return;

            LegacyBlockEntityMigration.ScheduleStripColumn(api, chunkCoord);
            LegacyPhaseBlockMigration.ScheduleRemapColumn(api, chunkCoord);
            if (!EcosystemConfig.Loaded.EcosystemEnabled) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            int cs = GlobalConstants.ChunkSize;
            var center = new BlockPos(chunkCoord.X * cs + 16, 64, chunkCoord.Y * cs + 16);
            bool nearPlayer = cfg.EnablePlayerPriorityRegistration
                && PlayerProximity.IsNearAnyPlayer(api, center, cfg.PlayerRegistrationPriorityRadiusBlocks);

            Vec2i coordCopy = chunkCoord;
            api.Event.RegisterCallback(_ => ScheduleRegistrationScan(coordCopy, nearPlayer), 500);

            foliageCells.ScheduleChunkSync(api, chunkCoord);
            MyceliumChunkRegistrar.ScheduleScanColumn(api, chunkCoord);
        }

        void ScheduleRegistrationScan(Vec2i chunkCoord, bool nearPlayer)
        {
            if (!EcosystemConfig.Loaded.EcosystemEnabled || api?.World?.BlockAccessor == null) return;
            if (api.World.BlockAccessor.GetMapChunk(chunkCoord.X, chunkCoord.Y) == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (nearPlayer && cfg.EnableBurstRegistrationNearPlayers)
            {
                TryBurstRegisterChunk(chunkCoord);
            }
            else
            {
                EnqueueChunkScan(chunkCoord, highPriority: nearPlayer);
            }
        }

        void TryBurstRegisterChunk(Vec2i chunkCoord)
        {
            if (!EcosystemConfig.Loaded.EcosystemEnabled || api == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EnableBurstRegistrationNearPlayers) return;

            ChunkRegistrationBurst.TryCompleteChunk(
                this,
                api,
                cfg,
                chunkCoord,
                registrationScanQueue,
                cfg.EffectiveMaxBurstRegistrationsPerChunk(),
                cfg.BurstRegistrationBudgetMs,
                cfg.ResolvePriorityRegistrationBudgetMs());
        }

        void EnqueueChunkScan(Vec2i chunkCoord, int nextLx = 0, int nextLz = 0, int nextY = -1, bool highPriority = false)
        {
            registrationScanQueue.Enqueue(
                new PendingChunkScan(chunkCoord, nextLx, nextLz, nextY),
                highPriority);
        }

        static long ChunkScanKey(int cx, int cz) => ((long)cx << 32) | (uint)cz;

        void OnChunkColumnUnloaded(Vec3i chunkCoord)
        {
            var cc = new Vec2i(chunkCoord.X, chunkCoord.Z);
            registrationScanQueue.MarkUnloaded(cc);
            pendingRegistrations.RemoveChunk(cc);
            backgroundRegistration.OnChunkUnload(cc);
            registry.RemoveChunk(cc);
            SpacingIndex?.RemoveChunk(cc);
            foliageCells.RemoveChunk(cc);
        }

        void OnDidPlaceBlock(IServerPlayer byPlayer, int oldBlockId, BlockSelection blockSel, ItemStack withItemStack)
        {
            if (blockSel?.Position == null || api == null) return;
            Block oldGround = oldBlockId > 0 ? api.World.Blocks[oldBlockId] : null;
            BlockPos placed = blockSel.Position.Copy();
            ScheduleFarmlandBridgeCheck(placed, oldGround);
            api.Event.RegisterCallback(_ =>
            {
                InvalidateEnvironmentAround(placed);
                TryRegisterPlacedBlock(placed);
                WakeEcologyAround(placed);
            }, 0);
        }

        void TryRegisterPlacedBlock(BlockPos pos)
        {
            if (!EcosystemConfig.Loaded.EcosystemEnabled) return;

            Block block = api.World.BlockAccessor.GetBlock(pos);
            if (block == null || block.Id == 0) return;

            if (CanopyFoliageRules.IsSeasonalFoliageBlock(block))
            {
                foliageCells.OnBlockAdded(pos);
            }

            if (TryRegisterPlacedVine(pos, block))
            {
                return;
            }

            if (PlantCodeHelper.IsTreeSaplingBlock(block))
            {
                string wood = PlantCodeHelper.GetTreeWood(block);
                if (!string.IsNullOrEmpty(wood))
                {
                    maturationQueues.AddTreeSapling(pos, wood, api.World.Calendar.TotalHours);
                }

                return;
            }

            if (!EcosystemParticipant.TryFromBlock(block, out IEcosystemParticipant participant))
            {
                if (TallgrassEstablishment.ShouldQueueAfterPlacement(api, pos, block))
                {
                    maturationQueues.AddTallgrassPromotion(api, pos);
                }

                return;
            }

            BlockPos anchor = PlantCodeHelper.GetReproduceAnchor(api.World.BlockAccessor, pos, block.Code);
            if (registry.Contains(anchor)) return;

            RegisterReproducer(anchor, participant, playerPlaced: true);
        }

        bool TryRegisterPlacedVine(BlockPos pos, Block block)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EnableWildVineEcology || !WildVineHelper.IsVineBlock(block)) return false;

            IBlockAccessor acc = api.World.BlockAccessor;

            Block placed = acc.GetBlock(pos);
            if (!WildVineHelper.TryParse(placed, out WildVineInfo info)) return true;

            if (registry.Contains(pos)) return true;
            if (WildVineHelper.ColumnHasRegistryEntry(acc, pos, info, registry.Contains)) return true;

            if (!EcosystemParticipant.TryFromBlock(placed, out IEcosystemParticipant participant)) return true;

            RegisterReproducer(pos, participant, playerPlaced: true);
            return true;
        }

        internal void TryRegisterVineSpreadTip(BlockPos pos)
        {
            if (api?.World?.BlockAccessor == null || pos == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EnableWildVineEcology) return;

            IBlockAccessor acc = api.World.BlockAccessor;
            Block block = acc.GetBlock(pos);
            if (!WildVineHelper.IsEndBlock(block)) return;
            if (!WildVineHelper.TryParse(block, out WildVineInfo info)) return;
            if (registry.Contains(pos)) return;
            if (WildVineHelper.ColumnHasRegistryEntry(acc, pos, info, registry.Contains)) return;
            if (!EcosystemParticipant.TryFromBlock(block, out IEcosystemParticipant participant)) return;

            RegisterReproducer(pos, participant, spawnBurst: false);
        }

        void OnDidUseBlock(IServerPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel?.Position == null || api == null) return;
            Block ground = api.World.BlockAccessor.GetBlock(blockSel.Position);
            ScheduleFarmlandBridgeCheck(blockSel.Position, ground);
        }

        void ScheduleFarmlandBridgeCheck(BlockPos pos, Block groundBeforeTill)
        {
            if (!EcosystemConfig.Loaded.UseFarmlandNutrientBridge) return;

            SoilFertilityTier tier = SoilFertilityTier.Medium;
            if (groundBeforeTill != null && WildSoilBlockMapper.IsSuccessionTarget(groundBeforeTill))
            {
                tier = SoilFertilityTierExtensions.FromBlockFertility(groundBeforeTill);
            }

            BlockPos copy = pos.Copy();
            SoilFertilityTier tierCopy = tier;
            api.Event.RegisterCallback(
                _ =>
                {
                    PlantSoilRole role = WildSoilAgroSampler.SampleDominantRole(api, copy);
                    FarmlandTillBridge.TryApplyAfterTill(api, copy, role, tierCopy);
                },
                50);
        }

        void OnDidBreakBlock(IServerPlayer byPlayer, int oldBlockId, BlockSelection blockSel)
        {
            if (blockSel?.Position == null || api == null) return;

            Block oldBlock = api.World.Blocks[oldBlockId];
            BlockPos pos = blockSel.Position;

            PlantHandHarvest.TryDropPlantBlockOnBreak(api, byPlayer, oldBlock, pos);

            Block newBlock = api.World.BlockAccessor.GetBlock(pos);
            if (PlantCodeHelper.IsBrownsedgeMowHarvestTransition(oldBlock, newBlock))
            {
                return;
            }

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (cfg.EnableWildVineEcology)
            {
                OnWildVineHostChanged(pos);
            }

            if (PlantCodeHelper.IsEcologySpreadParent(oldBlock) && cfg.EnableSymbiosis)
            {
                FloraSymbiosis.NotifyHostRemoved(api, pos, oldBlock);
            }

            if (PlantCodeHelper.IsTreeLogGrownBlock(oldBlock))
            {
                MyceliumTreeCascade.OnTreeRemoved(api, pos, oldBlock);
                string wood = PlantCodeHelper.GetTreeWood(oldBlock);
                treeCalendarAgeStore.TryRemoveIfTreeGone(api.World.BlockAccessor, pos, wood);
            }
            else if (PlantCodeHelper.IsFerntreeEcologyBlock(oldBlock))
            {
                treeCalendarAgeStore.TryRemoveIfTreeGone(
                    api.World.BlockAccessor, pos, EcologyFerntreeSpecies.Ferntree);
            }

            if (CanopyFoliageRules.IsSeasonalFoliageBlock(oldBlock))
            {
                foliageCells.OnBlockRemoved(pos);
                if (api.World?.Calendar != null)
                {
                    foliagePlayerVacancies.NotePlayerBreak(pos, api.World.Calendar.TotalHours);
                }
            }

            if (WildSoilGroundRules.HasActiveMycelium(api.World.BlockAccessor, pos))
            {
                registry.Remove(pos);
                SpacingIndex?.Remove(pos);
                InvalidateEnvironmentAround(pos);
                WakeEcologyAround(pos);
                return;
            }

            bool ecologyPlant = PlantCodeHelper.CountsAsEcologyPlantRemovalForWake(oldBlock);
            bool forestNeighbor = FloraContextSampler.IsForestNeighborBlock(oldBlock);
            bool inRegistry = registry.Contains(pos);
            bool wakeNeighbors = cfg.EnableEventDrivenSpread
                && (ecologyPlant
                    || inRegistry
                    || (PlantCodeHelper.IsEcologySpreadParent(oldBlock) && cfg.EnableSymbiosis)
                    || CanopyFoliageRules.IsSeasonalFoliageBlock(oldBlock));

            if (!inRegistry && !ecologyPlant && !forestNeighbor && !wakeNeighbors) return;

            if (forestNeighbor || ecologyPlant)
            {
                FloraContext?.InvalidateAround(pos, 2);
            }

            if (inRegistry)
            {
                registry.Remove(pos);
                SpacingIndex?.Remove(pos);
                InvalidateEnvironmentAround(pos);
            }

            maturationQueues.Remove(pos);
            stumpDecayScheduler.Remove(pos);
            EcologyHistoryRecorder.Remove(pos);

            if (wakeNeighbors)
            {
                WakeEcologyAround(pos);
            }
        }

        void OnWildVineHostChanged(BlockPos changedPos)
        {
            WildVineColumnSupport.OnStructuralChange(api, changedPos, OnWildVineCellRemoved);
        }

        /// <summary>
        /// Revalidate vine columns after a host cell is cleared (player break, ecology remove,
        /// seasonal leaf strip, tree senescence).
        /// </summary>
        internal void NotifyWildVineHostChanged(BlockPos changedPos)
        {
            if (changedPos == null || !EcosystemConfig.Loaded.EnableWildVineEcology) return;
            OnWildVineHostChanged(changedPos);
        }

        /// <summary>
        /// On chunk registration, prune vine columns whose top is no longer anchored (cheap check;
        /// at most one walk per column per drain via <see cref="vineLoadSupportChecked"/>).
        /// </summary>
        /// <returns>False when the column was unsupported and removed (or the cell is gone).</returns>
        bool TryEnsureVineColumnSupportedOnLoad(IBlockAccessor acc, BlockPos pos, in WildVineInfo info)
        {
            if (api?.World == null || acc == null || pos == null) return false;
            if (!EcosystemConfig.Loaded.EnableWildVineEcology) return true;

            BlockPos top = WildVineHelper.FindHighestColumnCell(acc, pos, info);
            var key = (top.X, top.Z, info.Facing.Code, info.Tropical);
            if (!vineLoadSupportChecked.Add(key))
            {
                return WildVineHelper.IsVineBlock(acc.GetBlock(pos));
            }

            if (WildVineColumnSupport.IsColumnTopAnchored(acc, api.World, top, info))
            {
                return true;
            }

            WildVineColumnSupport.PruneUnsupportedColumn(acc, api.World, top, OnWildVineCellRemoved);
            return false;
        }

        void OnWildVineCellRemoved(BlockPos pos)
        {
            if (pos == null) return;

            registry.Remove(pos);
            SpacingIndex?.Remove(pos);
            InvalidateEnvironmentAround(pos);
        }

        internal void PruneWildVineColumn(BlockPos anyInColumn)
        {
            if (api == null || anyInColumn == null) return;

            WildVineColumnSupport.PruneUnsupportedColumn(
                api.World.BlockAccessor,
                api.World,
                anyInColumn,
                OnWildVineCellRemoved);
        }

        void OnChunkScanTick(float dt)
        {
            if (!EcosystemConfig.Loaded.EcosystemEnabled || api == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            IBlockAccessor acc = api.World.BlockAccessor;
            int registrationBudgetMs = cfg.ResolveRegistrationBudgetMs();
            long budgetTicks = registrationBudgetMs > 0
                ? registrationBudgetMs * Stopwatch.Frequency / 1000
                : 0;

            HashSet<long> activePlayerChunks = null;
            if (cfg.OnlyActivateNearPlayers)
            {
                activePlayerChunks = PlayerProximity.BuildActivePlayerChunks(api, cfg.PlayerActivationRadiusBlocks);
            }

            tickBudgetWatch.Restart();

            if (cfg.EnableBackgroundRegistrationScan)
            {
                backgroundRegistration.PollCompleted(this, cfg);
            }

            int seasonKey = FoliageSeasonKey.Current(api);
            if (foliageCells.TickSeasonChanged(api))
            {
                if (activePlayerChunks != null)
                {
                    foreach (long key in activePlayerChunks)
                    {
                        int cx = (int)(key >> 32);
                        int cz = (int)(key & 0xFFFFFFFF);
                        if (acc.GetMapChunk(cx, cz) != null)
                        {
                            EnqueueChunkScan(new Vec2i(cx, cz), highPriority: cfg.EnablePlayerPriorityRegistration);
                        }
                    }
                }
            }

            RescanPlayerVicinity(cfg, acc);

            bool syncFoliage = cfg.EnableSeasonalFoliage && FoliageSyncModeHelper.UsesChunkSync(cfg);
            FoliageCellIndex foliageIndex = FoliageSyncModeHelper.UsesRandomTick(cfg)
                ? foliageCells.Index
                : null;

            if (syncFoliage && cfg.EnableBackgroundRegistrationScan)
            {
                ProcessFoliageChunkSyncBatch(cfg, seasonKey, budgetTicks);
            }

            int queuePasses = registrationScanQueue.Count;
            if (queuePasses > MaxChunkScanDequeAttemptsPerTick)
            {
                queuePasses = MaxChunkScanDequeAttemptsPerTick;
            }

            if (cfg.EnablePlayerPriorityRegistration)
            {
                int priorityPassesLeft = cfg.EffectiveMaxPriorityChunkScansPerTick();
                int priorityRegistrationsLeft = cfg.EffectiveMaxPriorityRegistrationsPerTick();
                int priorityPassBudgetMs = cfg.ResolvePriorityRegistrationBudgetMs();
                if (cfg.FoliageChunkSyncBudgetMs > priorityPassBudgetMs)
                {
                    priorityPassBudgetMs = cfg.FoliageChunkSyncBudgetMs;
                }

                ProcessRegistrationScanBatch(
                    cfg,
                    acc,
                    ref queuePasses,
                    ref priorityPassesLeft,
                    ref priorityRegistrationsLeft,
                    priorityPassBudgetMs,
                    budgetTicks,
                    syncFoliage,
                    seasonKey,
                    foliageIndex,
                    activePlayerChunks,
                    preferPriority: true);
            }

            int passesLeft = cfg.EffectiveMaxChunkColumnsScannedPerTick();
            int registrationsLeft = cfg.EffectiveMaxRegistrationsPerTick();
            int passBudgetMs = cfg.ResolveRegistrationBudgetMs();
            if (cfg.FoliageChunkSyncBudgetMs > passBudgetMs)
            {
                passBudgetMs = cfg.FoliageChunkSyncBudgetMs;
            }

            ProcessRegistrationScanBatch(
                cfg,
                acc,
                ref queuePasses,
                ref passesLeft,
                ref registrationsLeft,
                passBudgetMs,
                budgetTicks,
                syncFoliage,
                seasonKey,
                foliageIndex,
                activePlayerChunks,
                preferPriority: false);

            if (registrationsLeft > 0)
            {
                HashSet<long> treeScanChunks = activePlayerChunks;
                if (treeScanChunks == null || treeScanChunks.Count == 0)
                {
                    treeScanChunks = PlayerProximity.BuildActivePlayerChunks(
                        api, cfg.PlayerActivationRadiusBlocks);
                }

                cyclicTreeScanner.ProcessTick(
                    acc,
                    cfg,
                    treeScanChunks,
                    (basePos, wood) => TryRegisterDiscoveredTree(acc, basePos, ref registrationsLeft),
                    ref registrationsLeft,
                    budgetTicks,
                    tickBudgetWatch);
            }

            if (registrationsLeft > 0)
            {
                HashSet<long> floraScanChunks = activePlayerChunks;
                if (floraScanChunks == null || floraScanChunks.Count == 0)
                {
                    floraScanChunks = PlayerProximity.BuildActivePlayerChunks(
                        api, cfg.PlayerActivationRadiusBlocks);
                }

                cyclicFloraScanner.ProcessTick(
                    api,
                    acc,
                    cfg,
                    floraScanChunks,
                    (pos, block, needsEstablishment) =>
                        TryRegisterDiscoveredFlora(acc, pos, block, needsEstablishment, ref registrationsLeft),
                    ref registrationsLeft,
                    budgetTicks,
                    tickBudgetWatch);
            }

            DrainPendingRegistrations(cfg, acc, activePlayerChunks);

            tickBudgetWatch.Stop();
        }

        void DrainPendingRegistrations(EcosystemConfig cfg, IBlockAccessor acc, HashSet<long> activePlayerChunks)
        {
            if (pendingRegistrations.TotalPending == 0) return;

            // One support check per vine column per drain wave (section+end hits share a column).
            vineLoadSupportChecked.Clear();

            HashSet<long> priorityChunks = null;
            if (cfg.EnablePlayerPriorityRegistration && api != null)
            {
                priorityChunks = PlayerProximity.BuildActivePlayerChunks(
                    api, cfg.PlayerRegistrationPriorityRadiusBlocks);
            }

            int appliesLeft = cfg.EffectiveMaxPriorityRegistryAppliesPerTick();
            if (appliesLeft > 0 && priorityChunks != null && priorityChunks.Count > 0)
            {
                pendingRegistrations.Drain(
                    this,
                    acc,
                    appliesLeft,
                    cfg.EffectiveMaxRegistryAppliesPerChunkPerTick(),
                    priorityChunks);
            }

            appliesLeft = cfg.EffectiveMaxRegistryAppliesPerTick();
            if (appliesLeft > 0)
            {
                pendingRegistrations.Drain(
                    this,
                    acc,
                    appliesLeft,
                    cfg.EffectiveMaxRegistryAppliesPerChunkPerTick(),
                    priorityChunks);
            }
        }

        internal void OnPendingChunkDrained(Vec2i chunkCoord)
        {
            if (pendingRegistrations.IsReadyToMarkComplete(chunkCoord))
            {
                registrationScanQueue.MarkComplete(chunkCoord);
            }
        }

        internal void NotifyRegistrationScanCompleted(Vec2i chunkCoord)
        {
            pendingRegistrations.SetScanCompleted(chunkCoord, true);
            if (pendingRegistrations.IsReadyToMarkComplete(chunkCoord))
            {
                registrationScanQueue.MarkComplete(chunkCoord);
            }
        }

        internal bool TryApplyPendingRegistration(IBlockAccessor acc, PendingRegistration item, out bool stale)
        {
            stale = false;
            if (acc == null || item.Pos == null) return false;

            Block block = acc.GetBlock(item.Pos);
            if (block == null || block.Id == 0)
            {
                stale = true;
                return false;
            }

            if (item.BlockCode != null && block.Code != null
                && !RegistrationBlockMatch.MatchesSnapshot(block, item.BlockCode))
            {
                stale = true;
                return false;
            }

            switch (item.Kind)
            {
                case PendingRegistrationKind.Tree:
                    if (registry.Contains(item.Pos)) return true;
                    if (!EcosystemParticipant.TryFromBlock(block, out IEcosystemParticipant treeParticipant))
                    {
                        stale = true;
                        return false;
                    }

                    RegisterReproducer(item.Pos, treeParticipant, spawnBurst: false);
                    return registry.Contains(item.Pos);

                case PendingRegistrationKind.Vine:
                    if (registry.Contains(item.Pos)) return true;
                    if (!WildVineHelper.TryParse(block, out WildVineInfo vineInfo))
                    {
                        stale = true;
                        return false;
                    }

                    // Chunk-load / scan registration: drop floating columns (e.g. leaf host already gone)
                    // before they enter the reproduce registry. Deduped per column in this drain wave.
                    if (!TryEnsureVineColumnSupportedOnLoad(acc, item.Pos, vineInfo))
                    {
                        stale = true;
                        return false;
                    }

                    block = acc.GetBlock(item.Pos);
                    if (!WildVineHelper.IsVineBlock(block))
                    {
                        stale = true;
                        return false;
                    }

                    if (!EcosystemParticipant.TryFromBlock(block, out IEcosystemParticipant vineParticipant))
                    {
                        stale = true;
                        return false;
                    }

                    RegisterReproducer(item.Pos, vineParticipant, spawnBurst: false);
                    return registry.Contains(item.Pos);

                default:
                    if (registry.Contains(item.Pos)) return true;

                    if (!EcosystemParticipant.TryCreateForRegistration(api, item.Pos, block, out IEcosystemParticipant participant))
                    {
                        if (TallgrassEstablishment.ShouldQueueAfterPlacement(api, item.Pos, block))
                        {
                            maturationQueues.AddTallgrassPromotion(api, item.Pos);
                            return true;
                        }

                        if (TryQueueFernDiscoveryMaturation(item.Pos, block))
                        {
                            return true;
                        }

                        stale = true;
                        return false;
                    }

                    BlockPos anchor = PlantCodeHelper.GetReproduceAnchor(acc, item.Pos, item.BlockCode);
                    if (registry.Contains(anchor)) return true;

                    RegisterReproducer(anchor, participant, spawnBurst: false);
                    return registry.Contains(anchor);
            }
        }

        void ProcessFoliageChunkSyncBatch(EcosystemConfig cfg, int seasonKey, long budgetTicks)
        {
            int passBudgetMs = cfg.FoliageChunkSyncBudgetMs;
            long passDeadline = passBudgetMs > 0
                ? Stopwatch.GetTimestamp() + passBudgetMs * Stopwatch.Frequency / 1000
                : long.MaxValue;

            foliageCells.ProcessChunkSyncBatch(
                api,
                seasonKey,
                cfg.FoliageChunkWorkPerTick,
                passDeadline,
                budgetTicks,
                tickBudgetWatch,
                OnFoliageChunkChanged);
        }

        void OnFoliageChunkChanged(Vec2i chunkCoord, int foliageChanged)
        {
            if (foliageChanged <= 0) return;

            int cs = GlobalConstants.ChunkSize;
            var invalidateAt = new BlockPos(
                chunkCoord.X * cs + 8,
                64,
                chunkCoord.Y * cs + 8);
            FloraContext?.InvalidateAround(invalidateAt, 3);
            InvalidateEnvironmentAround(invalidateAt);
        }

        void ProcessRegistrationScanBatch(
            EcosystemConfig cfg,
            IBlockAccessor acc,
            ref int queuePasses,
            ref int passesLeft,
            ref int registrationsLeft,
            int passBudgetMs,
            long budgetTicks,
            bool syncFoliage,
            int seasonKey,
            FoliageCellIndex foliageIndex,
            HashSet<long> activePlayerChunks,
            bool preferPriority)
        {
            long passDeadline = passBudgetMs > 0
                ? Stopwatch.GetTimestamp() + passBudgetMs * Stopwatch.Frequency / 1000
                : long.MaxValue;

            while (queuePasses > 0 && passesLeft > 0)
            {
                if (budgetTicks > 0 && tickBudgetWatch.ElapsedTicks >= budgetTicks) break;

                if (!registrationScanQueue.TryDequeue(out PendingChunkScan job, preferPriority))
                {
                    break;
                }

                queuePasses--;

                if (activePlayerChunks != null && !PlayerProximity.IsActiveChunk(activePlayerChunks, job.ChunkCoord))
                {
                    EnqueueChunkScan(job.ChunkCoord, job.NextLx, job.NextLz, job.NextY, highPriority: preferPriority);
                    continue;
                }

                passesLeft--;

                if (cfg.EnableBackgroundRegistrationScan)
                {
                    bool highPriority = preferPriority || IsPlayerPriorityChunk(cfg, job.ChunkCoord);
                    if (backgroundRegistration.TryAdvance(
                            this,
                            acc,
                            cfg,
                            job,
                            highPriority,
                            passDeadline,
                            out bool needsRequeue))
                    {
                        if (needsRequeue)
                        {
                            EnqueueChunkScan(job.ChunkCoord, highPriority: highPriority);
                        }

                        continue;
                    }
                }

                if (!TryRunRegistrationPass(
                        job,
                        acc,
                        cfg,
                        ref registrationsLeft,
                        passDeadline,
                        syncFoliage,
                        seasonKey,
                        foliageIndex,
                        out ChunkEcologyColumnPass.Result pass,
                        out bool completed))
                {
                    EnqueueChunkScan(job.ChunkCoord, job.NextLx, job.NextLz, job.NextY, highPriority: preferPriority);
                    continue;
                }

                if (!completed)
                {
                    bool highPriority = preferPriority || IsPlayerPriorityChunk(cfg, job.ChunkCoord);
                    EnqueueChunkScan(job.ChunkCoord, pass.ResumeLx, pass.ResumeLz, pass.ResumeY, highPriority);
                }
                else
                {
                    NotifyRegistrationScanCompleted(job.ChunkCoord);
                }
            }
        }

        bool IsPlayerPriorityChunk(EcosystemConfig cfg, Vec2i chunkCoord)
        {
            if (!cfg.EnablePlayerPriorityRegistration || api == null) return false;

            int cs = GlobalConstants.ChunkSize;
            var center = new BlockPos(chunkCoord.X * cs + 16, 64, chunkCoord.Y * cs + 16);
            return PlayerProximity.IsNearAnyPlayer(api, center, cfg.PlayerRegistrationPriorityRadiusBlocks);
        }

        internal void EnqueueRegistrationScanHits(Vec2i chunkCoord, ChunkEcologyColumnPass.Result pass)
        {
            pendingRegistrations.EnqueueHits(chunkCoord, pass.FlowerHits, PendingRegistrationKind.Flower);
            pendingRegistrations.EnqueueHits(chunkCoord, pass.VineHits, PendingRegistrationKind.Vine);

            if (pass.EstablishingTallgrassHits != null && api != null)
            {
                for (int i = 0; i < pass.EstablishingTallgrassHits.Count; i++)
                {
                    ChunkFlowerHit hit = pass.EstablishingTallgrassHits[i];
                    if (hit.Pos == null) continue;
                    maturationQueues.AddTallgrassPromotion(api, hit.Pos);
                }
            }

            if (pass.TreeHits != null)
            {
                for (int i = 0; i < pass.TreeHits.Count; i++)
                {
                    ChunkFlowerHit hit = pass.TreeHits[i];
                    if (hit.Pos == null || hit.BlockCode == null) continue;
                    pendingRegistrations.TryEnqueueTree(chunkCoord, hit.Pos, hit.BlockCode, registry);
                }
            }
        }

        internal bool IsChunkRegistrationFinished(Vec2i chunkCoord)
        {
            if (backgroundRegistration.IsBusy(chunkCoord)) return false;
            if (pendingRegistrations.HasPending(chunkCoord)) return false;
            return pendingRegistrations.IsScanCompleted(chunkCoord);
        }

        /// <summary>Enqueue tallgrass growth when inspect finds grass below spread height.</summary>
        public bool TryQueueTallgrassPromotionAtInspect(BlockPos pos, Block block)
        {
            if (api == null || api.Side != EnumAppSide.Server || pos == null || block == null) return false;
            if (!TallgrassEstablishment.ShouldQueueAfterPlacement(api, pos, block)) return false;

            maturationQueues.AddTallgrassPromotion(api, pos);
            return true;
        }

        /// <summary>One-shot registration when a player inspects an eligible but missed plant.</summary>
        public bool TryRegisterEligiblePlantAtInspect(BlockPos pos, Block block)
        {
            if (api == null || api.Side != EnumAppSide.Server || pos == null || block == null) return false;
            if (registry.Contains(pos)) return true;

            if (TallgrassSpreadMaturation.UsesMaturation(EcosystemConfig.Loaded)
                && PlantCodeHelper.ResolveEcologySpecies(block) == "tallgrass"
                && !TallgrassSpreadMaturation.CanReproduceFrom(block, api, pos))
            {
                if (TallgrassEstablishment.ShouldQueueAfterPlacement(api, pos, block))
                {
                    maturationQueues.AddTallgrassPromotion(api, pos);
                }

                return false;
            }

            if (!EcosystemParticipant.TryCreateForRegistration(api, pos, block, out IEcosystemParticipant participant)) return false;

            BlockPos anchor = PlantCodeHelper.GetReproduceAnchor(api.World.BlockAccessor, pos, block.Code);
            if (registry.Contains(anchor)) return true;

            if (RegisterReproducer(anchor, participant, spawnBurst: false, ignorePlayerProximity: true))
            {
                return true;
            }

            EnqueueChunkScan(ReproducerRegistry.ToChunkCoord(anchor), highPriority: true);
            return false;
        }

        public bool IsRegistrationPendingAt(BlockPos pos) =>
            pos != null && pendingRegistrations.HasPendingAt(pos);

        /// <summary>Re-attach or detach animal trail physics hooks after config changes.</summary>
        public void RefreshFootTrafficAnimals() => footTraffic?.RefreshAnimalAttachments();

        void ProcessStress(double now, int maxChecks, ICollection<Vec2i> activeChunks, long budgetTicks = 0)
        {
            if (maxChecks <= 0 || api == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EnableStressDeath && !cfg.EnableMyceliumEcology) return;
            IBlockAccessor acc = api.World.BlockAccessor;

            registry.ProcessStress(
                maxChecks,
                entry =>
                {
                    if (budgetTicks > 0 && tickBudgetWatch.ElapsedTicks >= budgetTicks) return false;
                    if (now < entry.NextStressCheckAt) return false;

                    PlantRequirements req = entry.Requirements;
                    if (req == null) return false;

                    if (req.Habitat == EcologyHabitat.MyceliumAnchor)
                    {
                        if (!cfg.EnableMyceliumEcology
                            || !WildSoilGroundRules.HasActiveMycelium(acc, entry.Origin))
                        {
                            return true;
                        }

                        if (!MyceliumStressEvaluator.MeetsSurvival(api, entry))
                        {
                            entry.FailedSurvivalChecks++;
                        }
                        else
                        {
                            entry.FailedSurvivalChecks = 0;
                            entry.NextStressCheckAt = now + cfg.StressRecheckHours;
                            return false;
                        }

                        if (entry.FailedSurvivalChecks < cfg.MaxFailedSurvivalChecks)
                        {
                            entry.NextStressCheckAt = now + cfg.StressRecheckHours;
                            return false;
                        }

                        return true;
                    }

                    if (!cfg.EnableStressDeath) return false;

                    if (req.Habitat != EcologyHabitat.Terrestrial) return false;

                    if (cfg.EnableSymbiosis
                        && !string.IsNullOrEmpty(req.Species)
                        && FloraSymbiosis.TryGetRule(req.Species, out _)
                        && !FloraSymbiosis.HasRequiredHost(acc, entry.Origin, req.Species))
                    {
                        entry.FailedSurvivalChecks++;
                    }
                    else if (!CanSurviveAt(entry.Origin, req))
                    {
                        entry.FailedSurvivalChecks++;
                    }
                    else if (cfg.UseNicheContext
                        && req.HasNicheProfile
                        && Niche != null
                        && EcologySpreadFitness.NicheMultiplierFor(req, Niche.GetNiche(api, entry.Origin))
                            < cfg.NicheStressThreshold)
                    {
                        entry.FailedSurvivalChecks++;
                    }
                    else if (SeasonEcology.RollSeasonalStressFailure(api, entry.Origin, req))
                    {
                        entry.FailedSurvivalChecks++;
                    }
                    else
                    {
                        entry.FailedSurvivalChecks = 0;
                        entry.NextStressCheckAt = now + cfg.StressRecheckHours;
                        if (cfg.EnableFallowRestoration)
                            FallowRestoration.TryRestoreNear(api, entry.Origin);
                        return false;
                    }

                    if (entry.FailedSurvivalChecks < cfg.MaxFailedSurvivalChecks)
                    {
                        entry.NextStressCheckAt = now + cfg.StressRecheckHours;
                        return false;
                    }

                    return true;
                },
                pos =>
                {
                    if (TryGetReproducer(pos, out ReproducerEntry entry)
                        && entry.Requirements?.Habitat == EcologyHabitat.MyceliumAnchor)
                    {
                        RemoveMyceliumAnchor(pos, "mycelium-stress");
                        return;
                    }

                    string species = TryGetReproducer(pos, out ReproducerEntry dying)
                        ? dying.Requirements?.Species
                        : PlantCodeHelper.ResolveEcologySpecies(api.World.BlockAccessor.GetBlock(pos));
                    if (!string.IsNullOrEmpty(species))
                    {
                        EcologyHistoryRecorder.RecordStressDeath(api, pos, species);
                    }

                    RemoveEcologyPlant(pos, cascadeSymbiosis: true,
                        reason: "stress",
                        soilEvent: SoilSuccessionEvent.Death);
                },
                activeChunks);
        }

        ICollection<Vec2i> BuildActiveRegistryChunks(EcosystemConfig cfg)
        {
            if (!cfg.OnlyActivateNearPlayers && !cfg.LimitSpreadNearPlayers) return null;

            activeChunkScratch.Clear();
            activeChunkScratch.AddRange(registry.CollectChunksNearPlayers(api, cfg.PlayerActivationRadiusBlocks));
            return activeChunkScratch;
        }

        ICollection<Vec2i> BuildActivePlayerChunks(EcosystemConfig cfg)
        {
            if (!cfg.OnlyActivateNearPlayers) return null;

            activeChunkScratch.Clear();
            HashSet<long> keys = PlayerProximity.BuildActivePlayerChunks(api, cfg.PlayerActivationRadiusBlocks);
            foreach (long key in keys)
            {
                int cx = (int)(key >> 32);
                int cz = (int)(key & 0xFFFFFFFF);
                activeChunkScratch.Add(new Vec2i(cx, cz));
            }

            return activeChunkScratch;
        }

        void OnStressTick(float dt)
        {
            if (!EcosystemConfig.Loaded.EcosystemEnabled || api == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            double now = api.World.Calendar.TotalHours;
            ICollection<Vec2i> activeChunks = BuildActiveRegistryChunks(cfg);
            if (activeChunks != null && activeChunks.Count == 0) return;

            if (cfg.EnableStressDeath || cfg.EnableMyceliumEcology)
            {
                int stressBudgetMs = cfg.StressBudgetMs > 0 ? cfg.StressBudgetMs : cfg.TickBudgetMs;
                long budgetTicks = stressBudgetMs > 0
                    ? stressBudgetMs * Stopwatch.Frequency / 1000
                    : 0;

                tickBudgetWatch.Restart();
                ProcessStress(now, cfg.MaxStressChecksPerTick, activeChunks, budgetTicks);
                tickBudgetWatch.Stop();
            }

        }

        void OnReproduceTick(float dt)
        {
            if (!EcosystemConfig.Loaded.EcosystemEnabled || api == null) return;

            TryDeferredTreeBootstrap();
            TryDeferredFoliageBootstrap();
            TryLogCalendarDebugOnce();
            ColumnCache?.AdvanceGeneration();

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            double now = api.World.Calendar.TotalHours;

            // Trail soil sync is footstep-only. Calendar ticks must not walk the column store —
            // creative time scrub used to mass-decay + SetBlock via ProcessDeferredCoverageSync.

            SeasonEcologyWake.TryWakeOnMonthChange(api, cfg, registry, ref lastSeasonWakeMonth);
            ICollection<Vec2i> spreadActiveChunks = BuildActiveRegistryChunks(cfg);
            ICollection<Vec2i> canopyActiveChunks = BuildActivePlayerChunks(cfg);

            if (cfg.OnlyActivateNearPlayers
                && canopyActiveChunks != null
                && canopyActiveChunks.Count == 0)
            {
                return;
            }

            long spreadBudgetTicks = cfg.ResolveSpreadBudgetMs() > 0
                ? cfg.ResolveSpreadBudgetMs() * Stopwatch.Frequency / 1000
                : 0;

            var timings = new ReproduceTickTimings();
            long tickStart = Stopwatch.GetTimestamp();
            backgroundSpread.BeginReproduceTick();

            tickBudgetWatch.Restart();
            maturationQueues.ProcessTreeSaplings(api, this, now, cfg.MaxPendingTreeChecksPerTick);
            timings.SaplingsTicks = tickBudgetWatch.ElapsedTicks;

            tickBudgetWatch.Restart();
            maturationQueues.ProcessFlower(api, this, now, cfg.MaxPendingFlowerMaturationChecksPerTick);
            timings.FlowerMaturationTicks = tickBudgetWatch.ElapsedTicks;

            tickBudgetWatch.Restart();
            maturationQueues.ProcessFern(api, this, now, cfg.MaxPendingFernMaturationChecksPerTick);

            maturationQueues.ProcessShoreSedge(api, this, now, cfg.MaxPendingFlowerMaturationChecksPerTick);

            tickBudgetWatch.Restart();
            flowerPhenologyScheduler.Tick(api, cfg, registry, spreadActiveChunks, now);
            timings.FlowerPhenologyTicks = tickBudgetWatch.ElapsedTicks;

            tickBudgetWatch.Restart();
            fernPhenologyScheduler.Tick(api, cfg, registry, spreadActiveChunks, now);
            timings.FernPhenologyTicks = tickBudgetWatch.ElapsedTicks;

            tickBudgetWatch.Restart();
            tallgrassPhenologyScheduler.Tick(api, cfg, registry, spreadActiveChunks, now);
            timings.TallgrassPhenologyTicks = tickBudgetWatch.ElapsedTicks;

            tickBudgetWatch.Restart();
            plantSnowCoverScheduler.Tick(api, cfg, registry, spreadActiveChunks);
            timings.PlantSnowCoverTicks = tickBudgetWatch.ElapsedTicks;

            tickBudgetWatch.Restart();
            maturationQueues.ProcessTallgrass(api, this, now, cfg.MaxPendingTallgrassPromotionChecksPerTick);
            timings.TallgrassPromotionTicks = tickBudgetWatch.ElapsedTicks;

            tickBudgetWatch.Restart();
            maturationQueues.ProcessBerry(api, this, now, cfg.MaxPendingBerryMaturationChecksPerTick);
            timings.BerryMaturationTicks = tickBudgetWatch.ElapsedTicks;

            tickBudgetWatch.Restart();
            stumpDecayScheduler.Process(api, cfg, cfg.MaxStumpDecayChecksPerTick);
            timings.StumpDecayTicks = tickBudgetWatch.ElapsedTicks;

            tickBudgetWatch.Restart();

            long foliageBudgetTicks = cfg.FoliageBudgetMs > 0
                ? cfg.FoliageBudgetMs * Stopwatch.Frequency / 1000
                : 0;

            foliageCells.ProcessRandomTick(api, canopyActiveChunks, foliageBudgetTicks, tickBudgetWatch);
            timings.FoliageTicks = tickBudgetWatch.ElapsedTicks;

            tickBudgetWatch.Restart();
            treeGrowthScheduler.Tick(
                api,
                cfg,
                registry,
                spreadActiveChunks,
                treeCalendarAgeStore,
                CompleteTreeSenescenceRemoval);
            timings.TreeGrowthTicks = tickBudgetWatch.ElapsedTicks;

            tickBudgetWatch.Restart();
            ferntreeGrowthScheduler.Tick(
                api,
                cfg,
                registry,
                spreadActiveChunks,
                treeCalendarAgeStore,
                CompleteTreeSenescenceRemoval);
            timings.FerntreeGrowthTicks = tickBudgetWatch.ElapsedTicks;

            RunSpreadAndCommitPhase(cfg, now, spreadActiveChunks, spreadBudgetTicks, ref timings);

            timings.TotalTicks = Stopwatch.GetTimestamp() - tickStart;
            timings.SpreadSolveQueued = backgroundSpread.LastTickSolveQueued;
            timings.SpreadSolveCompleted = backgroundSpread.LastTickSolveCompleted;
            timings.SpreadSolveWorkerPending = backgroundSpread.WorkerPendingCount;
            ReproduceTickProfiler.MaybeLog(api, cfg, registry, timings, EcologyColumns);
        }

    }
}

