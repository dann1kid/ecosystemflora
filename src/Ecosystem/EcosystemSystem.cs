using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace WildFarming.Ecosystem
{
    public class EcosystemSystem
    {
        public static EcosystemSystem Instance { get; private set; }

        ICoreAPI api;
        readonly ReproducerRegistry registry = new ReproducerRegistry();
        readonly Queue<PendingChunkScan> pendingChunkScans = new Queue<PendingChunkScan>();
        readonly HashSet<long> pendingChunkScanQueued = new HashSet<long>();

        /// <summary>Cap inactive-chunk rotations per tick (avoids O(queue) spin when queue is huge).</summary>
        const int MaxChunkScanDequeAttemptsPerTick = 128;

        long reproduceListenerId;
        long stressListenerId;
        long chunkScanListenerId;
        ChunkColumnLoadedDelegate chunkLoadedHandler;
        ChunkColumnUnloadDelegate chunkUnloadedHandler;
        readonly PendingTreeSaplings pendingTreeSaplings = new PendingTreeSaplings();
        readonly CyclicTreeTrunkScanner cyclicTreeScanner = new CyclicTreeTrunkScanner();
        readonly TreeGrowthScheduler treeGrowthScheduler = new TreeGrowthScheduler();
        readonly FerntreeGrowthScheduler ferntreeGrowthScheduler = new FerntreeGrowthScheduler();
        readonly TreeCalendarAgeStore treeCalendarAgeStore = new TreeCalendarAgeStore();
        readonly FoliageCellScheduler foliageCells = new FoliageCellScheduler();
        bool calendarDebugLogged;
        bool deferredTreeBootstrapDone;
        int foliageBootstrapPasses;
        bool foliageStartupLogged;
        internal FoliageCellScheduler FoliageCells => foliageCells;
        internal FloraContextSampler FloraContext { get; private set; }
        internal NicheSampler Niche { get; private set; }
        internal EcologySpacingIndex SpacingIndex { get; private set; }
        internal EnvironmentalColumnCache ColumnCache { get; private set; }
        readonly List<Vec2i> activeChunkScratch = new List<Vec2i>();
        readonly Stopwatch tickBudgetWatch = new Stopwatch();

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

            reproduceListenerId = api.Event.RegisterGameTickListener(OnReproduceTick, 2000);
            int stressInterval = EcosystemConfig.Loaded.StressTickIntervalMs > 0
                ? EcosystemConfig.Loaded.StressTickIntervalMs : 6000;
            stressListenerId = api.Event.RegisterGameTickListener(OnStressTick, stressInterval);
            chunkScanListenerId = api.Event.RegisterGameTickListener(OnChunkScanTick, 2000);

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
            }

            WildFlowerClimate.LogMissingSpecies(api);
            WildTreeEcology.LogMissingWoods(api);
            WildBerryEcology.LogMissingTypes(api);
            WildFernEcology.LogMissingSpecies(api);
            WildTallgrassEcology.LogMissingSpecies(api);
            WildGrassColonizerEcology.LogMissingSpecies(api);
            WildShoreSedgeEcology.LogMissingSpecies(api);
            WildDesertEcology.LogMissingSpecies(api);
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
            HashSet<long> chunks = PlayerProximity.BuildActivePlayerChunks(api, cfg.PlayerActivationRadiusBlocks);
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

            IBlockAccessor acc = api.World.BlockAccessor;
            foreach (long key in chunkKeys)
            {
                int cx = (int)(key >> 32);
                int cz = (int)(key & 0xFFFFFFFF);
                if (acc.GetMapChunk(cx, cz) == null) continue;

                var coord = new Vec2i(cx, cz);
                EnqueueChunkScan(coord);
                foliageCells.ScheduleChunkSync(api, coord);
            }
        }

        void BootstrapTreeSpreadForLoadedChunks()
        {
            if (api == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            HashSet<long> chunks = PlayerProximity.BuildActivePlayerChunks(api, cfg.PlayerActivationRadiusBlocks);
            IBlockAccessor acc = api.World.BlockAccessor;
            int left = cfg.MaxRegistrationsPerTick * 4;
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
                "[ecosystemflora] Calendar: {0} days/year, {1} h/day; spread base {2} attempts/year (calendar-scaled={3})",
                cal.DaysPerYear,
                cal.HoursPerDay,
                attemptsPerYear,
                cfg.UseCalendarScaledSpread);
        }

        public void Dispose()
        {
            if (api is ICoreServerAPI sapi)
            {
                treeCalendarAgeStore.Unbind(sapi);
                treeCalendarAgeStore.Clear();

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

            pendingChunkScans.Clear();
            pendingChunkScanQueued.Clear();
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
            treeGrowthScheduler.Clear();
            ferntreeGrowthScheduler.Clear();
            cyclicTreeScanner.Clear();
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
                FloraSymbiosis.CascadeOnHostRemoved(api, pos, block);
            }

            string species = PlantCodeHelper.ResolveEcologySpecies(block);
            if (!string.IsNullOrEmpty(species))
            {
                SoilSuccessionApplier.Apply(api, pos, species, soilEvent);
            }

            registry.Remove(pos);
            SpacingIndex?.Remove(pos);
            acc.SetBlock(0, pos);
            acc.MarkBlockDirty(pos);
            FloraContext?.InvalidateAround(pos, 2);
            InvalidateEnvironmentAround(pos);

            if (EcosystemConfig.Loaded.VerboseLogging && EcosystemConfig.Loaded.ReproduceDebug && !string.IsNullOrEmpty(reason))
            {
                api.Logger.Notification(
                    "[ecosystemflora] Removed {0} at {1} ({2})",
                    PlantCodeHelper.ResolveEcologySpecies(block) ?? block.Code?.Path,
                    pos,
                    reason);
            }
        }

        public void RegisterReproducer(BlockPos origin, IEcosystemParticipant participant, bool spawnBurst = false, bool playerPlaced = false)
        {
            if (participant == null || api == null) return;

            BlockPos anchor = PlantCodeHelper.GetReproduceAnchor(
                api.World.BlockAccessor, origin, participant.BlockCode);

            RegisterReproducer(
                anchor,
                participant.SpreadBlockCode,
                participant.MatureBlockCode,
                participant.Requirements,
                spawnBurst,
                playerPlaced);
        }

        public void RegisterReproducer(
            BlockPos origin,
            AssetLocation spreadBlockCode,
            AssetLocation matureBlockCode,
            PlantRequirements requirements,
            bool spawnBurst = false,
            bool playerPlaced = false)
        {
            if (api == null || api.Side != EnumAppSide.Server) return;
            if (!EcosystemConfig.Loaded.EcosystemEnabled) return;
            if (spreadBlockCode == null || matureBlockCode == null || requirements == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (requirements.Habitat == EcologyHabitat.Ferntree && !cfg.EnableFerntreeEcology) return;
            if (requirements.Habitat == EcologyHabitat.WildVine && !cfg.EnableWildVineEcology) return;
            if (cfg.OnlyActivateNearPlayers && !PlayerProximity.IsNearAnyPlayer(api, origin, cfg.PlayerActivationRadiusBlocks))
            {
                return;
            }

            try
            {
                Block matureBlock = api.World.BlockAccessor.GetBlock(origin);
                if (matureBlock == null || !EcosystemParticipant.TryFromBlock(matureBlock, out _))
                {
                    if (cfg.ReproduceDebug && requirements.Habitat == EcologyHabitat.TerrestrialTree)
                    {
                        api.Logger.Warning(
                            "[ecosystemflora] Tree not registrable at {0}: block={1}",
                            origin,
                            matureBlock?.Code);
                    }
                    return;
                }

                Block spreadBlock = api.World.GetBlock(spreadBlockCode);
                if (spreadBlock == null)
                {
                    if (cfg.ReproduceDebug && requirements.Habitat == EcologyHabitat.TerrestrialTree)
                    {
                        api.Logger.Warning(
                            "[ecosystemflora] Tree spread block missing: {0} (origin {1})",
                            spreadBlockCode,
                            origin);
                    }
                    return;
                }

                double now = api.World.Calendar.TotalHours;
                double nextAttempt = now;
                if (cfg.StaggerReproduceAttempts)
                {
                    double staggerSpan = SpeciesSpread.EffectiveIntervalHours(api, origin, cfg, requirements);
                    nextAttempt = now + api.World.Rand.NextDouble() * staggerSpan;
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

                registry.Add(entry);
                SpacingIndex?.AddOrUpdate(api.World.BlockAccessor, origin);
                if (!playerPlaced)
                    SoilSuccessionApplier.Apply(api, origin, requirements.Species, SoilSuccessionEvent.Spread);

                if (requirements.Habitat == EcologyHabitat.TerrestrialTree
                    && PlantCodeHelper.IsTreeLogGrownBlock(matureBlock))
                {
                    string wood = PlantCodeHelper.GetTreeWood(matureBlock);
                    if (!treeCalendarAgeStore.TryRestore(entry, origin, wood))
                    {
                        entry.TreeAgeYears = 0;
                        entry.LastTreeGrowthYear = CanopyEcology.GameYear(api.World.Calendar);
                    }

                    treeCalendarAgeStore.Capture(entry, wood);
                    foliageCells.OnBlockAdded(origin);
                }
                else if (requirements.Habitat == EcologyHabitat.Ferntree
                         && PlantCodeHelper.IsFerntreeTrunkBlock(matureBlock)
                         && cfg.EnableFerntreeEcology)
                {
                    if (!treeCalendarAgeStore.TryRestore(entry, origin, WildFerntreeEcology.Species))
                    {
                        entry.TreeAgeYears = 0;
                        entry.LastTreeGrowthYear = CanopyEcology.GameYear(api.World.Calendar);
                    }

                    treeCalendarAgeStore.Capture(entry, WildFerntreeEcology.Species);
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
            }
            catch (System.Exception ex)
            {
                api.Logger.Error("[ecosystemflora] RegisterReproducer failed at {0}: {1}", origin, ex);
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
            if (!EcosystemConfig.Loaded.EcosystemEnabled) return;
            EnqueueChunkScan(chunkCoord);
            foliageCells.ScheduleChunkSync(api, chunkCoord);
            MyceliumChunkRegistrar.ScheduleScanColumn(api, chunkCoord);
        }

        void EnqueueChunkScan(Vec2i chunkCoord, int nextLx = 0, int nextLz = 0, int nextY = -1)
        {
            long key = ChunkScanKey(chunkCoord.X, chunkCoord.Y);
            if (nextLx == 0 && nextLz == 0 && nextY < 0 && !pendingChunkScanQueued.Add(key)) return;

            pendingChunkScans.Enqueue(new PendingChunkScan(chunkCoord, nextLx, nextLz, nextY));
        }

        static long ChunkScanKey(int cx, int cz) => ((long)cx << 32) | (uint)cz;

        void OnChunkColumnUnloaded(Vec3i chunkCoord)
        {
            var cc = new Vec2i(chunkCoord.X, chunkCoord.Z);
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
            api.Event.RegisterCallback(_ => TryRegisterPlacedBlock(placed), 0);
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

            if (PlantCodeHelper.IsTreeSaplingBlock(block))
            {
                string wood = PlantCodeHelper.GetTreeWood(block);
                if (!string.IsNullOrEmpty(wood))
                {
                    pendingTreeSaplings.Add(pos, wood, api.World.Calendar.TotalHours);
                }

                return;
            }

            if (!EcosystemParticipant.TryFromBlock(block, out IEcosystemParticipant participant)) return;

            BlockPos anchor = PlantCodeHelper.GetReproduceAnchor(api.World.BlockAccessor, pos, block.Code);
            if (registry.Contains(anchor)) return;

            RegisterReproducer(anchor, participant, playerPlaced: true);
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

            if (PlantCodeHelper.IsEcologySpreadParent(oldBlock) && EcosystemConfig.Loaded.EnableSymbiosis)
            {
                FloraSymbiosis.CascadeOnHostRemoved(api, pos, oldBlock);
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
                    api.World.BlockAccessor, pos, WildFerntreeEcology.Species);
            }

            if (CanopyFoliageRules.IsSeasonalFoliageBlock(oldBlock))
            {
                foliageCells.OnBlockRemoved(pos);
            }

            if (WildSoilGroundRules.HasActiveMycelium(api.World.BlockAccessor, pos))
            {
                registry.Remove(pos);
                SpacingIndex?.Remove(pos);
                InvalidateEnvironmentAround(pos);
                return;
            }

            if (FloraContextSampler.IsForestNeighborBlock(oldBlock)
                || (oldBlock != null && PlantCodeHelper.IsEcologyPlant(oldBlock)))
            {
                FloraContext?.InvalidateAround(pos, 2);
            }

            registry.Remove(pos);
            SpacingIndex?.Remove(pos);
            InvalidateEnvironmentAround(pos);
        }

        void OnChunkScanTick(float dt)
        {
            if (!EcosystemConfig.Loaded.EcosystemEnabled || api == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            IBlockAccessor acc = api.World.BlockAccessor;
            int columnsLeft = cfg.MaxChunkColumnsScannedPerTick;
            int registrationsLeft = cfg.MaxRegistrationsPerTick;
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
                            EnqueueChunkScan(new Vec2i(cx, cz));
                        }
                    }
                }
            }

            int queuePasses = pendingChunkScans.Count;
            if (queuePasses > MaxChunkScanDequeAttemptsPerTick)
            {
                queuePasses = MaxChunkScanDequeAttemptsPerTick;
            }

            bool syncFoliage = cfg.EnableSeasonalFoliage && FoliageSyncModeHelper.UsesChunkSync(cfg);
            int passBudgetMs = cfg.ResolveRegistrationBudgetMs();
            if (cfg.FoliageChunkSyncBudgetMs > passBudgetMs)
            {
                passBudgetMs = cfg.FoliageChunkSyncBudgetMs;
            }

            long passDeadline = passBudgetMs > 0
                ? System.Diagnostics.Stopwatch.GetTimestamp()
                    + passBudgetMs * System.Diagnostics.Stopwatch.Frequency / 1000
                : long.MaxValue;

            FoliageCellIndex foliageIndex = FoliageSyncModeHelper.UsesRandomTick(cfg)
                ? foliageCells.Index
                : null;

            while (queuePasses > 0 && columnsLeft > 0)
            {
                if (budgetTicks > 0 && tickBudgetWatch.ElapsedTicks >= budgetTicks) break;

                PendingChunkScan job = pendingChunkScans.Dequeue();
                queuePasses--;

                if (activePlayerChunks != null && !PlayerProximity.IsActiveChunk(activePlayerChunks, job.ChunkCoord))
                {
                    pendingChunkScans.Enqueue(job);
                    continue;
                }

                if (registrationsLeft <= 0 && !(syncFoliage && foliageCells.NeedsChunkSync(job.ChunkCoord, seasonKey)))
                {
                    pendingChunkScans.Enqueue(job);
                    continue;
                }

                columnsLeft--;
                long chunkKey = ChunkScanKey(job.ChunkCoord.X, job.ChunkCoord.Y);

                ChunkEcologyColumnPass.Result pass = ChunkEcologyColumnPass.Run(
                    api,
                    acc,
                    job.ChunkCoord,
                    new ChunkEcologyColumnPass.Request
                    {
                        MaxFlowerHits = registrationsLeft,
                        MaxTreeHits = registrationsLeft,
                        MaxVineHits = cfg.EnableWildVineEcology ? registrationsLeft : 0,
                        SyncFoliage = syncFoliage,
                        FoliageIndex = foliageIndex,
                    },
                    job.NextLx,
                    job.NextLz,
                    job.NextY,
                    (basePos, wood) => TryRegisterDiscoveredTree(acc, basePos, ref registrationsLeft),
                    passDeadline);

                if (pass.FlowerHits != null)
                {
                    for (int i = 0; i < pass.FlowerHits.Count; i++)
                    {
                        ChunkFlowerHit hit = pass.FlowerHits[i];
                        Block block = acc.GetBlock(hit.Pos);

                        if (registry.Contains(hit.Pos)) continue;

                        if (!EcosystemParticipant.TryFromBlock(block, out IEcosystemParticipant participant)) continue;

                        BlockPos anchor = PlantCodeHelper.GetReproduceAnchor(acc, hit.Pos, hit.BlockCode);
                        if (registry.Contains(anchor)) continue;

                        RegisterReproducer(anchor, participant, spawnBurst: false);
                        registrationsLeft--;
                        if (registrationsLeft <= 0) break;
                    }
                }

                if (registrationsLeft > 0 && pass.VineHits != null)
                {
                    for (int i = 0; i < pass.VineHits.Count; i++)
                    {
                        ChunkFlowerHit hit = pass.VineHits[i];
                        Block block = acc.GetBlock(hit.Pos);

                        if (registry.Contains(hit.Pos)) continue;
                        if (!WildVineHelper.IsEndBlock(block)) continue;
                        if (!EcosystemParticipant.TryFromBlock(block, out IEcosystemParticipant participant)) continue;

                        RegisterReproducer(hit.Pos, participant, spawnBurst: false);
                        registrationsLeft--;
                        if (registrationsLeft <= 0) break;
                    }
                }

                if (syncFoliage)
                {
                    foliageCells.ApplyEcologyPassResult(job.ChunkCoord, pass, seasonKey);

                    if (pass.FoliageChanged > 0)
                    {
                        int cs = GlobalConstants.ChunkSize;
                        var invalidateAt = new BlockPos(
                            job.ChunkCoord.X * cs + 8,
                            64,
                            job.ChunkCoord.Y * cs + 8);
                        FloraContext?.InvalidateAround(invalidateAt, 3);
                        InvalidateEnvironmentAround(invalidateAt);
                    }
                }

                if (!pass.Completed)
                {
                    EnqueueChunkScan(job.ChunkCoord, pass.ResumeLx, pass.ResumeLz, pass.ResumeY);
                }
                else
                {
                    pendingChunkScanQueued.Remove(chunkKey);
                }
            }

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

            tickBudgetWatch.Stop();
        }

        bool TryRegisterDiscoveredTree(IBlockAccessor acc, BlockPos basePos, ref int registrationsLeft)
        {
            if (registrationsLeft <= 0 || basePos == null) return false;
            if (registry.Contains(basePos)) return false;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            Block block = acc.GetBlock(basePos);
            if (PlantCodeHelper.IsFerntreeTrunkBlock(block) && !cfg.EnableFerntreeEcology) return false;

            if (!EcosystemParticipant.TryFromBlock(block, out IEcosystemParticipant participant)) return false;

            RegisterReproducer(basePos, participant, spawnBurst: false);
            registrationsLeft--;
            return true;
        }

        readonly HashSet<BlockPos> trampledScratch = new HashSet<BlockPos>();
        readonly PlayerProximity.Snapshot tramplingSnapshot = new PlayerProximity.Snapshot();

        void ProcessStress(double now, int maxChecks, ICollection<Vec2i> activeChunks, long budgetTicks = 0)
        {
            if (maxChecks <= 0 || api == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EnableStressDeath && !cfg.EnableMyceliumEcology) return;
            IBlockAccessor acc = api.World.BlockAccessor;
            trampledScratch.Clear();

            bool trampling = cfg.EnableTrampling;
            int tramplingRadius = cfg.TramplingRadius;
            if (trampling) tramplingSnapshot.Refresh(api, tramplingRadius);

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
                    else if (trampling
                        && tramplingSnapshot.IsNearChunk(entry.Origin)
                        && tramplingSnapshot.IsNear(entry.Origin, tramplingRadius))
                    {
                        entry.TramplingExposure++;
                        if (entry.TramplingExposure >= cfg.TramplingStressThreshold)
                        {
                            entry.FailedSurvivalChecks++;
                        }
                        entry.NextStressCheckAt = now + cfg.StressRecheckHours;
                        if (entry.FailedSurvivalChecks < cfg.MaxFailedSurvivalChecks) return false;
                        trampledScratch.Add(entry.Origin);
                        return true;
                    }
                    else
                    {
                        if (entry.TramplingExposure > 0) entry.TramplingExposure--;
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
                        RemoveMyceliumAnchor(pos, trampledScratch.Remove(pos) ? "trampled" : "mycelium-stress");
                        return;
                    }

                    bool trampled = trampledScratch.Remove(pos);
                    RemoveEcologyPlant(pos, cascadeSymbiosis: true,
                        reason: trampled ? "trampled" : "stress",
                        soilEvent: trampled && cfg.TramplingSoilDegradation
                            ? SoilSuccessionEvent.Trampled
                            : SoilSuccessionEvent.Death);
                },
                activeChunks);
        }

        ICollection<Vec2i> BuildActiveRegistryChunks(EcosystemConfig cfg)
        {
            if (!cfg.OnlyActivateNearPlayers) return null;

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

            tickBudgetWatch.Restart();

            pendingTreeSaplings.Process(api, this, now, cfg.MaxPendingTreeChecksPerTick);

            tickBudgetWatch.Restart();

            long foliageBudgetTicks = cfg.FoliageBudgetMs > 0
                ? cfg.FoliageBudgetMs * Stopwatch.Frequency / 1000
                : 0;

            foliageCells.ProcessRandomTick(api, canopyActiveChunks, foliageBudgetTicks, tickBudgetWatch);

            treeGrowthScheduler.Tick(
                api,
                cfg,
                registry,
                spreadActiveChunks,
                treeCalendarAgeStore,
                CompleteTreeSenescenceRemoval);

            ferntreeGrowthScheduler.Tick(
                api,
                cfg,
                registry,
                spreadActiveChunks,
                treeCalendarAgeStore,
                CompleteTreeSenescenceRemoval);

            IBlockAccessor acc = api.World.BlockAccessor;

            if (spreadActiveChunks == null || spreadActiveChunks.Count > 0)
            {
                tickBudgetWatch.Restart();

                registry.ProcessDue(
                    now,
                    cfg.MaxReproduceAttemptsPerTick,
                    entry => entry.Requirements?.Habitat == EcologyHabitat.MyceliumAnchor
                        ? MyceliumSpreadTiming.EffectiveIntervalHours(api, cfg, entry.Requirements)
                        : SpeciesSpread.EffectiveIntervalHours(api, entry.Origin, cfg, entry.Requirements),
                    entry =>
                    {
                        Block block = acc.GetBlock(entry.Origin);
                        if (entry.Requirements?.Habitat == EcologyHabitat.MyceliumAnchor)
                        {
                            if (!WildSoilGroundRules.HasActiveMycelium(acc, entry.Origin))
                            {
                                return false;
                            }

                            if (cfg.EnableMyceliumNetworkSpread)
                            {
                                MyceliumNetworkSpread.TrySpread(
                                    this,
                                    entry,
                                    cfg.VerboseLogging && cfg.ReproduceDebug);
                            }

                            return true;
                        }

                        if (entry.Requirements?.Habitat == EcologyHabitat.WildVine)
                        {
                            if (!cfg.EnableWildVineEcology || !WildVineHelper.IsEndBlock(block))
                            {
                                return false;
                            }

                            float vineChance = SpeciesSpread.EffectiveChance(api, entry.Origin, cfg, entry.Requirements);
                            if (api.World.Rand.NextDouble() > vineChance) return true;

                            WildVineSpread.TrySpread(this, entry, api, cfg);
                            return true;
                        }

                        if (block.Id == 0 || !entry.IsMatureBlock(block))
                        {
                            return false;
                        }

                        TrySpawnOffspring(entry, skipChanceRoll: false, maxSpawns: 1);
                        return true;
                    },
                    spreadActiveChunks,
                    spreadBudgetTicks,
                    tickBudgetWatch);
            }

            tickBudgetWatch.Stop();
        }

        void TrySpawnOffspring(ReproducerEntry entry, bool skipChanceRoll, int maxSpawns)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;

            if (entry.Requirements?.Habitat == EcologyHabitat.WildVine) return;

            if (TreeSenescence.SuppressesSpread(entry, cfg)) return;

            BlockPos spreadOrigin = PlantCodeHelper.GetReproduceAnchor(
                api.World.BlockAccessor, entry.Origin, entry.MatureBlockCode);

            float chance = SpeciesSpread.EffectiveChance(api, spreadOrigin, cfg, entry.Requirements);
            if (!skipChanceRoll && api.World.Rand.NextDouble() > chance) return;

            Block spreadBlock = api.World.GetBlock(entry.JuvenileBlockCode);
            if (spreadBlock == null)
            {
                if (EcosystemConfig.Loaded.VerboseLogging)
                    api.Logger.Warning("[ecosystemflora] Spread block not found: {0}", entry.JuvenileBlockCode);
                return;
            }

            int spawned = ReproducePlacement.TryPlaceSpreadAmongNeighbors(
                api,
                spreadOrigin,
                spreadBlock,
                entry.Requirements,
                cfg.MinFitness,
                cfg.HarshWildPlants,
                cfg.ReproduceRadius,
                cfg.ReproduceVerticalSearch,
                maxSpawns,
                api.World.Rand,
                cfg.VerboseLogging && cfg.ReproduceDebug,
                out string failureReason,
                onPlaced: (pos, requirements, displaced) =>
                {
                    if (requirements.Habitat == EcologyHabitat.TerrestrialTree)
                    {
                        pendingTreeSaplings.Add(pos, requirements.Species, api.World.Calendar.TotalHours);
                        return;
                    }

                    if (requirements.Habitat == EcologyHabitat.Ferntree)
                    {
                        BlockPos basePos = FerntreeStructure.GetTrunkBase(api.World.BlockAccessor, pos);
                        Block trunkBlock = api.World.BlockAccessor.GetBlock(basePos);
                        if (EcosystemParticipant.TryFromBlock(trunkBlock, out IEcosystemParticipant ferntreeParticipant))
                        {
                            RegisterReproducer(basePos, ferntreeParticipant, spawnBurst: false);
                        }

                        InvalidateEnvironmentAround(basePos);
                        return;
                    }

                    Block placed = api.World.BlockAccessor.GetBlock(pos);
                    if (EcosystemParticipant.TryFromBlock(placed, out IEcosystemParticipant participant))
                    {
                        RegisterReproducer(pos, participant, spawnBurst: false);
                    }

                    InvalidateEnvironmentAround(pos);
                });

            if (cfg.VerboseLogging && cfg.ReproduceDebug && spawned == 0 && failureReason != null)
            {
                api.Logger.Notification("[ecosystemflora] No spread near {0}: {1}", entry.Origin, failureReason);
            }
        }
    }
}
