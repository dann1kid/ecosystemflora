using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Column scan for log-grown trunk bases (surface + crown depth).</summary>
    internal static class TreeTrunkDiscovery
    {
        /// <summary>Return true when the trunk was newly registered (counts toward maxHits).</summary>
        public delegate bool TrunkFoundHandler(BlockPos basePos, string wood);

        public delegate bool FerntreeFoundHandler(BlockPos basePos);

        static readonly BlockPos scanScratch = new BlockPos(0);

        public readonly struct ScanResult
        {
            public readonly int TreesFound;
            public readonly int ColumnsScanned;
            public readonly int ResumeLx;
            public readonly int ResumeLz;
            public readonly bool Completed;

            public ScanResult(int treesFound, int columnsScanned, int resumeLx, int resumeLz, bool completed)
            {
                TreesFound = treesFound;
                ColumnsScanned = columnsScanned;
                ResumeLx = resumeLx;
                ResumeLz = resumeLz;
                Completed = completed;
            }
        }

        public static int ScanChunk(
            IBlockAccessor acc,
            Vec2i chunkCoord,
            TrunkFoundHandler onFound,
            int maxHits,
            System.Func<string, bool> woodFilter = null,
            int startLx = 0,
            int startLz = 0)
        {
            int chunkSize = GlobalConstants.ChunkSize;
            ScanResult result = ScanChunkColumns(
                acc,
                chunkCoord,
                onFound,
                maxHits,
                chunkSize * chunkSize,
                startLx,
                startLz,
                woodFilter);
            return result.TreesFound;
        }

        public static ScanResult ScanChunkColumns(
            IBlockAccessor acc,
            Vec2i chunkCoord,
            TrunkFoundHandler onFound,
            int maxHits,
            int maxColumns,
            int startLx = 0,
            int startLz = 0,
            System.Func<string, bool> woodFilter = null)
        {
            if (acc == null || onFound == null || maxHits <= 0 || maxColumns <= 0)
            {
                return new ScanResult(0, 0, startLx, startLz, completed: true);
            }

            int chunkSize = GlobalConstants.ChunkSize;
            int x0 = chunkCoord.X * chunkSize;
            int z0 = chunkCoord.Y * chunkSize;
            IMapChunk mapChunk = acc.GetMapChunk(chunkCoord.X, chunkCoord.Y);
            int fallbackY = acc.MapSizeY - 1;
            int found = 0;
            int columnsScanned = 0;

            for (int lx = startLx; lx < chunkSize; lx++)
            {
                int lzStart = lx == startLx ? startLz : 0;
                for (int lz = lzStart; lz < chunkSize; lz++)
                {
                    columnsScanned++;

                    int x = x0 + lx;
                    int z = z0 + lz;
                    int topY = GetSurfaceY(mapChunk, lx, lz, chunkSize, fallbackY);

                    if (TryFindTrunkBase(acc, x, z, topY, out BlockPos basePos, out string wood))
                    {
                        if (woodFilter != null && !woodFilter(wood)) continue;

                        if (onFound(basePos, wood))
                        {
                            found++;
                        }

                        if (found >= maxHits)
                        {
                            int nextLz = lz + 1;
                            int nextLx = lx;
                            if (nextLz >= chunkSize)
                            {
                                nextLx++;
                                nextLz = 0;
                            }

                            bool completed = nextLx >= chunkSize;
                            return new ScanResult(
                                found,
                                columnsScanned,
                                completed ? 0 : nextLx,
                                completed ? 0 : nextLz,
                                completed);
                        }
                    }

                    if (columnsScanned >= maxColumns)
                    {
                        int nextLz = lz + 1;
                        int nextLx = lx;
                        if (nextLz >= chunkSize)
                        {
                            nextLx++;
                            nextLz = 0;
                        }

                        bool chunkDone = nextLx >= chunkSize;
                        return new ScanResult(
                            found,
                            columnsScanned,
                            chunkDone ? 0 : nextLx,
                            chunkDone ? 0 : nextLz,
                            chunkDone);
                    }
                }
            }

            return new ScanResult(found, columnsScanned, 0, 0, completed: true);
        }

        static int GetSurfaceY(IMapChunk mapChunk, int lx, int lz, int chunkSize, int fallbackY)
        {
            if (mapChunk == null) return fallbackY;

            ushort[] heightmap = mapChunk.RainHeightMap;
            if (heightmap == null || heightmap.Length < chunkSize * chunkSize) return fallbackY;

            return heightmap[lz * chunkSize + lx] + 28;
        }

        static bool TryFindTrunkBase(IBlockAccessor acc, int x, int z, int topY, out BlockPos basePos, out string wood)
        {
            basePos = null;
            wood = null;

            for (int y = topY; y >= 0; y--)
            {
                scanScratch.Set(x, y, z);
                Block block = acc.GetBlock(scanScratch);
                if (block.Id == 0) continue;

                if (PlantCodeHelper.IsTreeLogGrownBlock(block))
                {
                    wood = PlantCodeHelper.GetTreeWood(block);
                    if (string.IsNullOrEmpty(wood) || !WildTreeEcology.TryGet(wood, out _))
                    {
                        return false;
                    }

                    basePos = PlantCodeHelper.GetTreeTrunkBase(acc, scanScratch);
                    return true;
                }

                if (PlantCodeHelper.IsFerntreeTrunkBlock(block))
                {
                    wood = WildFerntreeEcology.Species;
                    basePos = FerntreeStructure.GetTrunkBase(acc, scanScratch);
                    return true;
                }

                if (CanopyBlockHelper.IsRegularLeaf(block) || CanopyBlockHelper.IsBranchyLeaf(block))
                {
                    if (!string.IsNullOrEmpty(CanopyBlockHelper.GetWoodFromFoliageBlock(block))) continue;
                }

                if (!PlantVacancyRules.IsPassThroughForColumnScan(block))
                {
                    return false;
                }
            }

            return false;
        }
    }
}
