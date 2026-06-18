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
        public int MapSizeY { get; }
        public ushort[] RainHeightMap { get; }
        public int[] BlockIds { get; }

        public RegistrationChunkSnapshot(Vec2i chunkCoord, int mapSizeY, ushort[] rainHeightMap, int[] blockIds)
        {
            ChunkCoord = chunkCoord;
            MapSizeY = mapSizeY;
            RainHeightMap = rainHeightMap;
            BlockIds = blockIds;
        }

        public static int CellIndex(int lx, int lz, int y, int mapSizeY) =>
            (lx * ChunkSize + lz) * mapSizeY + y;

        public int GetBlockId(int lx, int lz, int y)
        {
            if (lx < 0 || lz < 0 || lx >= ChunkSize || lz >= ChunkSize || y < 0 || y >= MapSizeY)
            {
                return 0;
            }

            return BlockIds[CellIndex(lx, lz, y, MapSizeY)];
        }
    }

    internal sealed class RegistrationChunkSnapshotBuilder
    {
        static readonly BlockPos scratch = new BlockPos(0);

        public Vec2i ChunkCoord { get; }
        public bool Completed { get; private set; }

        int resumeLx;
        int resumeLz;
        int mapSizeY;
        ushort[] rainHeightMap;
        int[] blockIds;

        public RegistrationChunkSnapshotBuilder(Vec2i chunkCoord)
        {
            ChunkCoord = chunkCoord;
        }

        public RegistrationChunkSnapshot Snapshot { get; private set; }

        public ushort[] RainHeightMap => rainHeightMap;

        public bool Advance(IBlockAccessor acc, IMapChunk mapChunk, int maxCells, long deadlineTicks)
        {
            if (Completed || acc == null || mapChunk == null) return Completed;

            int cs = RegistrationChunkSnapshot.ChunkSize;
            if (blockIds == null)
            {
                mapSizeY = acc.MapSizeY;
                rainHeightMap = CopyRainHeightMap(mapChunk.RainHeightMap, cs);
                blockIds = new int[cs * cs * mapSizeY];
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
                    int topY = ChunkColumnWalker.GetColumnTopY(mapChunk, lx, lz, cs, columnTop);

                    for (int y = 0; y <= topY; y++)
                    {
                        if (maxCells > 0 && cells >= maxCells) return false;
                        if (deadlineTicks > 0 && StopwatchHelper.IsPast(deadlineTicks)) return false;

                        scratch.Set(x, y, z);
                        int idx = RegistrationChunkSnapshot.CellIndex(lx, lz, y, mapSizeY);
                        Block live = acc.GetBlock(scratch);
                        blockIds[idx] = live?.Id ?? 0;
                        cells++;
                    }

                    resumeLz = lz + 1;
                }

                resumeLz = 0;
            }

            Completed = true;
            Snapshot = new RegistrationChunkSnapshot(ChunkCoord, mapSizeY, rainHeightMap, blockIds);
            return true;
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
