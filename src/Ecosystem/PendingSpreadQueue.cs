using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Chunk-fair drain of evaluated spread intents (Phase 6.5 commit phase).</summary>
    internal sealed class PendingSpreadQueue
    {
        readonly Dictionary<Vec2i, Queue<PendingSpreadIntent>> byChunk =
            new Dictionary<Vec2i, Queue<PendingSpreadIntent>>();
        readonly List<Vec2i> roundRobin = new List<Vec2i>();
        int roundRobinIndex;
        int totalCount;

        public int Count => totalCount;

        public int LastCommitted { get; private set; }

        /// <summary>Dequeues / RR steps in the last ProcessCommit (O(commits) after F3 fix).</summary>
        public int LastIntentLookups { get; private set; }

        public void Enqueue(PendingSpreadIntent intent)
        {
            if (intent?.TargetPos == null || intent.SpreadBlock == null) return;

            Vec2i chunk = intent.TargetChunk;
            if (!byChunk.TryGetValue(chunk, out Queue<PendingSpreadIntent> queue))
            {
                queue = new Queue<PendingSpreadIntent>();
                byChunk[chunk] = queue;
                roundRobin.Add(chunk);
            }

            queue.Enqueue(intent);
            totalCount++;
        }

        public int ProcessCommit(
            ICoreAPI api,
            EcosystemConfig cfg,
            Action<PendingSpreadIntent> onCommitted,
            int maxCommits,
            long budgetTicks,
            Stopwatch budgetWatch,
            bool logFailures,
            Action<PendingSpreadIntent> onDropped = null)
        {
            return ProcessCommit(
                api,
                cfg,
                onCommitted,
                maxCommits,
                budgetTicks,
                budgetWatch,
                logFailures,
                onDropped,
                tryCommit: null);
        }

        /// <summary>
        /// Commit drain with optional test hook. When <paramref name="tryCommit"/> is set it replaces
        /// <see cref="ReproducePlacement.TryCommitSpread"/> (no world I/O).
        /// </summary>
        internal int ProcessCommit(
            ICoreAPI api,
            EcosystemConfig cfg,
            Action<PendingSpreadIntent> onCommitted,
            int maxCommits,
            long budgetTicks,
            Stopwatch budgetWatch,
            bool logFailures,
            Action<PendingSpreadIntent> onDropped,
            System.Func<PendingSpreadIntent, bool> tryCommit)
        {
            LastCommitted = 0;
            LastIntentLookups = 0;
            if (cfg == null || totalCount == 0 || maxCommits <= 0) return 0;
            if (tryCommit == null && api == null) return 0;

            int maxChunksVisited = cfg.MaxSpreadCommitChunksVisitedPerTick > 0
                ? cfg.MaxSpreadCommitChunksVisitedPerTick
                : cfg.MaxSpreadChunksVisitedPerTick;
            if (maxChunksVisited <= 0) maxChunksVisited = 32;

            int maxPerChunk = cfg.MaxSpreadCommitsPerChunkPerTick > 0
                ? cfg.MaxSpreadCommitsPerChunkPerTick
                : cfg.MaxSpreadAttemptsPerChunkPerTick;
            if (maxPerChunk <= 0) maxPerChunk = 2;

            if (roundRobin.Count == 0) return 0;

            int committed = 0;
            int chunksVisited = 0;
            int chunkPasses = 0;
            int maxChunkPasses = roundRobin.Count;

            while (committed < maxCommits
                   && chunksVisited < maxChunksVisited
                   && chunkPasses < maxChunkPasses
                   && totalCount > 0
                   && roundRobin.Count > 0)
            {
                if (budgetTicks > 0 && budgetWatch != null && budgetWatch.ElapsedTicks >= budgetTicks) break;

                if (roundRobinIndex >= roundRobin.Count) roundRobinIndex = 0;
                Vec2i chunk = roundRobin[roundRobinIndex];
                chunkPasses++;
                LastIntentLookups++;

                if (!byChunk.TryGetValue(chunk, out Queue<PendingSpreadIntent> queue) || queue.Count == 0)
                {
                    RemoveEmptyChunkAt(roundRobinIndex);
                    continue;
                }

                chunksVisited++;
                int attemptsThisChunk = 0;

                while (attemptsThisChunk < maxPerChunk
                       && committed < maxCommits
                       && queue.Count > 0)
                {
                    if (budgetTicks > 0 && budgetWatch != null && budgetWatch.ElapsedTicks >= budgetTicks) break;

                    LastIntentLookups++;
                    PendingSpreadIntent intent = queue.Dequeue();
                    totalCount--;

                    bool ok = tryCommit != null
                        ? tryCommit(intent)
                        : ReproducePlacement.TryCommitSpread(api, intent, logFailures);
                    if (ok)
                    {
                        onCommitted?.Invoke(intent);
                        committed++;
                        attemptsThisChunk++;
                    }
                    else
                    {
                        onDropped?.Invoke(intent);
                    }
                }

                if (queue.Count == 0)
                {
                    RemoveEmptyChunkAt(roundRobinIndex);
                }
                else
                {
                    roundRobinIndex++;
                }
            }

            LastCommitted = committed;
            return committed;
        }

        void RemoveEmptyChunkAt(int index)
        {
            if (index < 0 || index >= roundRobin.Count) return;

            Vec2i chunk = roundRobin[index];
            byChunk.Remove(chunk);
            int last = roundRobin.Count - 1;
            if (index != last)
            {
                roundRobin[index] = roundRobin[last];
            }

            roundRobin.RemoveAt(last);
            if (roundRobinIndex > last) roundRobinIndex = 0;
            else if (roundRobinIndex >= roundRobin.Count) roundRobinIndex = 0;
        }

        public void Clear()
        {
            byChunk.Clear();
            roundRobin.Clear();
            roundRobinIndex = 0;
            totalCount = 0;
        }
    }
}
