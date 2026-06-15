using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Chunk-sync foliage scheduler (v3.4) with optional legacy random tick.</summary>
    internal sealed class FoliageCellScheduler
    {
        internal readonly FoliageCellIndex Index = new FoliageCellIndex();
        readonly Dictionary<long, FoliageChunkState> chunkStates = new Dictionary<long, FoliageChunkState>();
        readonly Queue<Vec2i> syncQueue = new Queue<Vec2i>();
        readonly HashSet<long> syncQueued = new HashSet<long>();
        readonly HashSet<long> activeChunkKeysScratch = new HashSet<long>();

        int lastSeasonKey = int.MinValue;
        int autumnStripTotal;
        int totalSyncedCells;
        double lastAutumnProgressLogHours = -1000;

        public int PendingSyncChunks => syncQueue.Count;
        public int TrackedChunkCount => chunkStates.Count;

        public void Clear()
        {
            Index.Clear();
            chunkStates.Clear();
            syncQueue.Clear();
            syncQueued.Clear();
            autumnStripTotal = 0;
            totalSyncedCells = 0;
            lastSeasonKey = int.MinValue;
        }

        public void RemoveChunk(Vec2i chunkCoord)
        {
            long key = FoliageCellIndex.ChunkKey(chunkCoord.X, chunkCoord.Y);
            Index.RemoveChunk(chunkCoord);
            chunkStates.Remove(key);
            syncQueued.Remove(key);
        }

        public void OnBlockAdded(BlockPos pos)
        {
            if (pos == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (FoliageSyncModeHelper.UsesRandomTick(cfg))
            {
                Index.Add(pos);
            }

            InvalidateChunkAt(pos);
        }

        public void OnBlockRemoved(BlockPos pos)
        {
            if (pos == null) return;

            if (FoliageSyncModeHelper.UsesRandomTick(EcosystemConfig.Loaded))
            {
                Index.Remove(pos);
            }

            InvalidateChunkAt(pos);
        }

        public void ScheduleChunkSync(ICoreAPI api, Vec2i chunkCoord)
        {
            if (api?.World?.BlockAccessor == null) return;
            if (!EcosystemConfig.Loaded.EnableSeasonalFoliage) return;

            if (FoliageSyncModeHelper.Resolve(EcosystemConfig.Loaded) == FoliageSyncMode.Random)
            {
                ScheduleLegacyChunkIndex(api, chunkCoord);
                return;
            }

            EnqueueChunkSync(chunkCoord, force: true);
        }

        public void ScanChunkImmediate(ICoreAPI api, Vec2i chunkCoord) => ScheduleChunkSync(api, chunkCoord);

        void ScheduleLegacyChunkIndex(ICoreAPI api, Vec2i chunkCoord)
        {
            if (api?.World?.BlockAccessor == null) return;
            IBlockAccessor acc = api.World.BlockAccessor;
            if (acc.GetMapChunk(chunkCoord.X, chunkCoord.Y) == null) return;

            int catchUpCap = EcosystemConfig.Loaded.MaxFoliageCatchUpPerChunk <= 0
                ? int.MaxValue
                : EcosystemConfig.Loaded.MaxFoliageCatchUpPerChunk;

            FoliageColumnScanner.ScanChunk(
                acc,
                chunkCoord,
                Index,
                api,
                maxCatchUpOps: catchUpCap);
        }

        public int ProcessChunkSyncWork(
            ICoreAPI api,
            ICollection<Vec2i> activeChunks,
            Stopwatch budgetWatch,
            long budgetTicks)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EnableSeasonalFoliage || api == null) return 0;
            if (!FoliageSyncModeHelper.UsesChunkSync(cfg)) return 0;

            int seasonKey = FoliageSeasonKey.Current(api);
            if (seasonKey != lastSeasonKey)
            {
                lastSeasonKey = seasonKey;
                InvalidateAllTrackedChunks();
                EnqueueActiveChunksForSync(api, activeChunks);
            }

            if (syncQueue.Count == 0 && activeChunks != null)
            {
                EnqueueStaleActiveChunks(activeChunks, seasonKey);
            }

            IBlockAccessor acc = api.World.BlockAccessor;
            int changed = 0;
            int chunksLeft = cfg.FoliageChunkWorkPerTick <= 0 ? int.MaxValue : cfg.FoliageChunkWorkPerTick;
            long deadline = long.MaxValue;
            if (cfg.FoliageChunkSyncBudgetMs > 0)
            {
                deadline = Stopwatch.GetTimestamp()
                    + cfg.FoliageChunkSyncBudgetMs * Stopwatch.Frequency / 1000;
            }

            FoliageCellIndex index = FoliageSyncModeHelper.UsesRandomTick(cfg) ? Index : null;
            FillActiveChunkKeys(activeChunks, activeChunkKeysScratch);
            bool filterActive = cfg.OnlyActivateNearPlayers && activeChunkKeysScratch.Count > 0;

            while (chunksLeft > 0 && syncQueue.Count > 0)
            {
                if (Stopwatch.GetTimestamp() >= deadline) break;

                Vec2i coord = syncQueue.Dequeue();
                long chunkKey = FoliageCellIndex.ChunkKey(coord.X, coord.Y);
                syncQueued.Remove(chunkKey);

                if (filterActive && !activeChunkKeysScratch.Contains(chunkKey))
                {
                    syncQueue.Enqueue(coord);
                    syncQueued.Add(chunkKey);
                    continue;
                }

                if (acc.GetMapChunk(coord.X, coord.Y) == null) continue;

                if (!chunkStates.TryGetValue(chunkKey, out FoliageChunkState state))
                {
                    state = new FoliageChunkState { SyncedSeasonKey = -1, Completed = false };
                }

                if (state.Completed && state.SyncedSeasonKey == seasonKey) continue;

                int startLx = 0;
                int startLz = 0;
                int startY = acc.MapSizeY - 1;
                if (!state.Completed && state.ResumeY >= 0)
                {
                    startLx = state.ResumeLx;
                    startLz = state.ResumeLz;
                    startY = state.ResumeY;
                }

                FoliageChunkSyncPass.Result result = FoliageChunkSyncPass.Run(
                    api,
                    acc,
                    coord,
                    index,
                    startLx,
                    startLz,
                    startY,
                    deadline);

                if (result.Changed > 0)
                {
                    changed += result.Changed;
                    autumnStripTotal += result.Changed;
                    BlockPos invalidateAt = new BlockPos(
                        coord.X * GlobalConstants.ChunkSize + 8,
                        64,
                        coord.Y * GlobalConstants.ChunkSize + 8);
                    EcosystemSystem.Instance?.FloraContext?.InvalidateAround(invalidateAt, 3);
                    EcosystemSystem.Instance?.InvalidateEnvironmentAround(invalidateAt);
                }

                totalSyncedCells += result.Indexed;

                if (result.Completed)
                {
                    chunkStates[chunkKey] = new FoliageChunkState
                    {
                        SyncedSeasonKey = seasonKey,
                        Completed = true,
                        LastIndexed = result.Indexed,
                        LastChanged = result.Changed,
                    };
                }
                else
                {
                    chunkStates[chunkKey] = new FoliageChunkState
                    {
                        SyncedSeasonKey = -1,
                        ResumeLx = result.ResumeLx,
                        ResumeLz = result.ResumeLz,
                        ResumeY = result.ResumeY,
                        Completed = false,
                        LastIndexed = result.Indexed,
                        LastChanged = result.Changed,
                    };
                    syncQueue.Enqueue(coord);
                    syncQueued.Add(chunkKey);
                }

                chunksLeft--;
            }

            MaybeLogProgress(api, cfg, changed);
            return changed;
        }

        public int ProcessRandomTick(
            ICoreAPI api,
            ICollection<Vec2i> activeChunks,
            long budgetTicks,
            Stopwatch budgetWatch)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EnableSeasonalFoliage || api == null) return 0;
            if (!FoliageSyncModeHelper.UsesRandomTick(cfg)) return 0;
            if (cfg.MaxFoliageCellsTickedPerTick <= 0) return 0;

            FillActiveChunkKeys(activeChunks, activeChunkKeysScratch);
            if (activeChunkKeysScratch.Count == 0 && Index.TotalCells > 0)
            {
                activeChunkKeysScratch.Clear();
            }

            if (Index.TotalCells == 0)
            {
                FoliageDiagnostics.MaybeWarnEmptyIndex(api);
                return 0;
            }

            IBlockAccessor acc = api.World.BlockAccessor;
            int changed = 0;
            int attempts = cfg.MaxFoliageCellsTickedPerTick;
            const int maxPickRetries = 16;

            for (int i = 0; i < attempts; i++)
            {
                if (budgetTicks > 0 && budgetWatch != null && budgetWatch.ElapsedTicks >= budgetTicks) break;

                BlockPos pos = null;
                Block block = null;
                for (int retry = 0; retry < maxPickRetries; retry++)
                {
                    if (!Index.TryPickRandom(api.World.Rand, activeChunkKeysScratch, out BlockPos candidate)) break;

                    Block candidateBlock = acc.GetBlock(candidate);
                    if (!CanopyFoliageRules.IsSeasonalFoliageBlock(candidateBlock))
                    {
                        Index.Remove(candidate);
                        continue;
                    }

                    if (!CanopyFoliageRules.CanActThisSeason(api, candidate, candidateBlock)) continue;

                    pos = candidate;
                    block = candidateBlock;
                    break;
                }

                if (pos == null || block == null) continue;

                if (CanopyFoliageRules.TickCell(api, acc, pos, block, Index))
                {
                    EcosystemSystem.Instance?.FloraContext?.InvalidateAround(pos, 3);
                    EcosystemSystem.Instance?.InvalidateEnvironmentAround(pos);
                    changed++;
                    FoliageCellKind strippedKind = CanopyFoliageRules.Classify(block);
                    if (strippedKind == FoliageCellKind.RegularLeaf || strippedKind == FoliageCellKind.BranchyLeaf)
                    {
                        autumnStripTotal++;
                    }
                }
            }

            MaybeLogProgress(api, cfg, changed);
            return changed;
        }

        void EnqueueChunkSync(Vec2i chunkCoord, bool force)
        {
            long key = FoliageCellIndex.ChunkKey(chunkCoord.X, chunkCoord.Y);
            if (!force && syncQueued.Contains(key)) return;

            if (!chunkStates.TryGetValue(key, out FoliageChunkState state))
            {
                state = new FoliageChunkState { SyncedSeasonKey = -1, Completed = false, ResumeY = -1 };
                chunkStates[key] = state;
            }
            else if (state.Completed)
            {
                state.Completed = false;
                state.SyncedSeasonKey = -1;
                state.ResumeY = -1;
                state.ResumeLx = 0;
                state.ResumeLz = 0;
                chunkStates[key] = state;
            }

            if (syncQueued.Add(key))
            {
                syncQueue.Enqueue(chunkCoord);
            }
        }

        void InvalidateChunkAt(BlockPos pos)
        {
            if (pos == null) return;
            int cs = GlobalConstants.ChunkSize;
            EnqueueChunkSync(new Vec2i(pos.X / cs, pos.Z / cs), force: false);
        }

        void InvalidateAllTrackedChunks()
        {
            foreach (long key in chunkStates.Keys)
            {
                FoliageChunkState state = chunkStates[key];
                state.Completed = false;
                state.SyncedSeasonKey = -1;
                chunkStates[key] = state;

                int cx = (int)(key >> 32);
                int cz = (int)(key & 0xFFFFFFFF);
                EnqueueChunkSync(new Vec2i(cx, cz), force: false);
            }
        }

        void EnqueueActiveChunksForSync(ICoreAPI api, ICollection<Vec2i> activeChunks)
        {
            if (activeChunks == null || api?.World?.BlockAccessor == null) return;
            IBlockAccessor acc = api.World.BlockAccessor;
            foreach (Vec2i coord in activeChunks)
            {
                if (acc.GetMapChunk(coord.X, coord.Y) == null) continue;
                EnqueueChunkSync(coord, force: false);
            }
        }

        void EnqueueStaleActiveChunks(ICollection<Vec2i> activeChunks, int seasonKey)
        {
            if (activeChunks == null) return;
            foreach (Vec2i coord in activeChunks)
            {
                long key = FoliageCellIndex.ChunkKey(coord.X, coord.Y);
                if (chunkStates.TryGetValue(key, out FoliageChunkState state)
                    && state.Completed
                    && state.SyncedSeasonKey == seasonKey)
                {
                    continue;
                }

                EnqueueChunkSync(coord, force: false);
            }
        }

        void MaybeLogProgress(ICoreAPI api, EcosystemConfig cfg, int changedThisTick)
        {
            if (api == null || changedThisTick <= 0) return;

            if (cfg.ReproduceDebug)
            {
                api.Logger.Notification(
                    "[ecosystemflora] Foliage: {0} cell(s) updated, {1} sync pending, mode={2}",
                    changedThisTick,
                    syncQueue.Count,
                    cfg.FoliageSyncMode ?? "chunk");
                return;
            }

            IGameCalendar cal = api.World?.Calendar;
            if (cal == null) return;

            double now = cal.TotalHours;
            if (now - lastAutumnProgressLogHours < 1) return;

            lastAutumnProgressLogHours = now;
            api.Logger.Notification(
                "[ecosystemflora] Foliage sync: {0} block(s) this pass, {1} chunk(s) queued",
                changedThisTick,
                syncQueue.Count);
        }

        static void FillActiveChunkKeys(ICollection<Vec2i> activeChunks, HashSet<long> keys)
        {
            keys.Clear();
            if (activeChunks == null) return;

            foreach (Vec2i chunk in activeChunks)
            {
                keys.Add(FoliageCellIndex.ChunkKey(chunk.X, chunk.Y));
            }
        }

        internal int GetDiagnosticsIndexedCount()
        {
            if (FoliageSyncModeHelper.UsesRandomTick(EcosystemConfig.Loaded))
            {
                return Index.TotalCells;
            }

            return totalSyncedCells;
        }
    }
}
