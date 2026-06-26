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

                if (pass.FoliageChanged > 0)
                {
                    int cs = GlobalConstants.ChunkSize;
                    var invalidateAt = new BlockPos(
                        chunkCoord.X * cs + 8,
                        64,
                        chunkCoord.Y * cs + 8);
                    FloraContext?.InvalidateAround(invalidateAt, 3);
                    InvalidateEnvironmentAround(invalidateAt);
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

            if (!EcosystemParticipant.TryFromBlock(block, out IEcosystemParticipant participant)) return false;

            RegisterReproducer(basePos, participant, spawnBurst: false);
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
            if (registry.Contains(anchor) || registry.Contains(pos)) return false;

            if (needsEstablishment)
            {
                maturationQueues.AddTallgrassPromotion(api, pos);
                registrationsLeft--;
                return true;
            }

            if (TallgrassSpreadMaturation.ShouldQueuePromotion(
                    block, PlantRequirements.FromBlock(block), api, pos))
            {
                maturationQueues.AddTallgrassPromotion(api, pos);
                registrationsLeft--;
                return true;
            }

            if (!EcosystemParticipant.TryFromBlock(block, out IEcosystemParticipant participant)) return false;

            RegisterReproducer(anchor, participant, spawnBurst: false);
            registrationsLeft--;
            return true;
        }
    }
}
