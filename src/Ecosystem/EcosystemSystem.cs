using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace WildFarming.Ecosystem
{
    public class EcosystemSystem
    {
        public static EcosystemSystem Instance { get; private set; }

        ICoreAPI api;
        readonly ReproducerRegistry registry = new ReproducerRegistry();
        readonly Queue<Vec2i> pendingChunkScans = new Queue<Vec2i>();

        long reproduceListenerId;
        long chunkScanListenerId;
        ChunkColumnLoadedDelegate chunkLoadedHandler;
        ChunkColumnUnloadDelegate chunkUnloadedHandler;
        readonly PendingTreeSaplings pendingTreeSaplings = new PendingTreeSaplings();
        bool calendarDebugLogged;
        internal FloraContextSampler FloraContext { get; private set; }

        BlockBrokenDelegate didBreakBlockHandler;

        public void InitPre(ICoreAPI api)
        {
            if (api.Side != EnumAppSide.Server) return;

            this.api = api;
            Instance = this;

            try
            {
                EcosystemConfig fromDisk = api.LoadModConfig<EcosystemConfig>("wildfarming-ecosystem.json");
                if (fromDisk != null)
                {
                    EcosystemConfig.Loaded = fromDisk;
                    if (EcosystemBalancePresets.IsKnownPreset(fromDisk.BalancePreset))
                    {
                        EcosystemBalancePresets.Apply(EcosystemConfig.Loaded, fromDisk.BalancePreset);
                    }
                }
                else
                {
                    EcosystemConfig cfg = EcosystemConfig.Loaded;
                    if (EcosystemBalancePresets.IsKnownPreset(cfg.BalancePreset))
                    {
                        EcosystemBalancePresets.Apply(cfg, cfg.BalancePreset);
                    }

                    api.StoreModConfig(cfg, "wildfarming-ecosystem.json");
                }
            }
            catch
            {
                EcosystemConfig cfg = EcosystemConfig.Loaded;
                if (EcosystemBalancePresets.IsKnownPreset(cfg.BalancePreset))
                {
                    EcosystemBalancePresets.Apply(cfg, cfg.BalancePreset);
                }

                api.StoreModConfig(cfg, "wildfarming-ecosystem.json");
            }
        }

        public void Init(ICoreAPI api)
        {
            if (api.Side != EnumAppSide.Server) return;

            this.api = api;
            Instance = this;

            if (!EcosystemConfig.Loaded.EcosystemEnabled) return;

            FloraContext = new FloraContextSampler();

            reproduceListenerId = api.Event.RegisterGameTickListener(OnReproduceTick, 2000);
            chunkScanListenerId = api.Event.RegisterGameTickListener(OnChunkScanTick, 500);

            if (api is ICoreServerAPI sapi)
            {
                chunkLoadedHandler = OnChunkColumnLoaded;
                chunkUnloadedHandler = OnChunkColumnUnloaded;
                sapi.Event.ChunkColumnLoaded += chunkLoadedHandler;
                sapi.Event.ChunkColumnUnloaded += chunkUnloadedHandler;

                didBreakBlockHandler = OnDidBreakBlock;
                sapi.Event.DidBreakBlock += didBreakBlockHandler;
            }

            WildFlowerClimate.LogMissingSpecies(api);
            WildTreeEcology.LogMissingWoods(api);
            WildBerryEcology.LogMissingTypes(api);
            WildFernEcology.LogMissingSpecies(api);
            WildTallgrassEcology.LogMissingSpecies(api);
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
                "[wildfarming] Calendar: {0} days/year, {1} h/day; spread base {2} attempts/year (calendar-scaled={3})",
                cal.DaysPerYear,
                cal.HoursPerDay,
                attemptsPerYear,
                cfg.UseCalendarScaledSpread);
        }

        public void Dispose()
        {
            if (api is ICoreServerAPI sapi)
            {
                if (chunkLoadedHandler != null) sapi.Event.ChunkColumnLoaded -= chunkLoadedHandler;
                if (chunkUnloadedHandler != null) sapi.Event.ChunkColumnUnloaded -= chunkUnloadedHandler;
                if (didBreakBlockHandler != null) sapi.Event.DidBreakBlock -= didBreakBlockHandler;
            }

            if (api != null)
            {
                if (reproduceListenerId != 0) api.Event.UnregisterGameTickListener(reproduceListenerId);
                if (chunkScanListenerId != 0) api.Event.UnregisterGameTickListener(chunkScanListenerId);
            }

            pendingChunkScans.Clear();
            reproduceListenerId = 0;
            chunkScanListenerId = 0;
            chunkLoadedHandler = null;
            chunkUnloadedHandler = null;
            didBreakBlockHandler = null;
            calendarDebugLogged = false;
            FloraContext?.Clear();
            FloraContext = null;
            Instance = null;
            api = null;
        }

        public EnvironmentalContext Sample(BlockPos plantPos) => EnvironmentalContext.Sample(api, plantPos);

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

        public void RemoveEcologyPlant(BlockPos pos, bool cascadeSymbiosis, string reason)
        {
            if (api == null || pos == null) return;

            IBlockAccessor acc = api.World.BlockAccessor;
            Block block = acc.GetBlock(pos);
            if (block.Id == 0) return;

            if (cascadeSymbiosis && EcosystemConfig.Loaded.EnableSymbiosis)
            {
                FloraSymbiosis.CascadeOnHostRemoved(api, pos, block);
            }

            registry.Remove(pos);
            acc.SetBlock(0, pos);
            acc.MarkBlockDirty(pos);
            FloraContext?.InvalidateAround(pos, 2);

            if (EcosystemConfig.Loaded.ReproduceDebug && !string.IsNullOrEmpty(reason))
            {
                api.Logger.Notification(
                    "[wildfarming] Removed {0} at {1} ({2})",
                    PlantCodeHelper.GetEcologySpecies(block.Code) ?? block.Code?.Path,
                    pos,
                    reason);
            }
        }

        public void RegisterReproducer(BlockPos origin, IEcosystemParticipant participant, bool spawnBurst = false)
        {
            if (participant == null || api == null) return;

            BlockPos anchor = PlantCodeHelper.GetReproduceAnchor(
                api.World.BlockAccessor, origin, participant.BlockCode);

            RegisterReproducer(
                anchor,
                participant.SpreadBlockCode,
                participant.MatureBlockCode,
                participant.Requirements,
                spawnBurst);
        }

        public void RegisterReproducer(
            BlockPos origin,
            AssetLocation spreadBlockCode,
            AssetLocation matureBlockCode,
            PlantRequirements requirements,
            bool spawnBurst = false)
        {
            if (api == null || api.Side != EnumAppSide.Server) return;
            if (!EcosystemConfig.Loaded.EcosystemEnabled) return;
            if (spreadBlockCode == null || matureBlockCode == null || requirements == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (cfg.OnlyActivateNearPlayers && !PlayerProximity.IsNearAnyPlayer(api, origin, cfg.PlayerActivationRadiusBlocks))
            {
                return;
            }

            try
            {
                Block matureBlock = api.World.BlockAccessor.GetBlock(origin);
                if (matureBlock == null || !EcosystemParticipant.TryFromBlock(matureBlock, out _)) return;

                Block spreadBlock = api.World.GetBlock(spreadBlockCode);
                if (spreadBlock == null) return;

                double now = api.World.Calendar.TotalHours;
                double nextAttempt = now;
                if (cfg.StaggerReproduceAttempts)
                {
                    double staggerSpan = SpeciesSpread.EffectiveIntervalHours(api, cfg, requirements);
                    nextAttempt = now + api.World.Rand.NextDouble() * staggerSpan;
                }

                var entry = new ReproducerEntry(
                    origin.Copy(),
                    spreadBlockCode.Clone(),
                    matureBlockCode,
                    requirements,
                    nextAttempt)
                {
                    EstablishedAtHours = now,
                };

                registry.Add(entry);

                if (cfg.ReproduceDebug)
                {
                    api.Logger.Notification(
                        "[wildfarming] Registered {0} at {1} spreadRate={2:0.##} interval={3:0.#}h chance={4:0.##} (registry {5})",
                        spreadBlockCode,
                        origin,
                        requirements.SpreadRate,
                        SpeciesSpread.EffectiveIntervalHours(api, cfg, requirements),
                        SpeciesSpread.EffectiveChance(cfg, requirements),
                        registry.Count);
                }

                if (spawnBurst)
                {
                    TrySpawnOffspring(entry, skipChanceRoll: true, maxSpawns: 3);
                }
            }
            catch (System.Exception ex)
            {
                api.Logger.Error("[wildfarming] RegisterReproducer failed at {0}: {1}", origin, ex);
            }
        }

        void OnChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks)
        {
            if (!EcosystemConfig.Loaded.EcosystemEnabled) return;
            pendingChunkScans.Enqueue(chunkCoord);
        }

        void OnChunkColumnUnloaded(Vec3i chunkCoord)
        {
            registry.RemoveChunk(new Vec2i(chunkCoord.X, chunkCoord.Z));
        }

        void OnDidBreakBlock(IServerPlayer byPlayer, int oldBlockId, BlockSelection blockSel)
        {
            if (blockSel?.Position == null || api == null) return;

            Block oldBlock = api.World.Blocks[oldBlockId];
            BlockPos pos = blockSel.Position;

            if (PlantCodeHelper.IsEcologySpreadParent(oldBlock) && EcosystemConfig.Loaded.EnableSymbiosis)
            {
                FloraSymbiosis.CascadeOnHostRemoved(api, pos, oldBlock);
            }

            if (FloraContextSampler.IsForestNeighborBlock(oldBlock)
                || (oldBlock != null && PlantCodeHelper.IsEcologyPlant(oldBlock)))
            {
                FloraContext?.InvalidateAround(pos, 2);
            }

            registry.Remove(pos);
        }

        void OnChunkScanTick(float dt)
        {
            if (!EcosystemConfig.Loaded.EcosystemEnabled || api == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            IBlockAccessor acc = api.World.BlockAccessor;
            int columnsLeft = cfg.MaxChunkColumnsScannedPerTick;
            int registrationsLeft = cfg.MaxRegistrationsPerTick;

            while (columnsLeft > 0 && pendingChunkScans.Count > 0 && registrationsLeft > 0)
            {
                Vec2i chunkCoord = pendingChunkScans.Dequeue();
                columnsLeft--;

                if (cfg.OnlyActivateNearPlayers && !PlayerProximity.ChunkNearAnyPlayer(api, chunkCoord, cfg.PlayerActivationRadiusBlocks))
                {
                    continue;
                }

                List<ChunkFlowerHit> hits = ChunkFlowerScanner.ScanColumn(chunkCoord, acc, registrationsLeft);
                foreach (ChunkFlowerHit hit in hits)
                {
                    if (registry.Contains(hit.Pos)) continue;

                    Block block = api.World.GetBlock(hit.BlockCode);
                    if (!EcosystemParticipant.TryFromBlock(block, out IEcosystemParticipant participant)) continue;

                    BlockPos anchor = PlantCodeHelper.GetReproduceAnchor(acc, hit.Pos, hit.BlockCode);
                    RegisterReproducer(anchor, participant, spawnBurst: false);
                    registrationsLeft--;
                    if (registrationsLeft <= 0) break;
                }
            }
        }

        void ProcessStress(double now, int maxChecks)
        {
            if (maxChecks <= 0 || api == null || !EcosystemConfig.Loaded.EnableStressDeath) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            bool harsh = cfg.HarshWildPlants;
            IBlockAccessor acc = api.World.BlockAccessor;

            registry.ProcessStress(
                maxChecks,
                entry =>
                {
                    if (entry.FailedSurvivalChecks > 0 && now < entry.NextStressCheckAt) return false;

                    PlantRequirements req = entry.Requirements;
                    if (req == null) return false;

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
                    else
                    {
                        entry.FailedSurvivalChecks = 0;
                        entry.NextStressCheckAt = 0;
                        return false;
                    }

                    if (entry.FailedSurvivalChecks < cfg.MaxFailedSurvivalChecks)
                    {
                        entry.NextStressCheckAt = now + cfg.StressRecheckHours;
                        return false;
                    }

                    return true;
                },
                pos => RemoveEcologyPlant(pos, cascadeSymbiosis: true, reason: "stress"));
        }

        void OnReproduceTick(float dt)
        {
            if (!EcosystemConfig.Loaded.EcosystemEnabled || api == null) return;

            TryLogCalendarDebugOnce();

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            double now = api.World.Calendar.TotalHours;
            ProcessStress(now, cfg.MaxStressChecksPerTick);
            pendingTreeSaplings.Process(api, this, now, cfg.MaxPendingTreeChecksPerTick);
            IBlockAccessor acc = api.World.BlockAccessor;

            registry.ProcessDue(
                now,
                cfg.MaxReproduceAttemptsPerTick,
                entry => SpeciesSpread.EffectiveIntervalHours(api, cfg, entry.Requirements),
                entry =>
                {
                    if (cfg.OnlyActivateNearPlayers && !PlayerProximity.IsNearAnyPlayer(api, entry.Origin, cfg.PlayerActivationRadiusBlocks))
                    {
                        return false;
                    }

                    Block block = acc.GetBlock(entry.Origin);
                    if (block.Id == 0 || !entry.IsMatureBlock(block))
                    {
                        return false;
                    }

                    TrySpawnOffspring(entry, skipChanceRoll: false, maxSpawns: 1);
                    return true;
                });
        }

        void TrySpawnOffspring(ReproducerEntry entry, bool skipChanceRoll, int maxSpawns)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;

            float chance = SpeciesSpread.EffectiveChance(cfg, entry.Requirements);
            if (!skipChanceRoll && api.World.Rand.NextDouble() > chance) return;

            Block spreadBlock = api.World.GetBlock(entry.JuvenileBlockCode);
            if (spreadBlock == null)
            {
                api.Logger.Warning("[wildfarming] Spread block not found: {0}", entry.JuvenileBlockCode);
                return;
            }

            BlockPos spreadOrigin = PlantCodeHelper.GetReproduceAnchor(
                api.World.BlockAccessor, entry.Origin, entry.MatureBlockCode);

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
                cfg.ReproduceDebug,
                out string failureReason,
                onPlaced: (pos, requirements, displaced) =>
                {
                    if (requirements.Habitat == EcologyHabitat.TerrestrialTree)
                    {
                        pendingTreeSaplings.Add(pos, requirements.Species, api.World.Calendar.TotalHours);
                    }
                });

            if (cfg.ReproduceDebug && spawned == 0 && failureReason != null)
            {
                api.Logger.Notification("[wildfarming] No spread near {0}: {1}", entry.Origin, failureReason);
            }
        }
    }
}
