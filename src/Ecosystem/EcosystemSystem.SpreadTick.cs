using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Spread-tick coordinator for <see cref="EcosystemSystem"/>. This partial holds the reproduce
    /// spread phase, offspring spawning, the two-phase commit handlers, and attempt recording. It is
    /// kept as a partial of EcosystemSystem (rather than a free-standing type) because the spread path
    /// is woven through the shared reproducer registry and the public RegisterReproducer spawn-burst
    /// entry point; splitting the file isolates the concern without destabilizing that core coupling.
    /// </summary>
    public partial class EcosystemSystem
    {
        void RunSpreadAndCommitPhase(
            EcosystemConfig cfg,
            double now,
            System.Collections.Generic.ICollection<Vec2i> spreadActiveChunks,
            long spreadBudgetTicks,
            ref ReproduceTickTimings timings)
        {
            IBlockAccessor acc = api.World.BlockAccessor;

            spreadCooldown.ResetTick();
            spreadMatModeScratch.Clear();

            if (cfg.EnableBackgroundSpreadSolve)
            {
                backgroundSpread.PollCompleted(
                    this,
                    cfg.VerboseLogging && cfg.ReproduceDebug,
                    cfg.ResolveMaxSpreadSolveDrainPerTick());
            }

            // Shared placement budget with two-phase commits: vine/mycelium SetBlock outside
            // PendingSpreadQueue must not consume an uncapped share of the reproduce tick.
            int placementBudget = cfg.MaxSpreadCommitsPerTick > 0
                ? cfg.MaxSpreadCommitsPerTick
                : cfg.MaxReproduceAttemptsPerTick;
                if (placementBudget <= 0) placementBudget = 14;

            if (spreadActiveChunks == null || spreadActiveChunks.Count > 0)
            {
                tickBudgetWatch.Restart();

                System.Func<ReproducerEntry, bool> trySpread = entry =>
                {
                    PlantRequirements req = entry.Requirements;

                    // Entry-only gates before GetBlock / capture (senescence, phenology, zero rate).
                    if (req?.Habitat != EcologyHabitat.MyceliumAnchor
                        && SpreadGateChain.PreSpawn.BlocksSpread(api, entry, cfg))
                    {
                        return true;
                    }

                    // Near-zero seasonal activity: interval already stretches ×20; skip live work this due.
                    if (cfg.UseSeasonalEcology
                        && req != null
                        && req.Habitat != EcologyHabitat.MyceliumAnchor
                        && SeasonEcology.SpreadActivityMultiplier(api, entry.Origin, req) <= 0.05f)
                    {
                        return true;
                    }

                    Block block = acc.GetBlock(entry.Origin);
                    if (req?.Habitat == EcologyHabitat.MyceliumAnchor)
                    {
                        if (!WildSoilGroundRules.HasActiveMycelium(acc, entry.Origin))
                        {
                            return false;
                        }

                        // Chance runs inside TrySpread after cheap frontier reject.
                        if (cfg.EnableMyceliumNetworkSpread
                            && placementBudget > 0
                            && MyceliumNetworkSpread.TrySpread(
                                this,
                                entry,
                                cfg.VerboseLogging && cfg.ReproduceDebug))
                        {
                            placementBudget--;
                        }

                        return true;
                    }

                    if (req?.Habitat == EcologyHabitat.WildVine)
                    {
                        if (!cfg.EnableWildVineEcology || !WildVineHelper.IsVineBlock(block))
                        {
                            return false;
                        }

                        float vineChance = SpeciesSpread.EffectiveChance(api, entry.Origin, cfg, req);
                        if (api.World.Rand.NextDouble() > vineChance) return true;

                        if (placementBudget > 0 && WildVineSpread.TrySpread(this, entry, api, cfg))
                        {
                            placementBudget--;
                        }

                        return true;
                    }

                    if (block.Id == 0)
                    {
                        return false;
                    }

                    if (!entry.IsRegisteredPlantBlock(block))
                    {
                        return entry.MatchesRegisteredSpecies(block);
                    }

                    if (req?.Species == "tallgrass"
                        && TallgrassSpreadMaturation.UsesMaturation(cfg)
                        && !TallgrassSpreadMaturation.CanReproduceFrom(block, api, entry.Origin))
                    {
                        return true;
                    }

                    // Chance before capture / two-phase enqueue (already passed PreSpawn above).
                    BlockPos anchor = PlantCodeHelper.GetReproduceAnchor(
                        acc, entry.Origin, entry.MatureBlockCode);
                    float chance = SpeciesSpread.EffectiveChance(api, anchor, cfg, req);
                    if (api.World.Rand.NextDouble() > chance)
                    {
                        BlockPos anchorCopy = anchor.Copy();
                        MatSpreadCollectMode matMode = SpreadAttemptInspect.ResolveCollectMode(
                            req, api.World.Rand);
                        spreadMatModeScratch[anchorCopy] = matMode;
                        RecordSpreadAttempt(anchorCopy, entry, matMode, placed: false, "Chance roll failed");
                        spreadCooldown.ApplyFailedChanceRollOnce(anchorCopy, req);
                        return true;
                    }

                    TrySpawnOffspring(entry, skipChanceRoll: true, maxSpawns: 1);
                    return true;
                };

                System.Func<ReproducerEntry, double> spreadIntervalHours = entry =>
                {
                    double interval = entry.Requirements?.Habitat == EcologyHabitat.MyceliumAnchor
                        ? MyceliumSpreadTiming.EffectiveIntervalHours(api, cfg, entry.Requirements)
                        : SpeciesSpread.EffectiveIntervalHours(api, entry.Origin, cfg, entry.Requirements);
                    return TreeSpreadMaturity.RescheduleIntervalHours(api, entry, cfg, interval);
                };

                if (cfg.EnableChunkFairSpread)
                {
                    timings.SpreadProcessed = registry.ProcessDueChunkFair(
                        cfg,
                        now,
                        cfg.MaxReproduceAttemptsPerTick,
                        spreadActiveChunks,
                        spreadIntervalHours,
                        trySpread,
                        spreadBudgetTicks,
                        tickBudgetWatch);
                }
                else
                {
                    timings.SpreadProcessed = registry.ProcessDue(
                        now,
                        cfg.MaxReproduceAttemptsPerTick,
                        spreadIntervalHours,
                        trySpread,
                        spreadActiveChunks,
                        spreadBudgetTicks,
                        tickBudgetWatch,
                        cfg.EnableEventDrivenSpread);
                }

                timings.CollectDueTicks = registry.LastCollectDueTicks;
                timings.SpreadProcessTicks = registry.LastProcessDueTicks;
                timings.DueQueueSize = registry.LastDueQueueSize;
                timings.SpreadChunksVisited = registry.LastSpreadChunksVisited;
                timings.SpreadMaxAttemptsInChunk = registry.LastSpreadMaxAttemptsInChunk;
                timings.WakeDrivenAttempts = registry.LastWakeDrivenAttempts;
                timings.CalendarDrivenAttempts = registry.LastCalendarDrivenAttempts;
            }

            if (cfg.EnableTwoPhaseSpreadPlacement && pendingSpreadQueue.Count > 0)
            {
                tickBudgetWatch.Restart();

                timings.SpreadCommitted = pendingSpreadQueue.ProcessCommit(
                    api,
                    cfg,
                    intent => OnSpreadPlaced(
                        intent.TargetPos,
                        intent.Requirements,
                        intent.Displacing,
                        intent.ParentOrigin),
                    placementBudget,
                    spreadBudgetTicks,
                    tickBudgetWatch,
                    cfg.VerboseLogging && cfg.ReproduceDebug,
                    onDropped: OnSpreadCommitDropped);

                timings.SpreadCommitTicks = tickBudgetWatch.ElapsedTicks;
                timings.PendingSpreadQueueSize = pendingSpreadQueue.Count;
            }
        }

        void OnSpreadPlaced(BlockPos pos, PlantRequirements requirements, bool displaced, BlockPos spreadOrigin)
        {
            RecordSpreadAttempt(spreadOrigin, requirements, placed: true);

            if (requirements.Habitat == EcologyHabitat.TerrestrialTree)
            {
                // Wild tree spread places a log-grown seedling; register immediately (no sapling queue).
                BlockPos basePos = PlantCodeHelper.GetTreeTrunkBase(api.World.BlockAccessor, pos);
                Block trunkBlock = api.World.BlockAccessor.GetBlock(basePos);
                if (EcosystemParticipant.TryFromBlock(trunkBlock, out IEcosystemParticipant treeParticipant))
                {
                    RegisterReproducer(basePos, treeParticipant, spawnBurst: false);
                }

                InvalidateEnvironmentAround(basePos);
                if (spreadOrigin != null)
                {
                    WakeEcologyAround(spreadOrigin);
                }

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

            spreadCooldown.ApplyOnCommit(spreadOrigin, requirements);

            Block placed = api.World.BlockAccessor.GetBlock(pos);
            if (FernSpreadMaturation.ShouldQueueMaturation(placed, requirements))
            {
                double nowHours = api.World.Calendar.TotalHours;
                AssetLocation matureCode = FernJuvenileBlocks.ResolveMatureCode(
                    api, spreadOrigin, requirements.Species);
                double matureAt = nowHours + WildFernSpread.MaturationHours(
                    api, pos, requirements.Species, EcosystemConfig.Loaded);
                maturationQueues.AddFern(pos, matureCode, requirements.Species, matureAt);
                InvalidateEnvironmentAround(pos);
                if (spreadOrigin != null)
                {
                    WakeEcologyAround(spreadOrigin);
                }

                return;
            }

            if (FlowerSpreadMaturation.ShouldQueueMaturation(placed, requirements))
            {
                double nowHours = api.World.Calendar.TotalHours;
                AssetLocation matureCode = FlowerJuvenileBlocks.ResolveMatureCode(
                    api, spreadOrigin, requirements.Species);
                double matureAt = nowHours + WildFlowerMaturation.MaturationHours(
                    api, pos, requirements.Species, EcosystemConfig.Loaded);
                maturationQueues.AddFlower(pos, matureCode, requirements.Species, matureAt);
                InvalidateEnvironmentAround(pos);
                if (spreadOrigin != null)
                {
                    WakeEcologyAround(spreadOrigin);
                }

                return;
            }

            if (ShoreSedgeSpreadMaturation.ShouldQueueMaturation(placed, requirements))
            {
                double nowHours = api.World.Calendar.TotalHours;
                AssetLocation matureCode = ShoreSedgeJuvenileBlocks.ResolveMatureCode(
                    api, spreadOrigin, requirements.Species);
                double matureAt = nowHours + WildFlowerMaturation.MaturationHours(
                    api, pos, requirements.Species, EcosystemConfig.Loaded);
                maturationQueues.AddShoreSedge(pos, matureCode, requirements.Species, matureAt);
                InvalidateEnvironmentAround(pos);
                if (spreadOrigin != null)
                {
                    WakeEcologyAround(spreadOrigin);
                }

                return;
            }

            if (TallgrassSpreadMaturation.ShouldQueuePromotion(placed, requirements, api, pos))
            {
                maturationQueues.AddTallgrassPromotion(api, pos);
                InvalidateEnvironmentAround(pos);
                WakeEcologyAround(pos);
                if (spreadOrigin != null)
                {
                    WakeEcologyAround(spreadOrigin);
                }

                return;
            }

            if (BerrySpreadMaturation.ShouldQueueMaturation(placed, requirements, api, pos))
            {
                double nowHours = api.World.Calendar.TotalHours;
                double matureAt = nowHours + BerrySpreadMaturation.MaturationHours(
                    api, pos, requirements.Species, EcosystemConfig.Loaded);
                maturationQueues.AddBerry(pos, requirements.Species, matureAt);
                InvalidateEnvironmentAround(pos);
                if (spreadOrigin != null)
                {
                    WakeEcologyAround(spreadOrigin);
                }

                return;
            }

            if (EcosystemParticipant.TryFromBlock(placed, out IEcosystemParticipant participant))
            {
                RegisterReproducer(pos, participant, spawnBurst: false);
            }

            InvalidateEnvironmentAround(pos);
            WakeEcologyAround(pos);
            if (spreadOrigin != null)
            {
                WakeEcologyAround(spreadOrigin);
            }
        }

        void TrySpawnOffspring(ReproducerEntry entry, bool skipChanceRoll, int maxSpawns)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;

            if (entry.Requirements?.Habitat == EcologyHabitat.WildVine) return;

            if (SpreadGateChain.PreSpawn.BlocksSpread(api, entry, cfg)) return;

            BlockPos spreadOrigin = PlantCodeHelper.GetReproduceAnchor(
                api.World.BlockAccessor, entry.Origin, entry.MatureBlockCode);
            BlockPos spreadOriginCopy = spreadOrigin.Copy();
            MatSpreadCollectMode matMode = SpreadAttemptInspect.ResolveCollectMode(
                entry.Requirements, api.World.Rand);
            spreadMatModeScratch[spreadOriginCopy] = matMode;

            if (SpawnBlockedBySymbiosis(entry, spreadOrigin, spreadOriginCopy, matMode))
            {
                return;
            }

            float chance = SpeciesSpread.EffectiveChance(api, spreadOrigin, cfg, entry.Requirements);
            if (!skipChanceRoll && api.World.Rand.NextDouble() > chance)
            {
                RecordSpreadAttempt(spreadOriginCopy, entry, matMode, placed: false, "Chance roll failed");
                spreadCooldown.ApplyFailedChanceRollOnce(spreadOriginCopy, entry.Requirements);
                return;
            }

            Block spreadBlock = api.World.GetBlock(entry.JuvenileBlockCode);
            if (spreadBlock == null)
            {
                if (EcosystemConfig.Loaded.VerboseLogging)
                    api.Logger.Warning("[ecosystemflora] Spread block not found: {0}", entry.JuvenileBlockCode);
                return;
            }

            spreadBlock = FernSpreadMaturation.ResolveSpreadBlock(
                api, spreadOrigin, entry.Requirements, spreadBlock);
            spreadBlock = FlowerSpreadMaturation.ResolveSpreadBlock(
                api, spreadOrigin, entry.Requirements, spreadBlock);
            spreadBlock = ShoreSedgeSpreadMaturation.ResolveSpreadBlock(
                api, spreadOrigin, entry.Requirements, spreadBlock);
            if (spreadBlock == null) return;

            bool logFailures = cfg.VerboseLogging && cfg.ReproduceDebug;

            int spawned;
            string failureReason;
            bool backgroundQueued = false;
            if (cfg.EnableBackgroundSpreadSolve
                && cfg.EnableTwoPhaseSpreadPlacement
                && SpreadSolveBatchBuilder.CanBackgroundSolve(entry.Requirements)
                && backgroundSpread.TryQueueSolve(
                    api,
                    spreadOrigin,
                    spreadBlock,
                    entry.Requirements,
                    cfg.MinFitness,
                    cfg.HarshWildPlants,
                    cfg.ReproduceRadius,
                    cfg.ReproduceVerticalSearch,
                    maxSpawns,
                    api.World.Rand))
            {
                spawned = 0;
                failureReason = null;
                backgroundQueued = true;
            }
            else if (cfg.EnableTwoPhaseSpreadPlacement)
            {
                spawned = ReproducePlacement.TryEnqueueSpreadAmongNeighbors(
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
                    pendingSpreadQueue,
                    logFailures,
                    out failureReason);
            }
            else
            {
                spawned = ReproducePlacement.TryPlaceSpreadAmongNeighbors(
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
                    logFailures,
                    out failureReason,
                    onPlaced: (pos, requirements, displaced) =>
                        OnSpreadPlaced(pos, requirements, displaced, spreadOriginCopy));
            }

            if (logFailures && spawned == 0 && failureReason != null)
            {
                api.Logger.Notification("[ecosystemflora] No spread near {0}: {1}", entry.Origin, failureReason);
            }

            if (!FlowerSpreadCooldownTiming.ShouldDeferCooldownToPlacement(backgroundQueued, spawned))
            {
                spreadCooldown.ApplyPostSpreadAttemptOnce(spreadOriginCopy, entry.Requirements);
                if (spawned == 0)
                {
                    RecordSpreadAttempt(
                        spreadOriginCopy,
                        entry,
                        matMode,
                        placed: false,
                        failureReason ?? "No placement");
                }
            }
        }

        /// <summary>
        /// Spawn-site symbiosis gate. Unlike the position-independent <see cref="SpreadGateChain.PreSpawn"/>
        /// gates (which run on <c>entry.Origin</c> before anything is resolved), this gate is evaluated only
        /// after the reproduce anchor is known and records a "No symbiosis host" attempt when it vetoes.
        /// Those two traits (anchor dependency + side-effecting record) are why it lives here rather than in
        /// the pre-spawn chain. Returns true when spread must be aborted.
        /// </summary>
        bool SpawnBlockedBySymbiosis(
            ReproducerEntry entry,
            BlockPos spreadOrigin,
            BlockPos spreadOriginCopy,
            MatSpreadCollectMode matMode)
        {
            string species = entry.Requirements?.Species;
            if (string.IsNullOrEmpty(species)) return false;
            if (FloraSymbiosis.CanSpread(api.World.BlockAccessor, spreadOrigin, species)) return false;

            RecordSpreadAttempt(spreadOriginCopy, entry, matMode, placed: false, "No symbiosis host");
            return true;
        }

        void OnSpreadCommitDropped(PendingSpreadIntent intent)
        {
            if (intent?.ParentOrigin == null || intent.Requirements == null) return;

            spreadCooldown.ApplyOnCommit(intent.ParentOrigin, intent.Requirements);

            if (!registry.TryGetEntry(intent.ParentOrigin, out ReproducerEntry entry)) return;

            spreadMatModeScratch.TryGetValue(intent.ParentOrigin, out MatSpreadCollectMode matMode);
            SpreadAttemptInspect.Record(
                api,
                entry,
                matMode,
                placed: false,
                failureReason: "Commit revalidation failed");
        }

        void RecordSpreadAttempt(
            BlockPos spreadOrigin,
            PlantRequirements requirements,
            bool placed,
            string failureReason = null)
        {
            if (spreadOrigin == null || requirements == null) return;
            if (!registry.TryGetEntry(spreadOrigin, out ReproducerEntry entry)) return;

            spreadMatModeScratch.TryGetValue(spreadOrigin, out MatSpreadCollectMode matMode);
            SpreadAttemptInspect.Record(api, entry, matMode, placed, failureReason);
        }

        void RecordSpreadAttempt(
            BlockPos spreadOrigin,
            ReproducerEntry entry,
            MatSpreadCollectMode matMode,
            bool placed,
            string failureReason = null)
        {
            if (entry == null) return;
            SpreadAttemptInspect.Record(api, entry, matMode, placed, failureReason);
        }
    }
}
