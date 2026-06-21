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
        readonly Dictionary<Vec2i, ReproducerDueHeap> chunkDueHeaps = new Dictionary<Vec2i, ReproducerDueHeap>();

        readonly SpreadChunkScheduler spreadChunkScheduler = new SpreadChunkScheduler();
        ulong ecologyWakeGeneration;

        public int Count => entries.Count;

        public int ChunkCount => byChunk.Count;

        public int LastDueQueueSize { get; private set; }

        public long LastCollectDueTicks { get; private set; }

        public long LastProcessDueTicks { get; private set; }

        public int LastSpreadChunksVisited { get; private set; }

        public int LastSpreadMaxAttemptsInChunk { get; private set; }

        public int LastWakeDrivenAttempts { get; private set; }

        public int LastCalendarDrivenAttempts { get; private set; }

        internal enum SpreadDueReason
        {
            None,
            Calendar,
            Wake,
        }

        internal static SpreadDueReason ClassifyDueReason(ReproducerEntry entry, double now, bool eventDriven)
        {
            if (entry == null || now < entry.NextSpawnAllowedAtHours) return SpreadDueReason.None;
            if (now >= entry.NextAttemptHours) return SpreadDueReason.Calendar;
            if (!eventDriven) return SpreadDueReason.None;
            if (entry.WakeGeneration > entry.LastProcessedWakeGeneration) return SpreadDueReason.Wake;
            return SpreadDueReason.None;
        }

        void ResetSpreadTickStats()
        {
            LastSpreadChunksVisited = 0;
            LastSpreadMaxAttemptsInChunk = 0;
            LastWakeDrivenAttempts = 0;
            LastCalendarDrivenAttempts = 0;
        }

        void RecordDueReason(SpreadDueReason reason)
        {
            if (reason == SpreadDueReason.Wake) LastWakeDrivenAttempts++;
            else if (reason == SpreadDueReason.Calendar) LastCalendarDrivenAttempts++;
        }

        public ReproducerEntry GetEntry(int index)
        {
            if (index < 0 || index >= entries.Count) return null;
            return entries[index];
        }

        public bool Contains(BlockPos pos) => byPos.ContainsKey(pos);

        public bool TryGetEntry(BlockPos pos, out ReproducerEntry entry) => byPos.TryGetValue(pos, out entry);

        internal bool TryGetChunkEntries(Vec2i chunk, out List<ReproducerEntry> list) => byChunk.TryGetValue(chunk, out list);

        public int WakeMatching(System.Func<ReproducerEntry, bool> predicate)
        {
            if (predicate == null || entries.Count == 0) return 0;

            ecologyWakeGeneration++;
            if (ecologyWakeGeneration == 0) ecologyWakeGeneration = 1;

            int woken = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                ReproducerEntry entry = entries[i];
                if (!predicate(entry)) continue;

                entry.WakeGeneration = ecologyWakeGeneration;
                woken++;
            }

            return woken;
        }

        public void WakeAround(BlockPos center, int radiusBlocks, double now = 0, EcosystemConfig cfg = null)
        {
            if (center == null || radiusBlocks <= 0 || entries.Count == 0) return;

            ecologyWakeGeneration++;
            if (ecologyWakeGeneration == 0) ecologyWakeGeneration = 1;

            bool pullRetry = now > 0
                && cfg != null
                && cfg.EnableEventDrivenSpread;
            double wakeRetryHours = cfg != null && cfg.EventWakeRetryHours > 0
                ? cfg.EventWakeRetryHours
                : 6;

            long radiusSq = (long)radiusBlocks * radiusBlocks;
            int cs = GlobalConstants.ChunkSize;
            int minCx = (center.X - radiusBlocks) / cs;
            int maxCx = (center.X + radiusBlocks) / cs;
            int minCz = (center.Z - radiusBlocks) / cs;
            int maxCz = (center.Z + radiusBlocks) / cs;

            for (int cx = minCx; cx <= maxCx; cx++)
            {
                for (int cz = minCz; cz <= maxCz; cz++)
                {
                    if (!byChunk.TryGetValue(new Vec2i(cx, cz), out List<ReproducerEntry> list)) continue;

                    for (int i = 0; i < list.Count; i++)
                    {
                        ReproducerEntry entry = list[i];
                        long dx = entry.Origin.X - center.X;
                        long dz = entry.Origin.Z - center.Z;
                        if (dx * dx + dz * dz > radiusSq) continue;

                        entry.WakeGeneration = ecologyWakeGeneration;

                        if (pullRetry && now >= entry.NextSpawnAllowedAtHours)
                        {
                            double earliest = now + wakeRetryHours;
                            if (entry.NextAttemptHours > earliest)
                            {
                                entry.NextAttemptHours = earliest;
                            }
                        }
                    }
                }
            }
        }

        internal static bool IsEntryDue(ReproducerEntry entry, double now, bool eventDriven) =>
            ClassifyDueReason(entry, now, eventDriven) != SpreadDueReason.None;

        internal bool IsLiveEntry(ReproducerEntry entry)
        {
            if (entry == null || entry.EntriesIndex < 0) return false;
            return byPos.TryGetValue(entry.Origin, out ReproducerEntry current) && ReferenceEquals(current, entry);
        }

        internal void FlushDueRemoves()
        {
            FlushRemoves(removeQueueScratch);
            removeQueueScratch.Clear();
        }

        /// <summary>Chunk-fair spread executor (Phase 6.1).</summary>
        public int ProcessDueChunkFair(
            EcosystemConfig cfg,
            double now,
            int maxAttempts,
            ICollection<Vec2i> activeChunks,
            System.Func<ReproducerEntry, double> intervalHoursForEntry,
            System.Func<ReproducerEntry, bool> tryProcess,
            long budgetTicks = 0,
            Stopwatch budgetWatch = null)
        {
            var collectWatch = Stopwatch.StartNew();
            removeQueueScratch.Clear();
            ResetSpreadTickStats();

            int processed = spreadChunkScheduler.Process(
                this,
                cfg,
                now,
                maxAttempts,
                activeChunks,
                intervalHoursForEntry,
                tryProcess,
                budgetTicks,
                budgetWatch,
                out int dueQueueSize,
                out int chunksVisited,
                out int maxAttemptsInChunk);

            collectWatch.Stop();
            LastCollectDueTicks = collectWatch.ElapsedTicks;
            LastProcessDueTicks = collectWatch.ElapsedTicks;
            LastDueQueueSize = dueQueueSize;
            LastSpreadChunksVisited = chunksVisited;
            LastSpreadMaxAttemptsInChunk = maxAttemptsInChunk;
            return processed;
        }

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
            PushDueSchedule(entry);
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
            Stopwatch budgetWatch = null,
            bool eventDriven = false)
        {
            if (entries.Count == 0 || maxAttempts <= 0) return 0;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            int scopeChunks = CountScopeChunks(activeChunks);
            if (scopeChunks <= 0) return 0;

            var collectWatch = Stopwatch.StartNew();
            ResetSpreadTickStats();

            int processed = spreadChunkScheduler.Process(
                this,
                cfg,
                now,
                maxAttempts,
                activeChunks,
                intervalHoursForEntry,
                tryProcess,
                budgetTicks,
                budgetWatch,
                out int dueQueueSize,
                out int chunksVisited,
                out int maxAttemptsInChunk,
                maxChunksVisitedOverride: scopeChunks,
                maxAttemptsPerChunkOverride: maxAttempts,
                eventDrivenOverride: eventDriven);

            collectWatch.Stop();
            LastCollectDueTicks = collectWatch.ElapsedTicks;
            LastProcessDueTicks = collectWatch.ElapsedTicks;
            LastDueQueueSize = dueQueueSize;
            LastSpreadChunksVisited = chunksVisited;
            LastSpreadMaxAttemptsInChunk = maxAttemptsInChunk;
            return processed;
        }

        int CountScopeChunks(ICollection<Vec2i> activeChunks)
        {
            if (activeChunks == null) return ChunkCount;

            int count = 0;
            foreach (Vec2i chunk in activeChunks)
            {
                if (byChunk.TryGetValue(chunk, out List<ReproducerEntry> list) && list.Count > 0)
                {
                    count++;
                }
            }

            return count;
        }

        void PushDueSchedule(ReproducerEntry entry)
        {
            if (entry == null) return;
            GetChunkDueHeap(ToChunkCoord(entry.Origin)).Push(entry);
        }

        ReproducerDueHeap GetChunkDueHeap(Vec2i chunkCoord)
        {
            if (!chunkDueHeaps.TryGetValue(chunkCoord, out ReproducerDueHeap heap))
            {
                heap = new ReproducerDueHeap();
                chunkDueHeaps[chunkCoord] = heap;
            }

            return heap;
        }

        internal void CollectDueEntries(double now, ICollection<Vec2i> activeChunks, List<ReproducerEntry> output, bool eventDriven = false)
        {
            output.Clear();
            if (entries.Count == 0) return;

            if (activeChunks == null)
            {
                if (eventDriven)
                {
                    CollectDueFromAllChunksEventDriven(now, output);
                    return;
                }

                CollectDueFromHeap(now, output);
                return;
            }

            BuildSpatialChunkList(activeChunks, spatialChunkScratch);
            for (int c = 0; c < spatialChunkScratch.Count; c++)
            {
                Vec2i chunk = spatialChunkScratch[c];
                if (eventDriven)
                {
                    if (!byChunk.TryGetValue(chunk, out List<ReproducerEntry> list)) continue;

                    for (int i = 0; i < list.Count; i++)
                    {
                        ReproducerEntry entry = list[i];
                        if (IsEntryDue(entry, now, eventDriven: true)) output.Add(entry);
                    }
                }
                else
                {
                    CollectDueFromChunkHeap(chunk, now, output);
                }
            }
        }

        void CollectDueFromAllChunksEventDriven(double now, List<ReproducerEntry> output)
        {
            foreach (KeyValuePair<Vec2i, List<ReproducerEntry>> kv in byChunk)
            {
                List<ReproducerEntry> list = kv.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    ReproducerEntry entry = list[i];
                    if (IsEntryDue(entry, now, eventDriven: true)) output.Add(entry);
                }
            }
        }

        void CollectDueFromChunkHeap(Vec2i chunkCoord, double now, List<ReproducerEntry> output)
        {
            if (!chunkDueHeaps.TryGetValue(chunkCoord, out ReproducerDueHeap heap)) return;

            int safety = heap.Count;
            while (safety-- > 0 && heap.TryPeek(now, out _))
            {
                ReproducerEntry entry = heap.Pop();
                if (!IsLiveEntry(entry)) continue;

                if (entry.NextAttemptHours > now || entry.NextSpawnAllowedAtHours > now)
                {
                    heap.Push(entry);
                    break;
                }

                output.Add(entry);
            }
        }

        void CollectDueFromHeap(double now, List<ReproducerEntry> output)
        {
            foreach (Vec2i chunk in chunkDueHeaps.Keys)
            {
                CollectDueFromChunkHeap(chunk, now, output);
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

        void RequeueUnprocessedDue(List<ReproducerEntry> dueBatch, HashSet<BlockPos> attemptedOrigins)
        {
            for (int i = 0; i < dueBatch.Count; i++)
            {
                ReproducerEntry entry = dueBatch[i];
                if (attemptedOrigins.Contains(entry.Origin)) continue;
                if (!IsLiveEntry(entry)) continue;

                PushDueSchedule(entry);
            }
        }

        internal bool TryProcessDueEntry(
            ReproducerEntry entry,
            double now,
            bool eventDriven,
            System.Func<ReproducerEntry, double> intervalHoursForEntry,
            System.Func<ReproducerEntry, bool> tryProcess,
            List<BlockPos> removeQueue)
        {
            if (!IsEntryDue(entry, now, eventDriven)) return false;

            SpreadDueReason reason = ClassifyDueReason(entry, now, eventDriven);
            RecordDueReason(reason);

            entry.LastProcessedWakeGeneration = entry.WakeGeneration;

            bool keep = tryProcess(entry);
            if (!keep)
            {
                removeQueue.Add(entry.Origin);
                return false;
            }

            double intervalHours = intervalHoursForEntry != null ? intervalHoursForEntry(entry) : 24;
            entry.NextAttemptHours = now + intervalHours;
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

        internal List<BlockPos> RemoveQueueScratch => removeQueueScratch;

        internal static int CompareChunkCoords(Vec2i a, Vec2i b)
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
