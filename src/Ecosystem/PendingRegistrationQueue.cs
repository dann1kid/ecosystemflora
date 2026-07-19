using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal enum PendingRegistrationKind
    {
        Flower,
        Vine,
        Tree,
    }

    internal readonly struct PendingRegistration
    {
        public readonly Vec2i Chunk;
        public readonly BlockPos Pos;
        public readonly AssetLocation BlockCode;
        public readonly PendingRegistrationKind Kind;

        public PendingRegistration(Vec2i chunk, BlockPos pos, AssetLocation blockCode, PendingRegistrationKind kind)
        {
            Chunk = chunk;
            Pos = pos;
            BlockCode = blockCode;
            Kind = kind;
        }
    }

    /// <summary>Deferred ecology registration per chunk (Phase A paced apply).</summary>
    internal sealed class PendingRegistrationQueue
    {
        const int MaxHitsPerScanPass = GlobalConstants.ChunkSize * GlobalConstants.ChunkSize;

        readonly Dictionary<long, ChunkState> byChunk = new Dictionary<long, ChunkState>();
        readonly List<Vec2i> roundRobin = new List<Vec2i>();
        int roundRobinIndex;
        int totalPending;
        int lastChunkCount = -1;

        public int TotalPending => totalPending;

        public int LastDrainApplied { get; private set; }

        public int LastDrainStale { get; private set; }

        public static int MaxHitsPerPass => MaxHitsPerScanPass;

        public bool HasPending(Vec2i chunk)
        {
            return byChunk.TryGetValue(ChunkKey(chunk), out ChunkState state) && state.Items.Count > 0;
        }

        public bool HasPendingAt(BlockPos pos)
        {
            if (pos == null) return false;
            Vec2i chunk = ReproducerRegistry.ToChunkCoord(pos);
            if (!byChunk.TryGetValue(ChunkKey(chunk), out ChunkState state)) return false;

            foreach (PendingRegistration item in state.Items)
            {
                if (item.Pos != null && item.Pos.Equals(pos)) return true;
            }

            return false;
        }

        public bool IsScanCompleted(Vec2i chunk)
        {
            return byChunk.TryGetValue(ChunkKey(chunk), out ChunkState state) && state.ScanCompleted;
        }

        public bool IsReadyToMarkComplete(Vec2i chunk)
        {
            if (!byChunk.TryGetValue(ChunkKey(chunk), out ChunkState state)) return true;
            return state.ScanCompleted && state.Items.Count == 0;
        }

        public void SetScanCompleted(Vec2i chunk, bool completed)
        {
            ChunkState state = GetOrCreate(chunk);
            state.ScanCompleted = completed;
            if (completed && state.Items.Count == 0)
            {
                byChunk.Remove(ChunkKey(chunk));
                lastChunkCount = -1;
            }
        }

        public void EnqueueHits(Vec2i chunk, List<ChunkFlowerHit> hits, PendingRegistrationKind kind)
        {
            if (hits == null || hits.Count == 0) return;

            ChunkState state = GetOrCreate(chunk);
            for (int i = 0; i < hits.Count; i++)
            {
                ChunkFlowerHit hit = hits[i];
                if (hit.Pos == null || hit.BlockCode == null) continue;
                state.Items.Enqueue(new PendingRegistration(chunk, hit.Pos.Copy(), hit.BlockCode.Clone(), kind));
                totalPending++;
            }
        }

        public bool TryEnqueueTree(
            Vec2i chunk,
            BlockPos basePos,
            AssetLocation blockCode,
            ReproducerRegistry registry)
        {
            if (basePos == null || blockCode == null) return false;
            if (registry != null && registry.Contains(basePos)) return false;

            ChunkState state = GetOrCreate(chunk);
            foreach (PendingRegistration existing in state.Items)
            {
                if (existing.Kind == PendingRegistrationKind.Tree
                    && existing.Pos != null
                    && existing.Pos.Equals(basePos))
                {
                    return false;
                }
            }

            state.Items.Enqueue(new PendingRegistration(chunk, basePos.Copy(), blockCode.Clone(), PendingRegistrationKind.Tree));
            totalPending++;
            return true;
        }

        public void RemoveChunk(Vec2i chunk)
        {
            long key = ChunkKey(chunk);
            if (byChunk.TryGetValue(key, out ChunkState state))
            {
                totalPending -= state.Items.Count;
                if (totalPending < 0) totalPending = 0;
                byChunk.Remove(key);
                lastChunkCount = -1;
            }
        }

        public int Drain(
            EcosystemSystem eco,
            IBlockAccessor acc,
            int maxApplies,
            int maxAppliesPerChunk,
            HashSet<long> priorityChunkKeys)
        {
            LastDrainApplied = 0;
            LastDrainStale = 0;
            if (eco == null || acc == null || maxApplies <= 0 || byChunk.Count == 0) return 0;

            if (maxAppliesPerChunk <= 0)
            {
                maxAppliesPerChunk = maxApplies;
            }

            RefreshRoundRobin();
            int applied = 0;
            int chunkVisits = 0;
            int maxChunkVisits = roundRobin.Count * 4;

            while (applied < maxApplies && chunkVisits < maxChunkVisits && byChunk.Count > 0)
            {
                if (roundRobinIndex >= roundRobin.Count) roundRobinIndex = 0;
                Vec2i chunk = roundRobin[roundRobinIndex++];
                chunkVisits++;

                long key = ChunkKey(chunk);
                if (!byChunk.TryGetValue(key, out ChunkState state) || state.Items.Count == 0)
                {
                    continue;
                }

                bool prefer = priorityChunkKeys != null
                    && PlayerProximity.IsActiveChunk(priorityChunkKeys, chunk);

                if (!prefer && priorityChunkKeys != null && priorityChunkKeys.Count > 0)
                {
                    if (ChunkHasPriorityPending(priorityChunkKeys))
                    {
                        continue;
                    }
                }

                applied += DrainChunkState(eco, acc, chunk, state, maxApplies - applied, maxAppliesPerChunk);
            }

            return applied;
        }

        int DrainChunkState(
            EcosystemSystem eco,
            IBlockAccessor acc,
            Vec2i chunk,
            ChunkState state,
            int appliesBudget,
            int maxAppliesPerChunk)
        {
            if (appliesBudget <= 0 || state == null || state.Items.Count == 0) return 0;

            if (maxAppliesPerChunk <= 0)
            {
                maxAppliesPerChunk = appliesBudget;
            }

            int applied = 0;
            int chunkApplied = 0;
            while (applied < appliesBudget
                   && chunkApplied < maxAppliesPerChunk
                   && state.Items.Count > 0)
            {
                PendingRegistration item = state.Items.Dequeue();
                totalPending--;
                if (totalPending < 0) totalPending = 0;

                if (eco.TryApplyPendingRegistration(acc, item, out bool stale))
                {
                    applied++;
                    LastDrainApplied++;
                }
                else if (stale)
                {
                    LastDrainStale++;
                }
                else
                {
                    state.Items.Enqueue(item);
                    totalPending++;
                    break;
                }

                chunkApplied++;
            }

            if (state.Items.Count == 0 && state.ScanCompleted)
            {
                byChunk.Remove(ChunkKey(chunk));
                lastChunkCount = -1;
                eco.OnPendingChunkDrained(chunk);
            }

            return applied;
        }

        bool ChunkHasPriorityPending(HashSet<long> priorityChunkKeys)
        {
            foreach (long key in priorityChunkKeys)
            {
                if (byChunk.TryGetValue(key, out ChunkState state) && state.Items.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        void RefreshRoundRobin()
        {
            if (lastChunkCount == byChunk.Count && roundRobin.Count > 0) return;

            lastChunkCount = byChunk.Count;
            roundRobin.Clear();
            foreach (long key in byChunk.Keys)
            {
                int cx = (int)(key >> 32);
                int cz = (int)(key & 0xFFFFFFFF);
                roundRobin.Add(new Vec2i(cx, cz));
            }

            if (roundRobinIndex >= roundRobin.Count) roundRobinIndex = 0;
        }

        ChunkState GetOrCreate(Vec2i chunk)
        {
            long key = ChunkKey(chunk);
            if (!byChunk.TryGetValue(key, out ChunkState state))
            {
                state = new ChunkState();
                byChunk[key] = state;
                lastChunkCount = -1;
            }

            return state;
        }

        static long ChunkKey(Vec2i chunk) => ((long)chunk.X << 32) | (uint)chunk.Y;

        sealed class ChunkState
        {
            public bool ScanCompleted;
            public readonly Queue<PendingRegistration> Items = new Queue<PendingRegistration>();
        }
    }
}
