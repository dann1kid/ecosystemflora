using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// One paced registration slice for a near-player chunk load (priority queue fallback for the rest).
    /// </summary>
    internal static class ChunkRegistrationBurst
    {
        public static bool TryCompleteChunk(
            EcosystemSystem eco,
            ICoreAPI api,
            EcosystemConfig cfg,
            Vec2i chunkCoord,
            RegistrationScanQueue queue,
            int maxRegistrations,
            int totalBudgetMs,
            int passBudgetMs)
        {
            if (eco == null || api == null || cfg == null || queue == null || maxRegistrations <= 0) return false;

            IBlockAccessor acc = api.World?.BlockAccessor;
            if (acc == null) return false;

            int cs = GlobalConstants.ChunkSize;
            var center = new BlockPos(chunkCoord.X * cs + 16, 64, chunkCoord.Y * cs + 16);
            if (!PlayerProximity.IsNearAnyPlayer(api, center, cfg.PlayerRegistrationPriorityRadiusBlocks))
            {
                return false;
            }

            eco.PollBackgroundRegistration(cfg);
            if (eco.IsChunkRegistrationFinished(chunkCoord))
            {
                return true;
            }

            long freq = Stopwatch.Frequency;
            int sliceMs = passBudgetMs > 0 ? passBudgetMs : totalBudgetMs;
            if (sliceMs <= 0) sliceMs = totalBudgetMs;
            if (totalBudgetMs > 0 && sliceMs > totalBudgetMs) sliceMs = totalBudgetMs;

            long sliceDeadline = sliceMs > 0
                ? Stopwatch.GetTimestamp() + sliceMs * freq / 1000
                : long.MaxValue;

            if (cfg.EnableBackgroundRegistrationScan)
            {
                // Single paced slice — remainder goes to the priority registration queue.
                if (eco.TryAdvanceBackgroundScan(
                        chunkCoord,
                        acc,
                        cfg,
                        highPriority: true,
                        deadlineTicks: sliceDeadline,
                        out _))
                {
                    eco.PollBackgroundRegistration(cfg);
                    if (eco.IsChunkRegistrationFinished(chunkCoord))
                    {
                        return true;
                    }
                }

                queue.Enqueue(new PendingChunkScan(chunkCoord), highPriority: true);
                return false;
            }

            int scanScratch = 0;
            var job = new PendingChunkScan(chunkCoord);

            bool syncFoliage = cfg.EnableSeasonalFoliage && FoliageSyncModeHelper.UsesChunkSync(cfg);
            int seasonKey = FoliageSeasonKey.Current(api);
            FoliageCellIndex foliageIndex = FoliageSyncModeHelper.UsesRandomTick(cfg)
                ? eco.FoliageCells?.Index
                : null;

            if (!eco.TryRunRegistrationPass(
                    job,
                    acc,
                    cfg,
                    ref scanScratch,
                    sliceDeadline,
                    syncFoliage,
                    seasonKey,
                    foliageIndex,
                    out ChunkEcologyColumnPass.Result pass,
                    out bool completed))
            {
                queue.Enqueue(job, highPriority: true);
                return false;
            }

            if (completed)
            {
                eco.NotifyRegistrationScanCompleted(chunkCoord);
                return true;
            }

            queue.Enqueue(
                new PendingChunkScan(chunkCoord, pass.ResumeLx, pass.ResumeLz, pass.ResumeY),
                highPriority: true);
            return false;
        }
    }
}
