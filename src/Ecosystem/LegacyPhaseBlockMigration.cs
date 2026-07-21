using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Remaps legacy bare fern phase block codes (pre cover-variant migration)
    /// to <c>-free</c> cover variants. Scans only a band around the rain surface —
    /// never the full MapSizeY volume. Work is paced across frames so mass chunk
    /// loads (high view distance) do not remap every column in one hitch.
    /// </summary>
    internal static class LegacyPhaseBlockMigration
    {
        const int BandBelowSurface = 2;
        const int BandAboveSurface = 8;
        const int RemapBudgetMs = 2;
        const int PumpIntervalMs = 40;
        const int MaxQueue = 512;

        struct RemapJob
        {
            public Vec2i Coord;
            public int NextLx;
            public int NextLz;
        }

        static readonly Queue<RemapJob> pending = new Queue<RemapJob>();
        static readonly HashSet<long> queuedKeys = new HashSet<long>();
        static bool pumpScheduled;

        public static void ScheduleRemapColumn(ICoreAPI api, Vec2i chunkCoord)
        {
            if (api?.World?.BlockAccessor == null || chunkCoord == null) return;

            long key = ChunkKey(chunkCoord);
            if (!queuedKeys.Add(key)) return;
            if (pending.Count >= MaxQueue)
            {
                queuedKeys.Remove(key);
                return;
            }

            pending.Enqueue(new RemapJob
            {
                Coord = chunkCoord.Copy(),
                NextLx = 0,
                NextLz = 0,
            });
            EnsurePump(api, ChunkLoadDeferral.RemapDelayMs(chunkCoord));
        }

        static void EnsurePump(ICoreAPI api, int delayMs)
        {
            if (pumpScheduled || api == null) return;
            pumpScheduled = true;
            if (delayMs < 0) delayMs = 0;
            api.Event.RegisterCallback(_ => Pump(api), delayMs);
        }

        static void Pump(ICoreAPI api)
        {
            pumpScheduled = false;
            if (api?.World?.BlockAccessor == null)
            {
                pending.Clear();
                queuedKeys.Clear();
                return;
            }

            long deadline = Stopwatch.GetTimestamp()
                + RemapBudgetMs * Stopwatch.Frequency / 1000;

            while (pending.Count > 0)
            {
                RemapJob job = pending.Dequeue();
                long key = ChunkKey(job.Coord);
                queuedKeys.Remove(key);

                if (!AdvanceRemap(api, ref job, deadline))
                {
                    // Remainder — requeue and stop this frame.
                    if (queuedKeys.Add(key))
                    {
                        pending.Enqueue(job);
                    }

                    break;
                }

                if (Stopwatch.GetTimestamp() >= deadline) break;
            }

            if (pending.Count > 0)
            {
                EnsurePump(api, PumpIntervalMs);
            }
        }

        /// <summary>
        /// Returns true when the column finished; false when paused mid-column (job updated).
        /// </summary>
        static bool AdvanceRemap(ICoreAPI api, ref RemapJob job, long deadline)
        {
            IBlockAccessor acc = api.World?.BlockAccessor;
            if (acc == null) return true;
            if (acc.GetMapChunk(job.Coord.X, job.Coord.Y) == null) return true;

            int cs = GlobalConstants.ChunkSize;
            int baseX = job.Coord.X * cs;
            int baseZ = job.Coord.Y * cs;
            IMapChunk mapChunk = acc.GetMapChunk(job.Coord.X, job.Coord.Y);
            ushort[] heightmap = mapChunk?.RainHeightMap;
            int mapTop = acc.MapSizeY - 1;
            var pos = new BlockPos(0);

            for (int lx = job.NextLx; lx < cs; lx++)
            {
                int lzStart = lx == job.NextLx ? job.NextLz : 0;
                for (int lz = lzStart; lz < cs; lz++)
                {
                    if (Stopwatch.GetTimestamp() >= deadline)
                    {
                        job.NextLx = lx;
                        job.NextLz = lz;
                        return false;
                    }

                    int surfaceY = 64;
                    if (heightmap != null && heightmap.Length >= cs * cs)
                    {
                        surfaceY = heightmap[lz * cs + lx];
                    }

                    int yMin = surfaceY - BandBelowSurface;
                    if (yMin < 0) yMin = 0;
                    int yMax = surfaceY + BandAboveSurface;
                    if (yMax > mapTop) yMax = mapTop;

                    for (int y = yMin; y <= yMax; y++)
                    {
                        pos.Set(baseX + lx, y, baseZ + lz);
                        Block block = acc.GetBlock(pos);
                        if (block == null || block.Id == 0) continue;

                        TryRemapAt(acc, pos, block);
                        block = acc.GetBlock(pos);
                        if (PlantSnowCover.ShouldSyncCoverVariant(block?.Code))
                        {
                            PlantSnowCoverSync.TrySyncCover(api, pos, block);
                        }
                    }
                }
            }

            return true;
        }

        static void TryRemapAt(IBlockAccessor acc, BlockPos pos, Block block)
        {
            AssetLocation targetCode = ResolveRemapTarget(block?.Code);
            if (targetCode == null) return;

            Block target = acc.GetBlock(targetCode);
            if (target == null || target.Id == 0 || target.Id == block.Id) return;

            acc.SetBlock(target.Id, pos);
        }

        internal static AssetLocation ResolveRemapTarget(AssetLocation code)
        {
            if (code?.Domain != "ecosystemflora" || code.Path == null) return null;
            if (!code.Path.StartsWith("fernphase-")) return null;
            if (!PlantSnowCover.IsLegacyBareFernPhasePath(code.Path)) return null;

            return new AssetLocation(
                code.Domain,
                code.Path + JuvenileBlockNaming.FreeSuffix);
        }

        static long ChunkKey(Vec2i chunkCoord) =>
            ((long)chunkCoord.X << 32) | (uint)chunkCoord.Y;

        /// <summary>Test hook — pending remap columns.</summary>
        internal static int PendingCountForTests => pending.Count;

        /// <summary>Test hook — clear paced queue between cases.</summary>
        internal static void ClearPendingForTests()
        {
            pending.Clear();
            queuedKeys.Clear();
            pumpScheduled = false;
        }
    }
}
