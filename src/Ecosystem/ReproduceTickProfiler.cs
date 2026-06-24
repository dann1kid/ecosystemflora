using System.Diagnostics;
using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    internal struct ReproduceTickTimings
    {
        public long SaplingsTicks;
        public long FlowerMaturationTicks;
        public long FlowerPhenologyTicks;
        public long TallgrassPromotionTicks;
        public long FoliageTicks;
        public long TreeGrowthTicks;
        public long FerntreeGrowthTicks;
        public long CollectDueTicks;
        public long SpreadProcessTicks;
        public long SpreadCommitTicks;
        public long TotalTicks;
        public int SpreadProcessed;
        public int SpreadCommitted;
        public int DueQueueSize;
        public int PendingSpreadQueueSize;
        public int SpreadChunksVisited;
        public int SpreadMaxAttemptsInChunk;
        public int WakeDrivenAttempts;
        public int CalendarDrivenAttempts;
        public int SpreadSolveQueued;
        public int SpreadSolveCompleted;
        public int SpreadSolveWorkerPending;
        public long ColumnCacheHits;
        public long ColumnCacheMisses;
    }

    /// <summary>Throttled server log of reproduce-tick phase costs (enable via config).</summary>
    internal static class ReproduceTickProfiler
    {
        static long lastLogTimestamp;
        static long lastColumnCacheHits;
        static long lastColumnCacheMisses;

        public static void MaybeLog(
            ICoreAPI api,
            EcosystemConfig cfg,
            ReproducerRegistry registry,
            ReproduceTickTimings timings,
            EcologyColumnState columnCache = null)
        {
            if (api == null || cfg == null || registry == null) return;
            if (!cfg.EnableReproduceTickProfiling) return;
            if (registry.Count < cfg.ReproduceTickProfilingMinRegistry) return;

            long now = Stopwatch.GetTimestamp();
            long intervalTicks = cfg.ReproduceTickProfilingIntervalMs > 0
                ? cfg.ReproduceTickProfilingIntervalMs * Stopwatch.Frequency / 1000
                : 0;

            if (intervalTicks > 0 && now - lastLogTimestamp < intervalTicks) return;

            lastLogTimestamp = now;
            double toMs = 1000.0 / Stopwatch.Frequency;

            api.Logger.Notification(
                "[ecosystemflora] Reproduce tick profile (registry={0}): "
                + "saplings={1:F1}ms foliage={2:F1}ms trees={3:F1}ms ferntrees={4:F1}ms "
                + "collectDue={5:F1}ms spread={6:F1}ms commit={10:F1}ms total={7:F1}ms dueQueue={8} processed={9} pending={11} committed={12}",
                registry.Count,
                timings.SaplingsTicks * toMs,
                timings.FoliageTicks * toMs,
                timings.TreeGrowthTicks * toMs,
                timings.FerntreeGrowthTicks * toMs,
                timings.CollectDueTicks * toMs,
                timings.SpreadProcessTicks * toMs,
                timings.TotalTicks * toMs,
                timings.DueQueueSize,
                timings.SpreadProcessed,
                timings.SpreadCommitTicks * toMs,
                timings.PendingSpreadQueueSize,
                timings.SpreadCommitted);

            SampleColumnCacheDelta(columnCache, out long cacheHits, out long cacheMisses);
            long cacheTotal = cacheHits + cacheMisses;
            float cacheHitRate = cacheTotal > 0 ? (float)cacheHits / cacheTotal : 0f;

            api.Logger.Notification(
                "[ecosystemflora] Reproduce spread stats: chunks={0} max/chunk={1} wake={2} calendar={3} "
                + "workerQ={4} workerQueued={5} workerDone={6} spreadPending={7} columnCacheHit={8:P0} ({9}/{10})",
                timings.SpreadChunksVisited,
                timings.SpreadMaxAttemptsInChunk,
                timings.WakeDrivenAttempts,
                timings.CalendarDrivenAttempts,
                timings.SpreadSolveWorkerPending,
                timings.SpreadSolveQueued,
                timings.SpreadSolveCompleted,
                timings.PendingSpreadQueueSize,
                cacheHitRate,
                cacheHits,
                cacheTotal);
        }

        static void SampleColumnCacheDelta(EcologyColumnState columns, out long hits, out long misses)
        {
            hits = 0;
            misses = 0;
            if (columns == null) return;

            long totalHits = columns.CacheHits;
            long totalMisses = columns.CacheMisses;
            hits = totalHits - lastColumnCacheHits;
            misses = totalMisses - lastColumnCacheMisses;
            if (hits < 0) hits = 0;
            if (misses < 0) misses = 0;
            lastColumnCacheHits = totalHits;
            lastColumnCacheMisses = totalMisses;
        }

        internal static void ResetColumnCacheBaseline(EcologyColumnState columns)
        {
            if (columns == null) return;
            lastColumnCacheHits = columns.CacheHits;
            lastColumnCacheMisses = columns.CacheMisses;
        }
    }
}
