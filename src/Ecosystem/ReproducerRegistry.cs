using System.Collections.Generic;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal sealed class ReproducerRegistry
    {
        readonly Dictionary<BlockPos, ReproducerEntry> byPos = new Dictionary<BlockPos, ReproducerEntry>();
        readonly Dictionary<Vec2i, HashSet<BlockPos>> byChunk = new Dictionary<Vec2i, HashSet<BlockPos>>();
        readonly List<ReproducerEntry> entries = new List<ReproducerEntry>();
        int roundRobinIndex;

        public int Count => entries.Count;

        public bool Contains(BlockPos pos) => byPos.ContainsKey(pos);

        public void Add(ReproducerEntry entry)
        {
            BlockPos origin = entry.Origin;
            if (byPos.TryGetValue(origin, out ReproducerEntry existing))
            {
                RemoveFromChunkIndex(existing.Origin);
                entries.Remove(existing);
            }

            byPos[origin] = entry;
            entries.Add(entry);
            AddToChunkIndex(origin);
        }

        public bool Remove(BlockPos pos)
        {
            if (!byPos.TryGetValue(pos, out ReproducerEntry entry)) return false;

            byPos.Remove(pos);
            entries.Remove(entry);
            RemoveFromChunkIndex(pos);
            return true;
        }

        public void RemoveChunk(Vec2i chunkCoord)
        {
            if (!byChunk.TryGetValue(chunkCoord, out HashSet<BlockPos> positions)) return;

            foreach (BlockPos pos in positions)
            {
                if (byPos.TryGetValue(pos, out ReproducerEntry entry))
                {
                    byPos.Remove(pos);
                    entries.Remove(entry);
                }
            }

            byChunk.Remove(chunkCoord);
        }

        public int ProcessDue(
            double now,
            int maxAttempts,
            System.Func<ReproducerEntry, double> intervalHoursForEntry,
            System.Func<ReproducerEntry, bool> tryProcess)
        {
            if (entries.Count == 0 || maxAttempts <= 0) return 0;

            int processed = 0;
            int scanned = 0;
            int count = entries.Count;
            var removeQueue = new List<BlockPos>();

            while (processed < maxAttempts && scanned < count)
            {
                if (roundRobinIndex >= count) roundRobinIndex = 0;
                ReproducerEntry entry = entries[roundRobinIndex++];
                scanned++;

                if (now < entry.NextAttemptHours) continue;

                double intervalHours = intervalHoursForEntry != null ? intervalHoursForEntry(entry) : 24;
                entry.NextAttemptHours = now + intervalHours;

                bool keep = tryProcess(entry);
                if (!keep)
                {
                    removeQueue.Add(entry.Origin);
                    continue;
                }

                processed++;
            }

            for (int i = 0; i < removeQueue.Count; i++)
            {
                Remove(removeQueue[i]);
            }

            return processed;
        }

        void AddToChunkIndex(BlockPos pos)
        {
            Vec2i cc = ToChunkCoord(pos);
            if (!byChunk.TryGetValue(cc, out HashSet<BlockPos> set))
            {
                set = new HashSet<BlockPos>();
                byChunk[cc] = set;
            }

            set.Add(pos);
        }

        void RemoveFromChunkIndex(BlockPos pos)
        {
            Vec2i cc = ToChunkCoord(pos);
            if (!byChunk.TryGetValue(cc, out HashSet<BlockPos> set)) return;

            set.Remove(pos);
            if (set.Count == 0) byChunk.Remove(cc);
        }

        public static Vec2i ToChunkCoord(BlockPos pos)
        {
            int cs = GlobalConstants.ChunkSize;
            return new Vec2i(pos.X / cs, pos.Z / cs);
        }
    }
}
