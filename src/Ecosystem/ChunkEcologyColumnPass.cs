using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Single rain-heightmap column pass: flowers, tree trunks, and foliage season sync.</summary>
    internal static class ChunkEcologyColumnPass
    {
        static readonly BlockPos scanScratch = new BlockPos(0);

        public readonly struct Request
        {
            public int MaxFlowerHits { get; init; }
            public int MaxTreeHits { get; init; }
            public bool SyncFoliage { get; init; }
            public FoliageCellIndex FoliageIndex { get; init; }
        }

        public readonly struct Result
        {
            public readonly List<ChunkFlowerHit> FlowerHits;
            public readonly int TreesRegistered;
            public readonly int FoliageIndexed;
            public readonly int FoliageChanged;
            public readonly int ResumeLx;
            public readonly int ResumeLz;
            public readonly int ResumeY;
            public readonly bool Completed;

            public Result(
                List<ChunkFlowerHit> flowerHits,
                int treesRegistered,
                int foliageIndexed,
                int foliageChanged,
                int resumeLx,
                int resumeLz,
                int resumeY,
                bool completed)
            {
                FlowerHits = flowerHits;
                TreesRegistered = treesRegistered;
                FoliageIndexed = foliageIndexed;
                FoliageChanged = foliageChanged;
                ResumeLx = resumeLx;
                ResumeLz = resumeLz;
                ResumeY = resumeY;
                Completed = completed;
            }
        }

        public static Result Run(
            ICoreAPI api,
            IBlockAccessor acc,
            Vec2i chunkCoord,
            in Request request,
            int resumeLx,
            int resumeLz,
            int resumeY,
            TreeTrunkDiscovery.TrunkFoundHandler onTreeFound,
            long budgetDeadline)
        {
            var flowerHits = new List<ChunkFlowerHit>();
            if (acc == null)
            {
                return new Result(flowerHits, 0, 0, 0, resumeLx, resumeLz, resumeY, completed: true);
            }

            int chunkSize = GlobalConstants.ChunkSize;
            int x0 = chunkCoord.X * chunkSize;
            int z0 = chunkCoord.Y * chunkSize;
            IMapChunk mapChunk = acc.GetMapChunk(chunkCoord.X, chunkCoord.Y);
            if (mapChunk == null)
            {
                return new Result(flowerHits, 0, 0, 0, 0, 0, 0, completed: true);
            }

            int columnTop = acc.MapSizeY - 1;
            int foliageIndexed = 0;
            int foliageChanged = 0;
            int treesRegistered = 0;
            int gameYear = api != null ? CanopyEcology.GameYear(api.World.Calendar) : 0;
            bool syncFoliage = request.SyncFoliage && api != null;

            for (int lx = resumeLx; lx < chunkSize; lx++)
            {
                int lzStart = lx == resumeLx ? resumeLz : 0;
                for (int lz = lzStart; lz < chunkSize; lz++)
                {
                    int x = x0 + lx;
                    int z = z0 + lz;
                    int topY = ChunkColumnWalker.GetColumnTopY(mapChunk, lx, lz, chunkSize, columnTop);
                    int surfaceY = GetSurfaceY(mapChunk, lx, lz, chunkSize, 0);
                    int yStart = (lx == resumeLx && lz == resumeLz && resumeY >= 0) ? resumeY : topY;

                    bool flowerFound = false;
                    bool trunkFound = false;

                    for (int y = yStart; y >= 0; y--)
                    {
                        if (budgetDeadline > 0 && Stopwatch.GetTimestamp() >= budgetDeadline)
                        {
                            return new Result(
                                flowerHits, treesRegistered, foliageIndexed, foliageChanged,
                                lx, lz, y, completed: false);
                        }

                        scanScratch.Set(x, y, z);
                        Block block = acc.GetBlock(scanScratch);
                        if (block.Id == 0) continue;

                        if (syncFoliage && CanopyFoliageRules.IsSeasonalFoliageBlock(block))
                        {
                            if (CanopySeasonSync.TrySyncCell(
                                    api, acc, scanScratch, block, request.FoliageIndex, gameYear, out _))
                            {
                                foliageChanged++;
                            }

                            if (request.FoliageIndex != null)
                            {
                                request.FoliageIndex.Add(scanScratch);
                            }

                            foliageIndexed++;
                        }

                        if (!trunkFound
                            && onTreeFound != null
                            && treesRegistered < request.MaxTreeHits
                            && PlantCodeHelper.IsFerntreeTrunkBlock(block))
                        {
                            BlockPos basePos = FerntreeStructure.GetTrunkBase(acc, scanScratch);
                            if (onTreeFound(basePos, WildFerntreeEcology.Species))
                            {
                                treesRegistered++;
                            }

                            trunkFound = true;
                        }

                        if (!trunkFound
                            && onTreeFound != null
                            && treesRegistered < request.MaxTreeHits
                            && PlantCodeHelper.IsTreeLogGrownBlock(block))
                        {
                            string wood = PlantCodeHelper.GetTreeWood(block);
                            if (!string.IsNullOrEmpty(wood) && WildTreeEcology.TryGet(wood, out _))
                            {
                                BlockPos basePos = PlantCodeHelper.GetTreeTrunkBase(acc, scanScratch);
                                if (onTreeFound(basePos, wood))
                                {
                                    treesRegistered++;
                                }

                                trunkFound = true;
                            }
                        }

                        if (!flowerFound
                            && flowerHits.Count < request.MaxFlowerHits
                            && y <= surfaceY + 2
                            && EcologyAttributes.ReproduceEnabled(block))
                        {
                            flowerHits.Add(new ChunkFlowerHit(scanScratch.Copy(), block.Code));
                            flowerFound = true;
                        }

                        if (ChunkColumnWalker.ContinueColumnScan(block))
                        {
                            continue;
                        }

                        break;
                    }
                }
            }

            return new Result(flowerHits, treesRegistered, foliageIndexed, foliageChanged, 0, 0, 0, completed: true);
        }

        static int GetSurfaceY(IMapChunk mapChunk, int lx, int lz, int chunkSize, int fallbackY)
        {
            if (mapChunk == null) return fallbackY;

            ushort[] heightmap = mapChunk.RainHeightMap;
            if (heightmap == null || heightmap.Length < chunkSize * chunkSize) return fallbackY;

            return heightmap[lz * chunkSize + lx];
        }
    }
}
