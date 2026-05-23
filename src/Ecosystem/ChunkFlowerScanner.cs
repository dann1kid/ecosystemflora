using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Finds ecology parents (flowers, log-grown trees, …) per column instead of walking every block.</summary>
    internal static class ChunkFlowerScanner
    {
        public static List<ChunkFlowerHit> ScanColumn(Vec2i chunkCoord, IBlockAccessor acc, int maxHits)
        {
            var hits = new List<ChunkFlowerHit>();
            if (maxHits <= 0) return hits;

            int chunkSize = GlobalConstants.ChunkSize;
            int x0 = chunkCoord.X * chunkSize;
            int z0 = chunkCoord.Y * chunkSize;
            int yMax = acc.MapSizeY - 1;

            for (int lx = 0; lx < chunkSize; lx++)
            {
                for (int lz = 0; lz < chunkSize; lz++)
                {
                    int x = x0 + lx;
                    int z = z0 + lz;

                    if (TryFindTopFlower(acc, x, z, yMax, out Block block, out BlockPos pos))
                    {
                        hits.Add(new ChunkFlowerHit(pos, block.Code));
                        if (hits.Count >= maxHits) return hits;
                    }
                }
            }

            return hits;
        }

        static bool TryFindTopFlower(IBlockAccessor acc, int x, int z, int yMax, out Block block, out BlockPos pos)
        {
            block = null;
            pos = null;

            for (int y = yMax; y >= 0; y--)
            {
                pos = new BlockPos(x, y, z);
                block = acc.GetBlock(pos);
                if (block.Id == 0) continue;

                if (EcologyAttributes.ReproduceEnabled(block))
                {
                    return true;
                }

                if (block.Replaceable < SuitabilityEvaluator.ReproduceMinReplaceable)
                {
                    return false;
                }
            }

            return false;
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
}
