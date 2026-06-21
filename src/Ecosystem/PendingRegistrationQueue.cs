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

        public int TotalPending
        {
            get
            {
                int n = 0;
                foreach (KeyValuePair<long, ChunkState> kv in byChunk)
                {
                    n += kv.Value.Items.Count;
                }

                return n;
            }
        }

        public int LastDrainApplied { get; private set; }

        public int LastDrainStale { get; private set; }

        public static int MaxHitsPerPass => MaxHitsPerScanPass;

        public bool HasPending(Vec2i chunk)
        {
            return byChunk.TryGetValue(ChunkKey(chunk), out ChunkState state) && state.Items.Count > 0;
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
                state.Items.Add(new PendingRegistration(chunk, hit.Pos.Copy(), hit.BlockCode.Clone(), kind));
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
            for (int i = 0; i < state.Items.Count; i++)
            {
                if (state.Items[i].Kind == PendingRegistrationKind.Tree
                    && state.Items[i].Pos.Equals(basePos))
                {
                    return false;
                }
            }

            state.Items.Add(new PendingRegistration(chunk, basePos.Copy(), blockCode.Clone(), PendingRegistrationKind.Tree));
            return true;
        }

        public void RemoveChunk(Vec2i chunk)
        {
            byChunk.Remove(ChunkKey(chunk));
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

                int chunkApplied = 0;
                while (applied < maxApplies
                       && chunkApplied < maxAppliesPerChunk
                       && state.Items.Count > 0)
                {
                    if (eco.TryApplyPendingRegistration(acc, state.Items[0], out bool stale))
                    {
                        applied++;
                        LastDrainApplied++;
                    }
                    else if (stale)
                    {
                        LastDrainStale++;
                    }

                    state.Items.RemoveAt(0);
                    chunkApplied++;
                }

                if (state.Items.Count == 0 && state.ScanCompleted)
                {
                    byChunk.Remove(key);
                    eco.OnPendingChunkDrained(chunk);
                }
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
            }

            return state;
        }

        static long ChunkKey(Vec2i chunk) => ((long)chunk.X << 32) | (uint)chunk.Y;

        sealed class ChunkState
        {
            public bool ScanCompleted;
            public readonly List<PendingRegistration> Items = new List<PendingRegistration>();
        }
    }
}
