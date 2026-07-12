using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Round-robin spread attempts across registry chunks (Phase 6.1).</summary>
    internal sealed class SpreadChunkScheduler
    {
        readonly List<Vec2i> roundRobin = new List<Vec2i>();
        readonly Dictionary<Vec2i, int> chunkDueRoundRobin = new Dictionary<Vec2i, int>();
        readonly List<ReproducerEntry> dueScratch = new List<ReproducerEntry>();
        int roundRobinIndex;
        int lastRegistryCount;
        int lastChunkCount;

        public int Process(
            ReproducerRegistry registry,
            EcosystemConfig cfg,
            double now,
            int maxTotalAttempts,
            ICollection<Vec2i> scopeChunks,
            Func<ReproducerEntry, double> intervalHoursForEntry,
            Func<ReproducerEntry, bool> tryProcess,
            long budgetTicks,
            Stopwatch budgetWatch,
            out int dueQueueSize,
            out int chunksVisited,
            out int maxAttemptsInChunk,
            int? maxChunksVisitedOverride = null,
            int? maxAttemptsPerChunkOverride = null,
            bool? eventDrivenOverride = null)
        {
            dueQueueSize = 0;
            chunksVisited = 0;
            maxAttemptsInChunk = 0;
            if (registry == null || cfg == null || maxTotalAttempts <= 0) return 0;

            int maxChunksVisited = maxChunksVisitedOverride ?? (cfg.MaxSpreadChunksVisitedPerTick > 0
                ? cfg.MaxSpreadChunksVisitedPerTick
                : 32);
            int maxAttemptsPerChunk = maxAttemptsPerChunkOverride ?? (cfg.MaxSpreadAttemptsPerChunkPerTick > 0
                ? cfg.MaxSpreadAttemptsPerChunkPerTick
                : 2);
            bool eventDriven = eventDrivenOverride ?? cfg.EnableEventDrivenSpread;

            RefreshRoundRobin(registry, scopeChunks);

            if (roundRobin.Count == 0) return 0;

            int processed = 0;
            int visited = 0;
            int peakAttemptsInChunk = 0;
            int chunkPasses = 0;
            int maxChunkPasses = roundRobin.Count;

            while (processed < maxTotalAttempts
                   && visited < maxChunksVisited
                   && chunkPasses < maxChunkPasses)
            {
                if (budgetTicks > 0 && budgetWatch != null && budgetWatch.ElapsedTicks >= budgetTicks) break;

                if (roundRobinIndex >= roundRobin.Count) roundRobinIndex = 0;
                Vec2i chunk = roundRobin[roundRobinIndex++];
                chunkPasses++;

                if (!registry.TryGetChunkEntries(chunk, out List<ReproducerEntry> list) || list.Count == 0)
                {
                    continue;
                }

                visited++;

                dueScratch.Clear();
                for (int i = 0; i < list.Count; i++)
                {
                    ReproducerEntry entry = list[i];
                    if (!registry.IsLiveEntry(entry)) continue;
                    if (!ReproducerRegistry.IsEntryDue(entry, now, eventDriven)) continue;

                    dueScratch.Add(entry);
                }

                dueQueueSize += dueScratch.Count;
                if (dueScratch.Count == 0) continue;

                if (!chunkDueRoundRobin.TryGetValue(chunk, out int start)) start = 0;
                int attemptsThisChunk = 0;
                int offset = 0;

                while (attemptsThisChunk < maxAttemptsPerChunk
                       && processed < maxTotalAttempts
                       && offset < dueScratch.Count)
                {
                    if (budgetTicks > 0 && budgetWatch != null && budgetWatch.ElapsedTicks >= budgetTicks) break;

                    ReproducerEntry entry = dueScratch[(start + offset) % dueScratch.Count];
                    offset++;

                    if (!registry.IsLiveEntry(entry)) continue;
                    if (!ReproducerRegistry.IsEntryDue(entry, now, eventDriven)) continue;

                    if (!registry.TryProcessDueEntry(
                            entry,
                            now,
                            eventDriven,
                            intervalHoursForEntry,
                            tryProcess,
                            registry.RemoveQueueScratch))
                    {
                        continue;
                    }

                    attemptsThisChunk++;
                    processed++;
                }

                chunkDueRoundRobin[chunk] = dueScratch.Count > 0
                    ? (start + offset) % dueScratch.Count
                    : 0;

                if (attemptsThisChunk > peakAttemptsInChunk)
                {
                    peakAttemptsInChunk = attemptsThisChunk;
                }
            }

            registry.FlushDueRemoves();
            chunksVisited = visited;
            maxAttemptsInChunk = peakAttemptsInChunk;
            return processed;
        }

        void RefreshRoundRobin(ReproducerRegistry registry, ICollection<Vec2i> scopeChunks)
        {
            int registryCount = registry.Count;
            int chunkCount = registry.ChunkCount;
            if (registryCount == lastRegistryCount
                && chunkCount == lastChunkCount
                && roundRobin.Count > 0)
            {
                return;
            }

            lastRegistryCount = registryCount;
            lastChunkCount = chunkCount;
            roundRobin.Clear();
            chunkDueRoundRobin.Clear();

            if (scopeChunks != null)
            {
                foreach (Vec2i chunk in scopeChunks)
                {
                    if (registry.TryGetChunkEntries(chunk, out List<ReproducerEntry> list) && list.Count > 0)
                    {
                        roundRobin.Add(chunk);
                    }
                }
            }
            else
            {
                foreach (Vec2i chunk in registry.ChunkCoords)
                {
                    roundRobin.Add(chunk);
                }
            }

            roundRobin.Sort(ReproducerRegistry.CompareChunkCoords);
            if (roundRobinIndex >= roundRobin.Count) roundRobinIndex = 0;
        }
    }
}
