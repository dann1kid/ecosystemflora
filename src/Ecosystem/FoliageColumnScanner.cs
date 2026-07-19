using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Finds deciduous log-grown / branchy / regular leaf cells per column.</summary>
    internal static class FoliageColumnScanner
    {
        static readonly BlockPos scanScratch = new BlockPos(0);

        /// <summary>Legacy random-mode scan: index + patchy catch-up rolls.</summary>
        public static int ScanChunk(
            IBlockAccessor acc,
            Vec2i chunkCoord,
            FoliageCellIndex index,
            ICoreAPI api = null,
            int maxHits = int.MaxValue,
            int maxCatchUpOps = int.MaxValue)
        {
            if (acc == null || index == null || maxHits <= 0) return 0;

            int chunkSize = GlobalConstants.ChunkSize;
            int x0 = chunkCoord.X * chunkSize;
            int z0 = chunkCoord.Y * chunkSize;
            IMapChunk mapChunk = acc.GetMapChunk(chunkCoord.X, chunkCoord.Y);
            int columnTop = acc.MapSizeY - 1;
            int found = 0;
            int catchUpOps = 0;
            bool catchUp = api != null && EcosystemConfig.Loaded.FoliageCatchUpOnChunkLoad;

            for (int lx = 0; lx < chunkSize; lx++)
            {
                for (int lz = 0; lz < chunkSize; lz++)
                {
                    int x = x0 + lx;
                    int z = z0 + lz;
                    int topY = ChunkColumnWalker.GetColumnTopY(mapChunk, lx, lz, chunkSize, columnTop);

                    for (int y = topY; y >= 0; y--)
                    {
                        scanScratch.Set(x, y, z);
                        Block block = acc.GetBlock(scanScratch);
                        if (block.Id == 0) continue;

                        if (CanopyFoliageRules.IsSeasonalFoliageBlock(block))
                        {
                            if (catchUp
                                && catchUpOps < maxCatchUpOps
                                && CanopyFoliageRules.TryCatchUpStripOnScan(api, acc, scanScratch, block))
                            {
                                catchUpOps++;
                                EcosystemSystem.Instance?.InvalidateEnvironmentAround(scanScratch, floraRadius: 2);
                                continue;
                            }

                            if (catchUp
                                && catchUpOps < maxCatchUpOps
                                && CanopyFoliageRules.TryCatchUpBudOnScan(api, acc, scanScratch, block, index))
                            {
                                catchUpOps++;
                                EcosystemSystem.Instance?.InvalidateEnvironmentAround(scanScratch, floraRadius: 2);
                            }

                            index.Add(scanScratch);
                            found++;
                            if (found >= maxHits) return found;
                        }

                        if (ChunkColumnWalker.ContinueColumnScan(block))
                        {
                            continue;
                        }

                        break;
                    }
                }
            }

            return found;
        }

        internal static bool ContinueColumnScan(Block block) => ChunkColumnWalker.ContinueColumnScan(block);
    }
}
