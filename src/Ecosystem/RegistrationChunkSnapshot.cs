using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Immutable block-id grid for one chunk column scan on a worker thread.</summary>
    internal sealed class RegistrationChunkSnapshot
    {
        public const int ChunkSize = GlobalConstants.ChunkSize;

        public Vec2i ChunkCoord { get; }
        /// <summary>World map height (for validity checks).</summary>
        public int MapSizeY { get; }
        /// <summary>Captured vertical stride (max flora scan top + 1); may be ≪ MapSizeY.</summary>
        public int YStride { get; }
        public ushort[] RainHeightMap { get; }
        public int[] BlockIds { get; }

        public RegistrationChunkSnapshot(
            Vec2i chunkCoord,
            int mapSizeY,
            int yStride,
            ushort[] rainHeightMap,
            int[] blockIds)
        {
            ChunkCoord = chunkCoord;
            MapSizeY = mapSizeY;
            YStride = yStride > 0 ? yStride : 1;
            RainHeightMap = rainHeightMap;
            BlockIds = blockIds;
        }

        /// <summary>Legacy ctor: full-height stride equal to MapSizeY.</summary>
        public RegistrationChunkSnapshot(Vec2i chunkCoord, int mapSizeY, ushort[] rainHeightMap, int[] blockIds)
            : this(chunkCoord, mapSizeY, mapSizeY, rainHeightMap, blockIds)
        {
        }

        public static int CellIndex(int lx, int lz, int y, int yStride) =>
            (lx * ChunkSize + lz) * yStride + y;

        public int GetBlockId(int lx, int lz, int y)
        {
            if (lx < 0 || lz < 0 || lx >= ChunkSize || lz >= ChunkSize || y < 0 || y >= YStride)
            {
                return 0;
            }

            return BlockIds[CellIndex(lx, lz, y, YStride)];
        }
    }

    internal sealed class RegistrationChunkSnapshotBuilder
    {
        static readonly BlockPos scratch = new BlockPos(0);

        public Vec2i ChunkCoord { get; }
        public bool Completed { get; private set; }

        int resumeLx;
        int resumeLz;
        int resumeY;
        int mapSizeY;
        int yStride;
        ushort[] rainHeightMap;
        int[] blockIds;

        public RegistrationChunkSnapshotBuilder(Vec2i chunkCoord)
        {
            ChunkCoord = chunkCoord;
        }

        public RegistrationChunkSnapshot Snapshot { get; private set; }

        public ushort[] RainHeightMap => rainHeightMap;

        public int YStride => yStride;

        public bool Advance(IBlockAccessor acc, IMapChunk mapChunk, int maxCells, long deadlineTicks)
        {
            if (Completed || acc == null || mapChunk == null) return Completed;

            int cs = RegistrationChunkSnapshot.ChunkSize;
            if (blockIds == null)
            {
                mapSizeY = acc.MapSizeY;
                rainHeightMap = CopyRainHeightMap(mapChunk.RainHeightMap, cs);
                yStride = ComputeYStride(rainHeightMap, cs, mapSizeY - 1) + 1;
                if (yStride < 1) yStride = 1;
                blockIds = new int[cs * cs * yStride];
            }

            int columnTop = mapSizeY - 1;
            int x0 = ChunkCoord.X * cs;
            int z0 = ChunkCoord.Y * cs;
            int cells = 0;

            for (int lx = resumeLx; lx < cs; lx++)
            {
                int lzStart = lx == resumeLx ? resumeLz : 0;
                for (int lz = lzStart; lz < cs; lz++)
                {
                    resumeLx = lx;
                    resumeLz = lz;

                    int x = x0 + lx;
                    int z = z0 + lz;
                    int topY = ChunkColumnWalker.GetFloraRegistrationScanTopY(
                        rainHeightMap, lx, lz, cs, columnTop);
                    if (topY >= yStride) topY = yStride - 1;

                    int surfaceY = 0;
                    if (rainHeightMap != null && rainHeightMap.Length >= cs * cs)
                    {
                        surfaceY = rainHeightMap[lz * cs + lx];
                    }

                    int band = EcosystemConfig.Loaded?.RegistrationSnapshotBandBelowSurface ?? 24;
                    int yMin = 0;
                    if (band > 0 && surfaceY > 0)
                    {
                        yMin = surfaceY - band;
                        if (yMin < 0) yMin = 0;
                    }

                    int yStart = lx == resumeLx && lz == resumeLz && resumeY > 0 ? resumeY : yMin;
                    if (yStart < yMin) yStart = yMin;
                    for (int y = yStart; y <= topY; y++)
                    {
                        if (maxCells > 0 && cells >= maxCells)
                        {
                            resumeY = y;
                            return false;
                        }

                        if (deadlineTicks > 0 && StopwatchHelper.IsPast(deadlineTicks))
                        {
                            resumeY = y;
                            return false;
                        }

                        scratch.Set(x, y, z);
                        int idx = RegistrationChunkSnapshot.CellIndex(lx, lz, y, yStride);
                        Block live = acc.GetBlock(scratch);
                        blockIds[idx] = live?.Id ?? 0;
                        cells++;
                    }

                    resumeY = 0;
                    resumeLz = lz + 1;
                }

                resumeLz = 0;
            }

            Completed = true;
            Snapshot = new RegistrationChunkSnapshot(ChunkCoord, mapSizeY, yStride, rainHeightMap, blockIds);
            return true;
        }

        /// <summary>Max flora-scan top Y across the chunk (from rain heightmap only — no GetBlock).</summary>
        internal static int ComputeYStride(ushort[] rainHeightMap, int chunkSize, int mapTopY)
        {
            int maxTop = 0;
            for (int lx = 0; lx < chunkSize; lx++)
            {
                for (int lz = 0; lz < chunkSize; lz++)
                {
                    int top = ChunkColumnWalker.GetFloraRegistrationScanTopY(
                        rainHeightMap, lx, lz, chunkSize, mapTopY);
                    if (top > maxTop) maxTop = top;
                }
            }

            return maxTop;
        }

        static ushort[] CopyRainHeightMap(ushort[] source, int chunkSize)
        {
            int len = chunkSize * chunkSize;
            var copy = new ushort[len];
            if (source != null && source.Length >= len)
            {
                System.Array.Copy(source, copy, len);
            }

            return copy;
        }
    }

    internal static class StopwatchHelper
    {
        public static bool IsPast(long deadlineTicks) =>
            System.Diagnostics.Stopwatch.GetTimestamp() >= deadlineTicks;
    }
}
