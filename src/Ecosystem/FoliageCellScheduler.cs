using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Chunk-sync foliage state; column work runs in unified ChunkEcologyColumnPass.</summary>
    internal sealed class FoliageCellScheduler
    {
        internal readonly FoliageCellIndex Index = new FoliageCellIndex();
        readonly Dictionary<long, FoliageChunkState> chunkStates = new Dictionary<long, FoliageChunkState>();
        readonly HashSet<long> activeChunkKeysScratch = new HashSet<long>();

        int lastSeasonKey = int.MinValue;
        int autumnStripTotal;
        int totalSyncedCells;
        double lastAutumnProgressLogHours = -1000;

        internal Action<Vec2i> RequestEcologyScan;

        public int PendingSyncChunks
        {
            get
            {
                int n = 0;
                foreach (KeyValuePair<long, FoliageChunkState> kv in chunkStates)
                {
                    if (!kv.Value.Completed) n++;
                }

                return n;
            }
        }

        public int TrackedChunkCount => chunkStates.Count;

        public void Clear()
        {
            Index.Clear();
            chunkStates.Clear();
            autumnStripTotal = 0;
            totalSyncedCells = 0;
            lastSeasonKey = int.MinValue;
        }

        public void RemoveChunk(Vec2i chunkCoord)
        {
            long key = FoliageCellIndex.ChunkKey(chunkCoord.X, chunkCoord.Y);
            Index.RemoveChunk(chunkCoord);
            chunkStates.Remove(key);
        }

        public void OnBlockAdded(BlockPos pos)
        {
            if (pos == null) return;

            if (FoliageSyncModeHelper.UsesRandomTick(EcosystemConfig.Loaded))
            {
                Index.Add(pos);
            }

            // Do not invalidate chunk sync here — re-scan runs catch-up budding and regrows
            // player-cleared or mod-placed foliage. Season change and chunk load handle sync.
        }

        public void OnBlockRemoved(BlockPos pos)
        {
            if (pos == null) return;

            if (FoliageSyncModeHelper.UsesRandomTick(EcosystemConfig.Loaded))
            {
                Index.Remove(pos);
            }

            // Do not invalidate chunk sync on break — see OnBlockAdded.
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

            MarkChunkNeedsSync(chunkCoord);
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

        public bool TickSeasonChanged(ICoreAPI api)
        {
            if (api == null || !EcosystemConfig.Loaded.EnableSeasonalFoliage) return false;
            if (!FoliageSyncModeHelper.UsesChunkSync(EcosystemConfig.Loaded)) return false;

            int seasonKey = FoliageSeasonKey.Current(api);
            if (seasonKey == lastSeasonKey) return false;

            lastSeasonKey = seasonKey;
            InvalidateAllTrackedChunks();
            return true;
        }

        public bool NeedsChunkSync(Vec2i chunkCoord, int seasonKey)
        {
            if (!EcosystemConfig.Loaded.EnableSeasonalFoliage) return false;
            if (!FoliageSyncModeHelper.UsesChunkSync(EcosystemConfig.Loaded)) return false;

            long key = FoliageCellIndex.ChunkKey(chunkCoord.X, chunkCoord.Y);
            if (!chunkStates.TryGetValue(key, out FoliageChunkState state)) return true;

            return !state.Completed || state.SyncedSeasonKey != seasonKey;
        }

        public void ApplyEcologyPassResult(Vec2i chunkCoord, ChunkEcologyColumnPass.Result result, int seasonKey)
        {
            if (!EcosystemConfig.Loaded.EnableSeasonalFoliage) return;
            if (!FoliageSyncModeHelper.UsesChunkSync(EcosystemConfig.Loaded)) return;

            long key = FoliageCellIndex.ChunkKey(chunkCoord.X, chunkCoord.Y);
            totalSyncedCells += result.FoliageIndexed;

            if (result.FoliageChanged > 0)
            {
                autumnStripTotal += result.FoliageChanged;
            }

            if (result.Completed)
            {
                chunkStates[key] = new FoliageChunkState
                {
                    SyncedSeasonKey = seasonKey,
                    Completed = true,
                    LastIndexed = result.FoliageIndexed,
                    LastChanged = result.FoliageChanged,
                };
            }
            else
            {
                chunkStates[key] = new FoliageChunkState
                {
                    SyncedSeasonKey = -1,
                    ResumeLx = result.ResumeLx,
                    ResumeLz = result.ResumeLz,
                    ResumeY = result.ResumeY,
                    Completed = false,
                    LastIndexed = result.FoliageIndexed,
                    LastChanged = result.FoliageChanged,
                };
            }
        }

        public int ProcessRandomTick(
            ICoreAPI api,
            ICollection<Vec2i> activeChunks,
            long budgetTicks,
            System.Diagnostics.Stopwatch budgetWatch)
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

        void MarkChunkNeedsSync(Vec2i chunkCoord)
        {
            long key = FoliageCellIndex.ChunkKey(chunkCoord.X, chunkCoord.Y);
            if (!chunkStates.TryGetValue(key, out FoliageChunkState state))
            {
                state = new FoliageChunkState { SyncedSeasonKey = -1, Completed = false, ResumeY = -1 };
            }
            else
            {
                state.Completed = false;
                state.SyncedSeasonKey = -1;
                state.ResumeY = -1;
                state.ResumeLx = 0;
                state.ResumeLz = 0;
            }

            chunkStates[key] = state;
            RequestEcologyScan?.Invoke(chunkCoord);
        }

        void InvalidateChunkAt(BlockPos pos)
        {
            if (pos == null) return;
            int cs = GlobalConstants.ChunkSize;
            MarkChunkNeedsSync(new Vec2i(pos.X / cs, pos.Z / cs));
        }

        void InvalidateAllTrackedChunks()
        {
            var coords = new List<Vec2i>();
            foreach (long key in chunkStates.Keys)
            {
                FoliageChunkState state = chunkStates[key];
                state.Completed = false;
                state.SyncedSeasonKey = -1;
                state.ResumeY = -1;
                state.ResumeLx = 0;
                state.ResumeLz = 0;
                chunkStates[key] = state;

                int cx = (int)(key >> 32);
                int cz = (int)(key & 0xFFFFFFFF);
                coords.Add(new Vec2i(cx, cz));
            }

            for (int i = 0; i < coords.Count; i++)
            {
                RequestEcologyScan?.Invoke(coords[i]);
            }
        }

        void MaybeLogProgress(ICoreAPI api, EcosystemConfig cfg, int changedThisTick)
        {
            if (api == null || changedThisTick <= 0) return;

            if (cfg.ReproduceDebug)
            {
                api.Logger.Notification(
                    "[ecosystemflora] Foliage: {0} cell(s) updated, {1} chunk(s) pending sync, mode={2}",
                    changedThisTick,
                    PendingSyncChunks,
                    cfg.FoliageSyncMode ?? "chunk");
                return;
            }

            IGameCalendar cal = api.World?.Calendar;
            if (cal == null) return;

            double now = cal.TotalHours;
            if (now - lastAutumnProgressLogHours < 1) return;

            lastAutumnProgressLogHours = now;
            api.Logger.Notification(
                "[ecosystemflora] Foliage sync: {0} block(s) this pass, {1} chunk(s) pending",
                changedThisTick,
                PendingSyncChunks);
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

        readonly List<Vec2i> pendingSyncScratch = new List<Vec2i>();

        /// <summary>Chunk column foliage sync (main thread). Used when ecology registration runs off-thread.</summary>
        public int ProcessChunkSyncBatch(
            ICoreAPI api,
            int seasonKey,
            int maxChunks,
            long passDeadlineTicks,
            long totalBudgetTicks,
            System.Diagnostics.Stopwatch budgetWatch,
            System.Action<Vec2i, int> onFoliageChanged = null)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EnableSeasonalFoliage || api?.World?.BlockAccessor == null) return 0;
            if (!FoliageSyncModeHelper.UsesChunkSync(cfg) || maxChunks <= 0) return 0;

            IBlockAccessor acc = api.World.BlockAccessor;
            CollectPendingSyncChunks(seasonKey, pendingSyncScratch);
            if (pendingSyncScratch.Count == 0) return 0;

            int chunksProcessed = 0;
            int changedThisTick = 0;

            for (int i = 0; i < pendingSyncScratch.Count && chunksProcessed < maxChunks; i++)
            {
                if (totalBudgetTicks > 0 && budgetWatch != null && budgetWatch.ElapsedTicks >= totalBudgetTicks)
                {
                    break;
                }

                Vec2i chunkCoord = pendingSyncScratch[i];
                if (acc.GetMapChunk(chunkCoord.X, chunkCoord.Y) == null) continue;

                long chunkDeadline = passDeadlineTicks;
                if (totalBudgetTicks > 0 && budgetWatch != null)
                {
                    long remaining = totalBudgetTicks - budgetWatch.ElapsedTicks;
                    if (remaining <= 0) break;

                    long totalDeadline = System.Diagnostics.Stopwatch.GetTimestamp() + remaining;
                    chunkDeadline = passDeadlineTicks > 0 && passDeadlineTicks < long.MaxValue
                        ? System.Math.Min(passDeadlineTicks, totalDeadline)
                        : totalDeadline;
                }

                long key = FoliageCellIndex.ChunkKey(chunkCoord.X, chunkCoord.Y);
                if (!chunkStates.TryGetValue(key, out FoliageChunkState state))
                {
                    state = new FoliageChunkState { SyncedSeasonKey = -1, Completed = false, ResumeY = -1 };
                }

                int resumeY = state.ResumeY;
                if (state.Completed || resumeY < 0) resumeY = -1;

                FoliageChunkSyncPass.Result pass = FoliageChunkSyncPass.Run(
                    api,
                    acc,
                    chunkCoord,
                    Index,
                    state.ResumeLx,
                    state.ResumeLz,
                    resumeY,
                    chunkDeadline);

                var ecologyResult = new ChunkEcologyColumnPass.Result(
                    null,
                    null,
                    null,
                    null,
                    pass.Indexed,
                    pass.Changed,
                    pass.ResumeLx,
                    pass.ResumeLz,
                    pass.ResumeY,
                    pass.Completed);

                ApplyEcologyPassResult(chunkCoord, ecologyResult, seasonKey);
                chunksProcessed++;

                if (pass.Changed > 0)
                {
                    changedThisTick += pass.Changed;
                    onFoliageChanged?.Invoke(chunkCoord, pass.Changed);
                }
            }

            if (changedThisTick > 0)
            {
                MaybeLogProgress(api, cfg, changedThisTick);
            }

            return chunksProcessed;
        }

        void CollectPendingSyncChunks(int seasonKey, List<Vec2i> output)
        {
            output.Clear();
            foreach (KeyValuePair<long, FoliageChunkState> kv in chunkStates)
            {
                FoliageChunkState state = kv.Value;
                if (state.Completed && state.SyncedSeasonKey == seasonKey) continue;

                int cx = (int)(kv.Key >> 32);
                int cz = (int)(kv.Key & 0xFFFFFFFF);
                output.Add(new Vec2i(cx, cz));
            }
        }
    }
}
