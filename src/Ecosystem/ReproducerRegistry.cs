using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal sealed class ReproducerRegistry
    {
        readonly Dictionary<BlockPos, ReproducerEntry> byPos = new Dictionary<BlockPos, ReproducerEntry>();
        readonly Dictionary<Vec2i, List<ReproducerEntry>> byChunk = new Dictionary<Vec2i, List<ReproducerEntry>>();
        readonly List<ReproducerEntry> entries = new List<ReproducerEntry>();

        int stressGlobalRoundRobinIndex;
        int stressSpatialChunkCursor;
        readonly Dictionary<Vec2i, int> stressChunkEntryIndex = new Dictionary<Vec2i, int>();
        readonly List<Vec2i> spatialChunkScratch = new List<Vec2i>();
        readonly List<BlockPos> removeQueueScratch = new List<BlockPos>();
        readonly List<ReproducerEntry> dueQueueScratch = new List<ReproducerEntry>();
        int spreadFairCursor;

        public int Count => entries.Count;

        public int LastDueQueueSize { get; private set; }

        public ReproducerEntry GetEntry(int index)
        {
            if (index < 0 || index >= entries.Count) return null;
            return entries[index];
        }

        public bool Contains(BlockPos pos) => byPos.ContainsKey(pos);

        public bool TryGetEntry(BlockPos pos, out ReproducerEntry entry) => byPos.TryGetValue(pos, out entry);

        public IEnumerable<Vec2i> ChunkCoords => byChunk.Keys;

        readonly List<Vec2i> nearPlayerScratch = new List<Vec2i>();

        public List<Vec2i> CollectChunksNearPlayers(ICoreAPI api, int radiusBlocks)
        {
            nearPlayerScratch.Clear();
            if (api == null || radiusBlocks <= 0) return nearPlayerScratch;

            HashSet<long> activeChunks = PlayerProximity.BuildActivePlayerChunks(api, radiusBlocks);

            foreach (Vec2i chunk in byChunk.Keys)
            {
                if (byChunk[chunk].Count == 0) continue;
                if (PlayerProximity.IsActiveChunk(activeChunks, chunk))
                {
                    nearPlayerScratch.Add(chunk);
                }
            }

            return nearPlayerScratch;
        }

        public void Add(ReproducerEntry entry)
        {
            BlockPos origin = entry.Origin;
            if (byPos.TryGetValue(origin, out ReproducerEntry existing))
            {
                RemoveEntry(existing);
            }

            byPos[origin] = entry;
            entry.EntriesIndex = entries.Count;
            entries.Add(entry);
            AddToChunkIndex(entry);
        }

        public bool Remove(BlockPos pos)
        {
            if (!byPos.TryGetValue(pos, out ReproducerEntry entry)) return false;

            RemoveEntry(entry);
            return true;
        }

        public void RemoveChunk(Vec2i chunkCoord)
        {
            if (!byChunk.TryGetValue(chunkCoord, out List<ReproducerEntry> list)) return;

            var snapshot = new List<ReproducerEntry>(list);
            for (int i = 0; i < snapshot.Count; i++)
            {
                RemoveEntry(snapshot[i]);
            }

            stressChunkEntryIndex.Remove(chunkCoord);
        }

        void RemoveEntry(ReproducerEntry entry)
        {
            if (entry == null) return;

            byPos.Remove(entry.Origin);
            RemoveFromEntriesList(entry);
            RemoveFromChunkIndex(entry);
        }

        void RemoveFromEntriesList(ReproducerEntry entry)
        {
            int idx = entry.EntriesIndex;
            if (idx < 0 || idx >= entries.Count) return;

            int last = entries.Count - 1;
            if (idx != last)
            {
                ReproducerEntry swapped = entries[last];
                entries[idx] = swapped;
                swapped.EntriesIndex = idx;
            }

            entries.RemoveAt(last);
            entry.EntriesIndex = -1;
        }

        /// <summary>
        /// Enqueue all due entries in scope, then process up to maxAttempts with optional time budget.
        /// Fair cursor rotates so the same subset does not monopolize attempts every tick.
        /// </summary>
        public int ProcessDue(
            double now,
            int maxAttempts,
            System.Func<ReproducerEntry, double> intervalHoursForEntry,
            System.Func<ReproducerEntry, bool> tryProcess,
            ICollection<Vec2i> activeChunks = null,
            long budgetTicks = 0,
            Stopwatch budgetWatch = null)
        {
            if (entries.Count == 0 || maxAttempts <= 0) return 0;

            dueQueueScratch.Clear();
            CollectDueEntries(now, activeChunks, dueQueueScratch);
            LastDueQueueSize = dueQueueScratch.Count;
            if (dueQueueScratch.Count == 0) return 0;

            int processed = 0;
            int queueSize = dueQueueScratch.Count;
            removeQueueScratch.Clear();

            if (spreadFairCursor >= queueSize) spreadFairCursor = 0;

            for (int i = 0; i < queueSize && processed < maxAttempts; i++)
            {
                if (budgetTicks > 0 && budgetWatch != null && budgetWatch.ElapsedTicks >= budgetTicks) break;

                ReproducerEntry entry = dueQueueScratch[(spreadFairCursor + i) % queueSize];
                if (!byPos.TryGetValue(entry.Origin, out ReproducerEntry current) || !ReferenceEquals(current, entry))
                {
                    continue;
                }

                if (!TryProcessDueEntry(entry, now, intervalHoursForEntry, tryProcess, removeQueueScratch))
                {
                    continue;
                }

                processed++;
            }

            spreadFairCursor = queueSize > 0 ? (spreadFairCursor + processed) % queueSize : 0;
            FlushRemoves(removeQueueScratch);
            return processed;
        }

        internal void CollectDueEntries(double now, ICollection<Vec2i> activeChunks, List<ReproducerEntry> output)
        {
            output.Clear();
            if (entries.Count == 0) return;

            if (activeChunks == null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    ReproducerEntry entry = entries[i];
                    if (now >= entry.NextAttemptHours) output.Add(entry);
                }

                return;
            }

            BuildSpatialChunkList(activeChunks, spatialChunkScratch);
            for (int c = 0; c < spatialChunkScratch.Count; c++)
            {
                Vec2i chunk = spatialChunkScratch[c];
                if (!byChunk.TryGetValue(chunk, out List<ReproducerEntry> list)) continue;

                for (int i = 0; i < list.Count; i++)
                {
                    ReproducerEntry entry = list[i];
                    if (now >= entry.NextAttemptHours) output.Add(entry);
                }
            }
        }

        public int ProcessStress(
            int maxChecks,
            System.Func<ReproducerEntry, bool> tryExpire,
            System.Action<BlockPos> onExpired = null,
            ICollection<Vec2i> activeChunks = null)
        {
            if (entries.Count == 0 || maxChecks <= 0 || tryExpire == null) return 0;

            if (activeChunks != null)
            {
                return ProcessStressSpatial(maxChecks, activeChunks, tryExpire, onExpired);
            }

            return ProcessStressGlobal(maxChecks, tryExpire, onExpired);
        }

        int ProcessStressGlobal(
            int maxChecks,
            System.Func<ReproducerEntry, bool> tryExpire,
            System.Action<BlockPos> onExpired)
        {
            int expired = 0;
            int scanned = 0;
            int scanBudget = entries.Count;
            removeQueueScratch.Clear();

            while (expired < maxChecks && scanned < scanBudget)
            {
                int count = entries.Count;
                if (count == 0) break;
                if (stressGlobalRoundRobinIndex >= count) stressGlobalRoundRobinIndex = 0;
                ReproducerEntry entry = entries[stressGlobalRoundRobinIndex++];
                scanned++;

                if (!tryExpire(entry)) continue;

                removeQueueScratch.Add(entry.Origin);
                expired++;
            }

            FlushStressRemoves(removeQueueScratch, onExpired);
            return expired;
        }

        int ProcessStressSpatial(
            int maxChecks,
            ICollection<Vec2i> activeChunks,
            System.Func<ReproducerEntry, bool> tryExpire,
            System.Action<BlockPos> onExpired)
        {
            BuildSpatialChunkList(activeChunks, spatialChunkScratch);
            if (spatialChunkScratch.Count == 0) return 0;

            int expired = 0;
            int scanned = 0;
            int scanBudget = CountEntriesInChunks(spatialChunkScratch);
            removeQueueScratch.Clear();

            while (expired < maxChecks && scanned < scanBudget)
            {
                if (stressSpatialChunkCursor >= spatialChunkScratch.Count) stressSpatialChunkCursor = 0;
                Vec2i chunk = spatialChunkScratch[stressSpatialChunkCursor++];

                if (!byChunk.TryGetValue(chunk, out List<ReproducerEntry> list) || list.Count == 0)
                {
                    scanned++;
                    continue;
                }

                if (!stressChunkEntryIndex.TryGetValue(chunk, out int entryIndex)) entryIndex = 0;
                if (entryIndex >= list.Count) entryIndex = 0;

                ReproducerEntry entry = list[entryIndex];
                stressChunkEntryIndex[chunk] = entryIndex + 1;
                scanned++;

                if (!byPos.TryGetValue(entry.Origin, out ReproducerEntry current) || !ReferenceEquals(current, entry))
                {
                    continue;
                }

                if (!tryExpire(entry)) continue;

                removeQueueScratch.Add(entry.Origin);
                expired++;
            }

            FlushStressRemoves(removeQueueScratch, onExpired);
            return expired;
        }

        bool TryProcessDueEntry(
            ReproducerEntry entry,
            double now,
            System.Func<ReproducerEntry, double> intervalHoursForEntry,
            System.Func<ReproducerEntry, bool> tryProcess,
            List<BlockPos> removeQueue)
        {
            if (now < entry.NextAttemptHours) return false;

            double intervalHours = intervalHoursForEntry != null ? intervalHoursForEntry(entry) : 24;
            entry.NextAttemptHours = now + intervalHours;

            bool keep = tryProcess(entry);
            if (!keep)
            {
                removeQueue.Add(entry.Origin);
                return false;
            }

            return true;
        }

        void FlushRemoves(List<BlockPos> removeQueue)
        {
            for (int i = 0; i < removeQueue.Count; i++)
            {
                Remove(removeQueue[i]);
            }
        }

        void FlushStressRemoves(List<BlockPos> removeQueue, System.Action<BlockPos> onExpired)
        {
            for (int i = 0; i < removeQueue.Count; i++)
            {
                BlockPos pos = removeQueue[i];
                if (onExpired != null)
                {
                    onExpired(pos);
                }
                else
                {
                    Remove(pos);
                }
            }
        }

        void BuildSpatialChunkList(ICollection<Vec2i> activeChunks, List<Vec2i> output)
        {
            output.Clear();
            if (activeChunks == null || activeChunks.Count == 0) return;

            foreach (Vec2i chunk in activeChunks)
            {
                if (byChunk.TryGetValue(chunk, out List<ReproducerEntry> list) && list.Count > 0)
                {
                    output.Add(chunk);
                }
            }

            output.Sort(CompareChunkCoords);
        }

        static int CompareChunkCoords(Vec2i a, Vec2i b)
        {
            int cmp = a.X.CompareTo(b.X);
            return cmp != 0 ? cmp : a.Y.CompareTo(b.Y);
        }

        int CountEntriesInChunks(List<Vec2i> chunks)
        {
            int total = 0;
            for (int i = 0; i < chunks.Count; i++)
            {
                if (byChunk.TryGetValue(chunks[i], out List<ReproducerEntry> list))
                {
                    total += list.Count;
                }
            }

            return total;
        }

        void AddToChunkIndex(ReproducerEntry entry)
        {
            Vec2i cc = ToChunkCoord(entry.Origin);
            if (!byChunk.TryGetValue(cc, out List<ReproducerEntry> list))
            {
                list = new List<ReproducerEntry>();
                byChunk[cc] = list;
            }

            entry.ChunkListIndex = list.Count;
            list.Add(entry);
        }

        void RemoveFromChunkIndex(ReproducerEntry entry)
        {
            Vec2i cc = ToChunkCoord(entry.Origin);
            if (!byChunk.TryGetValue(cc, out List<ReproducerEntry> list)) return;

            int idx = entry.ChunkListIndex;
            int last = list.Count - 1;
            if (idx < 0 || idx > last) return;

            if (idx != last)
            {
                ReproducerEntry swapped = list[last];
                list[idx] = swapped;
                swapped.ChunkListIndex = idx;
            }

            list.RemoveAt(last);
            entry.ChunkListIndex = -1;

            if (list.Count == 0)
            {
                byChunk.Remove(cc);
                stressChunkEntryIndex.Remove(cc);
            }
        }

        public static Vec2i ToChunkCoord(BlockPos pos)
        {
            int cs = GlobalConstants.ChunkSize;
            return new Vec2i(pos.X / cs, pos.Z / cs);
        }

        /// <summary>When <paramref name="activeChunks"/> is null, all origins are in scope (normal play).</summary>
        public static bool IsInActiveChunks(BlockPos origin, ICollection<Vec2i> activeChunks)
        {
            if (activeChunks == null || origin == null) return true;

            Vec2i chunk = ToChunkCoord(origin);
            foreach (Vec2i active in activeChunks)
            {
                if (active.X == chunk.X && active.Y == chunk.Y) return true;
            }

            return false;
        }
    }
}
