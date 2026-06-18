using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Completes ecology registration for one chunk in a single burst (player-vicinity latency).</summary>
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

            if (cfg.EnableBackgroundRegistrationScan)
            {
                long freq = Stopwatch.Frequency;
                long totalDeadline = totalBudgetMs > 0
                    ? Stopwatch.GetTimestamp() + totalBudgetMs * freq / 1000
                    : long.MaxValue;

                while (Stopwatch.GetTimestamp() < totalDeadline)
                {
                    if (eco.TryAdvanceBackgroundScan(
                            chunkCoord,
                            acc,
                            cfg,
                            highPriority: true,
                            deadlineTicks: totalDeadline,
                            out bool needsRequeue))
                    {
                        eco.PollBackgroundRegistration(cfg);
                        if (eco.IsChunkRegistrationFinished(chunkCoord))
                        {
                            return true;
                        }

                        if (!needsRequeue && eco.IsChunkRegistrationFinished(chunkCoord))
                        {
                            return true;
                        }
                    }
                }

                queue.Enqueue(new PendingChunkScan(chunkCoord), highPriority: true);
                return false;
            }

            long freqSync = Stopwatch.Frequency;
            long totalDeadlineSync = totalBudgetMs > 0
                ? Stopwatch.GetTimestamp() + totalBudgetMs * freqSync / 1000
                : long.MaxValue;

            int scanScratch = 0;
            var job = new PendingChunkScan(chunkCoord);

            bool syncFoliage = cfg.EnableSeasonalFoliage && FoliageSyncModeHelper.UsesChunkSync(cfg);
            int seasonKey = FoliageSeasonKey.Current(api);
            FoliageCellIndex foliageIndex = FoliageSyncModeHelper.UsesRandomTick(cfg)
                ? eco.FoliageCells?.Index
                : null;

            while (Stopwatch.GetTimestamp() < totalDeadlineSync)
            {
                long passDeadline = passBudgetMs > 0
                    ? System.Math.Min(
                        totalDeadlineSync,
                        Stopwatch.GetTimestamp() + passBudgetMs * freqSync / 1000)
                    : totalDeadlineSync;

                if (!eco.TryRunRegistrationPass(
                        job,
                        acc,
                        cfg,
                        ref scanScratch,
                        passDeadline,
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

                job = new PendingChunkScan(chunkCoord, pass.ResumeLx, pass.ResumeLz, pass.ResumeY);
            }

            queue.Enqueue(job, highPriority: true);
            return false;
        }
    }
}
