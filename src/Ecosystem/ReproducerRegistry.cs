using System.Collections.Generic;
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

        int dueGlobalRoundRobinIndex;
        int stressGlobalRoundRobinIndex;
        int dueSpatialChunkCursor;
        int stressSpatialChunkCursor;
        readonly Dictionary<Vec2i, int> dueChunkEntryIndex = new Dictionary<Vec2i, int>();
        readonly Dictionary<Vec2i, int> stressChunkEntryIndex = new Dictionary<Vec2i, int>();
        readonly List<Vec2i> spatialChunkScratch = new List<Vec2i>();

        public int Count => entries.Count;

        public bool Contains(BlockPos pos) => byPos.ContainsKey(pos);

        public IEnumerable<Vec2i> ChunkCoords => byChunk.Keys;

        public List<Vec2i> CollectChunksNearPlayers(ICoreAPI api, int radiusBlocks)
        {
            var result = new List<Vec2i>();
            if (api == null || radiusBlocks <= 0) return result;

            foreach (Vec2i chunk in byChunk.Keys)
            {
                if (byChunk[chunk].Count == 0) continue;
                if (PlayerProximity.ChunkNearAnyPlayer(api, chunk, radiusBlocks))
                {
                    result.Add(chunk);
                }
            }

            return result;
        }

        public void Add(ReproducerEntry entry)
        {
            BlockPos origin = entry.Origin;
            if (byPos.TryGetValue(origin, out ReproducerEntry existing))
            {
                RemoveFromChunkIndex(existing);
                entries.Remove(existing);
            }

            byPos[origin] = entry;
            entries.Add(entry);
            AddToChunkIndex(entry);
        }

        public bool Remove(BlockPos pos)
        {
            if (!byPos.TryGetValue(pos, out ReproducerEntry entry)) return false;

            byPos.Remove(pos);
            entries.Remove(entry);
            RemoveFromChunkIndex(entry);
            return true;
        }

        public void RemoveChunk(Vec2i chunkCoord)
        {
            if (!byChunk.TryGetValue(chunkCoord, out List<ReproducerEntry> list)) return;

            for (int i = 0; i < list.Count; i++)
            {
                ReproducerEntry entry = list[i];
                byPos.Remove(entry.Origin);
                entries.Remove(entry);
            }

            byChunk.Remove(chunkCoord);
            dueChunkEntryIndex.Remove(chunkCoord);
            stressChunkEntryIndex.Remove(chunkCoord);
        }

        public int ProcessDue(
            double now,
            int maxAttempts,
            System.Func<ReproducerEntry, double> intervalHoursForEntry,
            System.Func<ReproducerEntry, bool> tryProcess,
            ICollection<Vec2i> activeChunks = null)
        {
            if (entries.Count == 0 || maxAttempts <= 0) return 0;

            if (activeChunks != null)
            {
                return ProcessDueSpatial(now, maxAttempts, activeChunks, intervalHoursForEntry, tryProcess);
            }

            return ProcessDueGlobal(now, maxAttempts, intervalHoursForEntry, tryProcess);
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

        int ProcessDueGlobal(
            double now,
            int maxAttempts,
            System.Func<ReproducerEntry, double> intervalHoursForEntry,
            System.Func<ReproducerEntry, bool> tryProcess)
        {
            int processed = 0;
            int scanned = 0;
            int scanBudget = entries.Count;
            var removeQueue = new List<BlockPos>();

            while (processed < maxAttempts && scanned < scanBudget)
            {
                int count = entries.Count;
                if (count == 0) break;
                if (dueGlobalRoundRobinIndex >= count) dueGlobalRoundRobinIndex = 0;
                ReproducerEntry entry = entries[dueGlobalRoundRobinIndex++];
                scanned++;

                if (!TryProcessDueEntry(entry, now, intervalHoursForEntry, tryProcess, removeQueue))
                {
                    continue;
                }

                processed++;
            }

            FlushRemoves(removeQueue);
            return processed;
        }

        int ProcessDueSpatial(
            double now,
            int maxAttempts,
            ICollection<Vec2i> activeChunks,
            System.Func<ReproducerEntry, double> intervalHoursForEntry,
            System.Func<ReproducerEntry, bool> tryProcess)
        {
            BuildSpatialChunkList(activeChunks, spatialChunkScratch);
            if (spatialChunkScratch.Count == 0) return 0;

            int processed = 0;
            int scanned = 0;
            int scanBudget = CountEntriesInChunks(spatialChunkScratch);
            var removeQueue = new List<BlockPos>();

            while (processed < maxAttempts && scanned < scanBudget)
            {
                if (dueSpatialChunkCursor >= spatialChunkScratch.Count) dueSpatialChunkCursor = 0;
                Vec2i chunk = spatialChunkScratch[dueSpatialChunkCursor++];

                if (!byChunk.TryGetValue(chunk, out List<ReproducerEntry> list) || list.Count == 0)
                {
                    scanned++;
                    continue;
                }

                if (!dueChunkEntryIndex.TryGetValue(chunk, out int entryIndex)) entryIndex = 0;
                if (entryIndex >= list.Count) entryIndex = 0;

                ReproducerEntry entry = list[entryIndex];
                dueChunkEntryIndex[chunk] = entryIndex + 1;
                scanned++;

                if (!byPos.TryGetValue(entry.Origin, out ReproducerEntry current) || !ReferenceEquals(current, entry))
                {
                    continue;
                }

                if (!TryProcessDueEntry(entry, now, intervalHoursForEntry, tryProcess, removeQueue))
                {
                    continue;
                }

                processed++;
            }

            FlushRemoves(removeQueue);
            return processed;
        }

        int ProcessStressGlobal(
            int maxChecks,
            System.Func<ReproducerEntry, bool> tryExpire,
            System.Action<BlockPos> onExpired)
        {
            int expired = 0;
            int scanned = 0;
            int scanBudget = entries.Count;
            var removeQueue = new List<BlockPos>();

            while (expired < maxChecks && scanned < scanBudget)
            {
                int count = entries.Count;
                if (count == 0) break;
                if (stressGlobalRoundRobinIndex >= count) stressGlobalRoundRobinIndex = 0;
                ReproducerEntry entry = entries[stressGlobalRoundRobinIndex++];
                scanned++;

                if (!tryExpire(entry)) continue;

                removeQueue.Add(entry.Origin);
                expired++;
            }

            FlushStressRemoves(removeQueue, onExpired);
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
            var removeQueue = new List<BlockPos>();

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

                removeQueue.Add(entry.Origin);
                expired++;
            }

            FlushStressRemoves(removeQueue, onExpired);
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

            list.Add(entry);
        }

        void RemoveFromChunkIndex(ReproducerEntry entry)
        {
            Vec2i cc = ToChunkCoord(entry.Origin);
            if (!byChunk.TryGetValue(cc, out List<ReproducerEntry> list)) return;

            list.Remove(entry);
            if (list.Count == 0)
            {
                byChunk.Remove(cc);
                dueChunkEntryIndex.Remove(cc);
                stressChunkEntryIndex.Remove(cc);
            }
        }

        public static Vec2i ToChunkCoord(BlockPos pos)
        {
            int cs = GlobalConstants.ChunkSize;
            return new Vec2i(pos.X / cs, pos.Z / cs);
        }
    }
}
