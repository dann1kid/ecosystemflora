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
        readonly List<PendingSpreadIntent> pending = new List<PendingSpreadIntent>();
        readonly List<Vec2i> roundRobin = new List<Vec2i>();
        readonly Dictionary<Vec2i, int> chunkCursor = new Dictionary<Vec2i, int>();
        int roundRobinIndex;
        int lastPendingCount;

        public int Count => pending.Count;

        public int LastCommitted { get; private set; }

        public void Enqueue(PendingSpreadIntent intent)
        {
            if (intent?.TargetPos == null || intent.SpreadBlock == null) return;
            pending.Add(intent);
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
            LastCommitted = 0;
            if (api == null || cfg == null || pending.Count == 0 || maxCommits <= 0) return 0;

            int maxChunksVisited = cfg.MaxSpreadCommitChunksVisitedPerTick > 0
                ? cfg.MaxSpreadCommitChunksVisitedPerTick
                : cfg.MaxSpreadChunksVisitedPerTick;
            if (maxChunksVisited <= 0) maxChunksVisited = 32;

            int maxPerChunk = cfg.MaxSpreadCommitsPerChunkPerTick > 0
                ? cfg.MaxSpreadCommitsPerChunkPerTick
                : cfg.MaxSpreadAttemptsPerChunkPerTick;
            if (maxPerChunk <= 0) maxPerChunk = 2;

            RefreshRoundRobin();
            if (roundRobin.Count == 0) return 0;

            int committed = 0;
            int chunksVisited = 0;
            int chunkPasses = 0;
            int maxChunkPasses = roundRobin.Count;

            while (committed < maxCommits
                   && chunksVisited < maxChunksVisited
                   && chunkPasses < maxChunkPasses
                   && pending.Count > 0)
            {
                if (budgetTicks > 0 && budgetWatch != null && budgetWatch.ElapsedTicks >= budgetTicks) break;

                if (roundRobinIndex >= roundRobin.Count) roundRobinIndex = 0;
                Vec2i chunk = roundRobin[roundRobinIndex++];
                chunkPasses++;

                if (!ChunkHasPending(chunk)) continue;

                chunksVisited++;

                if (!chunkCursor.TryGetValue(chunk, out int cursor)) cursor = 0;
                int attemptsThisChunk = 0;
                int scanned = 0;

                while (attemptsThisChunk < maxPerChunk
                       && committed < maxCommits
                       && pending.Count > 0
                       && scanned < pending.Count)
                {
                    if (budgetTicks > 0 && budgetWatch != null && budgetWatch.ElapsedTicks >= budgetTicks) break;

                    int index = FindNextInChunk(chunk, ref cursor);
                    if (index < 0) break;

                    scanned++;

                    PendingSpreadIntent intent = pending[index];
                    if (ReproducePlacement.TryCommitSpread(api, intent, logFailures))
                    {
                        onCommitted?.Invoke(intent);
                        RemoveAt(index);
                        committed++;
                        attemptsThisChunk++;
                        if (cursor > index) cursor--;
                    }
                    else
                    {
                        onDropped?.Invoke(intent);
                        RemoveAt(index);
                        if (cursor > index) cursor--;
                    }
                }

                chunkCursor[chunk] = cursor;
            }

            lastPendingCount = pending.Count;
            LastCommitted = committed;
            return committed;
        }

        bool ChunkHasPending(Vec2i chunk)
        {
            for (int i = 0; i < pending.Count; i++)
            {
                Vec2i targetChunk = pending[i].TargetChunk;
                if (targetChunk.X == chunk.X && targetChunk.Y == chunk.Y) return true;
            }

            return false;
        }

        int FindNextInChunk(Vec2i chunk, ref int cursor)
        {
            if (pending.Count == 0) return -1;

            int start = cursor;
            for (int pass = 0; pass < pending.Count; pass++)
            {
                if (cursor >= pending.Count) cursor = 0;
                if (pass > 0 && cursor == start) break;

                PendingSpreadIntent intent = pending[cursor];
                int idx = cursor;
                cursor++;

                if (intent.TargetChunk.X == chunk.X && intent.TargetChunk.Y == chunk.Y)
                {
                    return idx;
                }
            }

            return -1;
        }

        void RemoveAt(int index)
        {
            int last = pending.Count - 1;
            if (index != last)
            {
                pending[index] = pending[last];
            }

            pending.RemoveAt(last);
        }

        void RefreshRoundRobin()
        {
            if (pending.Count == lastPendingCount && roundRobin.Count > 0) return;

            lastPendingCount = pending.Count;
            roundRobin.Clear();
            chunkCursor.Clear();

            var seen = new HashSet<long>();
            for (int i = 0; i < pending.Count; i++)
            {
                Vec2i chunk = pending[i].TargetChunk;
                long key = ((long)chunk.X << 32) | (uint)chunk.Y;
                if (seen.Add(key))
                {
                    roundRobin.Add(chunk);
                }
            }

            roundRobin.Sort(ReproducerRegistry.CompareChunkCoords);
            if (roundRobinIndex >= roundRobin.Count) roundRobinIndex = 0;
        }

        public void Clear() => pending.Clear();
    }
}
