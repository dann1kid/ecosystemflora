using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Player-priority chunk registration queue (Phase 6 registration latency).</summary>
    internal sealed class RegistrationScanQueue
    {
        readonly Queue<PendingChunkScan> priority = new Queue<PendingChunkScan>();
        readonly Queue<PendingChunkScan> background = new Queue<PendingChunkScan>();
        readonly HashSet<long> freshQueued = new HashSet<long>();

        public int Count => priority.Count + background.Count;

        public int PriorityCount => priority.Count;

        public int BackgroundCount => background.Count;

        public void Clear()
        {
            priority.Clear();
            background.Clear();
            freshQueued.Clear();
        }

        public void Enqueue(PendingChunkScan job, bool highPriority)
        {
            bool isFresh = job.NextLx == 0 && job.NextLz == 0 && job.NextY < 0;
            long key = ChunkKey(job.ChunkCoord);

            if (isFresh)
            {
                if (!freshQueued.Add(key)) return;
            }

            if (highPriority)
            {
                priority.Enqueue(job);
            }
            else
            {
                background.Enqueue(job);
            }
        }

        public bool TryDequeue(out PendingChunkScan job, bool preferPriority)
        {
            if (preferPriority && priority.Count > 0)
            {
                job = priority.Dequeue();
                return true;
            }

            if (background.Count > 0)
            {
                job = background.Dequeue();
                return true;
            }

            if (!preferPriority && priority.Count > 0)
            {
                job = priority.Dequeue();
                return true;
            }

            job = default;
            return false;
        }

        public void MarkComplete(Vec2i chunkCoord)
        {
            freshQueued.Remove(ChunkKey(chunkCoord));
        }

        public void MarkUnloaded(Vec2i chunkCoord)
        {
            freshQueued.Remove(ChunkKey(chunkCoord));
        }

        static long ChunkKey(Vec2i chunkCoord)
        {
            return ((long)chunkCoord.X << 32) | (uint)chunkCoord.Y;
        }
    }
}
