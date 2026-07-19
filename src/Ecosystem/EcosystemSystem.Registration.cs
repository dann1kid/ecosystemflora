using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Background-registration scan and discovery for <see cref="EcosystemSystem"/>: drives the
    /// per-chunk ecology column pass and registers discovered trees/flora into the shared reproducer
    /// registry. Kept as a partial (not a free-standing service) for the same reason as the spread-tick
    /// coordinator — it threads the shared registry and the RegisterReproducer entry point.
    /// </summary>
    public partial class EcosystemSystem
    {
        internal void PollBackgroundRegistration(EcosystemConfig cfg)
        {
            if (cfg.EnableBackgroundRegistrationScan)
            {
                backgroundRegistration.PollCompleted(this, cfg);
            }
        }

        internal bool TryAdvanceBackgroundScan(
            Vec2i chunkCoord,
            IBlockAccessor acc,
            EcosystemConfig cfg,
            bool highPriority,
            long deadlineTicks,
            out bool needsRequeue)
        {
            var job = new PendingChunkScan(chunkCoord);
            return backgroundRegistration.TryAdvance(
                this,
                acc,
                cfg,
                job,
                highPriority,
                deadlineTicks,
                out needsRequeue);
        }

        internal bool TryRunRegistrationPass(
            PendingChunkScan job,
            IBlockAccessor acc,
            EcosystemConfig cfg,
            ref int registrationsLeft,
            long passDeadline,
            bool syncFoliage,
            int seasonKey,
            FoliageCellIndex foliageIndex,
            out ChunkEcologyColumnPass.Result pass,
            out bool completed)
        {
            completed = false;
            pass = default;

            if (acc == null)
            {
                return false;
            }

            Vec2i chunkCoord = job.ChunkCoord;
            int maxHits = PendingRegistrationQueue.MaxHitsPerPass;

            FoliageChunkPassState passState = null;
            if (syncFoliage && cfg.EnableOrphanFoliagePrune)
            {
                int maxChecks = cfg.OrphanFoliageMaxChecksPerChunkPass;
                if (maxChecks <= 0) maxChecks = int.MaxValue;
                passState = new FoliageChunkPassState { OrphanChecksRemaining = maxChecks };
            }

            pass = ChunkEcologyColumnPass.Run(
                api,
                acc,
                chunkCoord,
                new ChunkEcologyColumnPass.Request
                {
                    MaxFlowerHits = maxHits,
                    MaxTreeHits = maxHits,
                    MaxVineHits = cfg.EnableWildVineEcology ? maxHits : 0,
                    SyncFoliage = syncFoliage,
                    FoliageIndex = foliageIndex,
                    PassState = passState,
                },
                job.NextLx,
                job.NextLz,
                job.NextY,
                (basePos, wood) =>
                {
                    Block trunk = acc.GetBlock(basePos);
                    if (trunk?.Code == null) return false;
                    return pendingRegistrations.TryEnqueueTree(chunkCoord, basePos, trunk.Code, registry);
                },
                passDeadline);

            EnqueueRegistrationScanHits(chunkCoord, pass);

            if (syncFoliage)
            {
                foliageCells.ApplyEcologyPassResult(chunkCoord, pass, seasonKey);
                foliageCells.ApplyFoliagePassState(
                    chunkCoord,
                    passState,
                    pass.Completed,
                    api.World.Calendar.TotalHours);

                if (pass.FoliageChanged > 0)
                {
                    int cs = GlobalConstants.ChunkSize;
                    var invalidateAt = new BlockPos(
                        chunkCoord.X * cs + 8,
                        64,
                        chunkCoord.Y * cs + 8);
                    InvalidateEnvironmentAround(invalidateAt, floraRadius: 2);
                }
            }

            completed = pass.Completed;
            return true;
        }

        bool TryRegisterDiscoveredTree(IBlockAccessor acc, BlockPos basePos, ref int registrationsLeft)
        {
            if (registrationsLeft <= 0 || basePos == null) return false;
            if (registry.Contains(basePos)) return false;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            Block block = acc.GetBlock(basePos);
            if (PlantCodeHelper.IsFerntreeTrunkBlock(block) && !cfg.EnableFerntreeEcology) return false;

            if (!EcosystemParticipant.TryCreateForRegistration(api, basePos, block, out IEcosystemParticipant participant)) return false;

            if (!RegisterReproducer(basePos, participant, spawnBurst: false))
            {
                return false;
            }

            registrationsLeft--;
            return true;
        }

        bool TryRegisterDiscoveredFlora(
            IBlockAccessor acc,
            BlockPos pos,
            Block block,
            bool needsEstablishment,
            ref int registrationsLeft)
        {
            if (registrationsLeft <= 0 || pos == null || block == null) return false;

            BlockPos anchor = PlantCodeHelper.GetReproduceAnchor(acc, pos, block.Code);

            // Tallgrass below target must re-enter promotion even when already registered for spread
            // (half-target register + dropped queue previously left meadows stuck mid-height).
            bool needsTallgrassPromotion = needsEstablishment
                || TallgrassEstablishment.ShouldQueueAfterPlacement(api, pos, block);
            if (needsTallgrassPromotion)
            {
                bool wasQueued = maturationQueues.TryGetTallgrassPromotionState(pos, out _, out _);
                maturationQueues.AddTallgrassPromotion(api, pos);
                bool newlyQueued = !wasQueued
                    && maturationQueues.TryGetTallgrassPromotionState(pos, out _, out _);
                if (newlyQueued)
                {
                    registrationsLeft--;
                }

                // Establishing grass is only registered for spread from the promotion loop at half-target.
                // Already-queued cells return false so discovery budget is not burned every rescan.
                return newlyQueued;
            }

            if (registry.Contains(anchor) || registry.Contains(pos)) return false;

            if (TryQueueFernDiscoveryMaturation(pos, block))
            {
                registrationsLeft--;
                return true;
            }

            if (!EcosystemParticipant.TryCreateForRegistration(api, pos, block, out IEcosystemParticipant participant)) return false;

            if (!RegisterReproducer(anchor, participant, spawnBurst: false))
            {
                return false;
            }

            registrationsLeft--;
            return true;
        }

        bool TryQueueFernDiscoveryMaturation(BlockPos pos, Block block)
        {
            if (api == null || pos == null || block == null) return false;

            PlantRequirements requirements = PlantRequirements.FromBlock(block);
            if (requirements == null || !FernSpreadMaturation.ShouldQueueMaturation(block, requirements))
            {
                return false;
            }

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            double nowHours = api.World.Calendar.TotalHours;
            AssetLocation matureCode = FernJuvenileBlocks.ResolveMatureCode(api, pos, requirements.Species);
            if (matureCode == null) return false;

            double matureAt = nowHours + WildFernSpread.MaturationHours(api, pos, requirements.Species, cfg);
            maturationQueues.AddFern(pos, matureCode, requirements.Species, matureAt);
            return true;
        }
    }
}
