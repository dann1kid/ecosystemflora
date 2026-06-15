using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace WildFarming.Ecosystem
{
    /// <summary>Finds ecology parents (flowers, log-grown trees, …) per column instead of walking every block.</summary>
    internal static class ChunkFlowerScanner
    {
        public static ChunkScanResult ScanChunk(
            Vec2i chunkCoord,
            IBlockAccessor acc,
            int maxHits,
            int startLx = 0,
            int startLz = 0)
        {
            var hits = new List<ChunkFlowerHit>();
            if (maxHits <= 0)
            {
                return new ChunkScanResult(hits, startLx, startLz, completed: false);
            }

            int chunkSize = GlobalConstants.ChunkSize;
            int x0 = chunkCoord.X * chunkSize;
            int z0 = chunkCoord.Y * chunkSize;

            IMapChunk mapChunk = acc.GetMapChunk(chunkCoord.X, chunkCoord.Y);
            int fallbackY = acc.MapSizeY - 1;

            int resumeLx = startLx;
            int resumeLz = startLz;

            for (int lx = startLx; lx < chunkSize; lx++)
            {
                int lzStart = lx == startLx ? startLz : 0;
                for (int lz = lzStart; lz < chunkSize; lz++)
                {
                    resumeLx = lx;
                    resumeLz = lz;

                    int x = x0 + lx;
                    int z = z0 + lz;
                    int topY = GetSurfaceY(mapChunk, lx, lz, chunkSize, fallbackY);

                    if (TryFindTopFlower(acc, x, z, topY, out Block block, out BlockPos pos))
                    {
                        hits.Add(new ChunkFlowerHit(pos, block.Code));
                        if (hits.Count >= maxHits)
                        {
                            return AdvanceCursor(chunkSize, lx, lz, hits);
                        }
                    }
                }
            }

            return new ChunkScanResult(hits, 0, 0, completed: true);
        }

        static ChunkScanResult AdvanceCursor(int chunkSize, int lx, int lz, List<ChunkFlowerHit> hits)
        {
            lz++;
            if (lz >= chunkSize)
            {
                lx++;
                lz = 0;
            }

            if (lx >= chunkSize)
            {
                return new ChunkScanResult(hits, 0, 0, completed: true);
            }

            return new ChunkScanResult(hits, lx, lz, completed: false);
        }

        static int GetSurfaceY(IMapChunk mapChunk, int lx, int lz, int chunkSize, int fallbackY)
        {
            if (mapChunk == null) return fallbackY;

            ushort[] heightmap = mapChunk.RainHeightMap;
            if (heightmap == null || heightmap.Length < chunkSize * chunkSize) return fallbackY;

            int surfaceY = heightmap[lz * chunkSize + lx];
            return surfaceY + 2;
        }

        static readonly BlockPos scanScratch = new BlockPos(0);

        static bool TryFindTopFlower(IBlockAccessor acc, int x, int z, int topY, out Block block, out BlockPos pos)
        {
            block = null;
            pos = null;

            for (int y = topY; y >= 0; y--)
            {
                scanScratch.Set(x, y, z);
                block = acc.GetBlock(scanScratch);
                if (block.Id == 0) continue;

                if (EcologyAttributes.ReproduceEnabled(block))
                {
                    pos = scanScratch.Copy();
                    return true;
                }

                if (!PlantVacancyRules.IsPassThroughForColumnScan(block))
                {
                    return false;
                }
            }

            return false;
        }
    }

    internal readonly struct ChunkScanResult
    {
        public List<ChunkFlowerHit> Hits { get; }
        public int ResumeLx { get; }
        public int ResumeLz { get; }
        public bool Completed { get; }

        public ChunkScanResult(List<ChunkFlowerHit> hits, int resumeLx, int resumeLz, bool completed)
        {
            Hits = hits;
            ResumeLx = resumeLx;
            ResumeLz = resumeLz;
            Completed = completed;
        }
    }

    internal readonly struct ChunkFlowerHit
    {
        public BlockPos Pos { get; }
        public AssetLocation BlockCode { get; }

        public ChunkFlowerHit(BlockPos pos, AssetLocation blockCode)
        {
            Pos = pos;
            BlockCode = blockCode;
        }
    }

    internal readonly struct PendingChunkScan
    {
        public Vec2i ChunkCoord { get; }
        public int NextLx { get; }
        public int NextLz { get; }
        public int NextY { get; }

        public PendingChunkScan(Vec2i chunkCoord, int nextLx = 0, int nextLz = 0, int nextY = -1)
        {
            ChunkCoord = chunkCoord;
            NextLx = nextLx;
            NextLz = nextLz;
            NextY = nextY;
        }
    }
}
