using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

// Export-only C# tables: fallback when SpeciesEcologyRegistry is not loaded.
#pragma warning disable CS0618

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
            public int MaxVineHits { get; init; }
            public bool SyncFoliage { get; init; }
            public FoliageCellIndex FoliageIndex { get; init; }
        }

        public readonly struct Result
        {
            public readonly List<ChunkFlowerHit> FlowerHits;
            public readonly List<ChunkFlowerHit> VineHits;
            public readonly List<ChunkFlowerHit> TreeHits;
            public readonly List<ChunkFlowerHit> EstablishingTallgrassHits;
            public readonly int FoliageIndexed;
            public readonly int FoliageChanged;
            public readonly int ResumeLx;
            public readonly int ResumeLz;
            public readonly int ResumeY;
            public readonly bool Completed;

            public Result(
                List<ChunkFlowerHit> flowerHits,
                List<ChunkFlowerHit> vineHits,
                List<ChunkFlowerHit> treeHits,
                List<ChunkFlowerHit> establishingTallgrassHits,
                int foliageIndexed,
                int foliageChanged,
                int resumeLx,
                int resumeLz,
                int resumeY,
                bool completed)
            {
                FlowerHits = flowerHits;
                VineHits = vineHits;
                TreeHits = treeHits;
                EstablishingTallgrassHits = establishingTallgrassHits;
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
            long budgetDeadline) =>
            Run(
                api,
                new LiveRegistrationColumnView(acc),
                chunkCoord,
                in request,
                resumeLx,
                resumeLz,
                resumeY,
                onTreeFound,
                budgetDeadline);

        public static Result Run(
            ICoreAPI api,
            IRegistrationColumnView view,
            Vec2i chunkCoord,
            in Request request,
            int resumeLx,
            int resumeLz,
            int resumeY,
            TreeTrunkDiscovery.TrunkFoundHandler onTreeFound,
            long budgetDeadline)
        {
            var flowerHits = new List<ChunkFlowerHit>();
            var vineHits = new List<ChunkFlowerHit>();
            var treeHits = new List<ChunkFlowerHit>();
            var establishingTallgrassHits = new List<ChunkFlowerHit>();
            if (view == null)
            {
                return new Result(flowerHits, vineHits, treeHits, establishingTallgrassHits, 0, 0, resumeLx, resumeLz, resumeY, completed: true);
            }

            int chunkSize = GlobalConstants.ChunkSize;
            int x0 = chunkCoord.X * chunkSize;
            int z0 = chunkCoord.Y * chunkSize;
            IMapChunk mapChunk = view.GetMapChunk(chunkCoord);
            ushort[] heightmap = null;
            if (view is SnapshotRegistrationColumnView snapView)
            {
                heightmap = snapView.RainHeightMap;
            }
            else if (mapChunk == null)
            {
                return new Result(flowerHits, vineHits, treeHits, establishingTallgrassHits, 0, 0, 0, 0, 0, completed: true);
            }
            else
            {
                heightmap = mapChunk.RainHeightMap;
            }

            int columnTop = view.MapSizeY - 1;
            int foliageIndexed = 0;
            int foliageChanged = 0;
            int treesRegistered = 0;
            int gameYear = api != null ? CanopyEcology.GameYear(api.World.Calendar) : 0;
            bool syncFoliage = request.SyncFoliage && api != null && view.SupportsFoliageMutation;
            IBlockAccessor acc = view.SupportsFoliageMutation && api?.World?.BlockAccessor != null
                ? api.World.BlockAccessor
                : null;

            for (int lx = resumeLx; lx < chunkSize; lx++)
            {
                int lzStart = lx == resumeLx ? resumeLz : 0;
                for (int lz = lzStart; lz < chunkSize; lz++)
                {
                    int x = x0 + lx;
                    int z = z0 + lz;
                    int topY = ChunkColumnWalker.GetColumnTopY(heightmap, lx, lz, chunkSize, columnTop);
                    int yStart = (lx == resumeLx && lz == resumeLz && resumeY >= 0) ? resumeY : topY;

                    bool trunkFound = false;

                    for (int y = yStart; y >= 0; y--)
                    {
                        if (budgetDeadline > 0 && Stopwatch.GetTimestamp() >= budgetDeadline)
                        {
                            return new Result(
                                flowerHits, vineHits, treeHits, establishingTallgrassHits, foliageIndexed, foliageChanged,
                                lx, lz, y, completed: false);
                        }

                        scanScratch.Set(x, y, z);
                        Block block = view.GetBlock(x, y, z);
                        if (block == null || block.Id == 0) continue;

                        if (syncFoliage && acc != null && CanopyFoliageRules.IsSeasonalFoliageBlock(block))
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
                            && treesRegistered < request.MaxTreeHits
                            && PlantCodeHelper.IsFerntreeTrunkBlock(block))
                        {
                            BlockPos basePos = RegistrationColumnScan.GetFerntreeTrunkBase(view, scanScratch);
                            Block trunk = view.GetBlock(basePos.X, basePos.Y, basePos.Z);
                            if (trunk?.Code != null
                                && (onTreeFound == null || onTreeFound(basePos, WildFerntreeEcology.Species)))
                            {
                                if (onTreeFound == null)
                                {
                                    treeHits.Add(new ChunkFlowerHit(basePos.Copy(), trunk.Code));
                                }

                                treesRegistered++;
                            }

                            trunkFound = true;
                        }

                        if (!trunkFound
                            && treesRegistered < request.MaxTreeHits
                            && PlantCodeHelper.IsTreeLogGrownBlock(block))
                        {
                            string wood = PlantCodeHelper.GetTreeWood(block);
                            if (!string.IsNullOrEmpty(wood) && WildTreeEcology.TryGet(wood, out _))
                            {
                                BlockPos basePos = RegistrationColumnScan.GetTreeTrunkBase(view, scanScratch);
                                Block trunk = view.GetBlock(basePos.X, basePos.Y, basePos.Z);
                                if (trunk?.Code != null
                                    && (onTreeFound == null || onTreeFound(basePos, wood)))
                                {
                                    if (onTreeFound == null)
                                    {
                                        treeHits.Add(new ChunkFlowerHit(basePos.Copy(), trunk.Code));
                                    }

                                    treesRegistered++;
                                }

                                trunkFound = true;
                            }
                        }

                        if (vineHits.Count < request.MaxVineHits
                            && WildVineHelper.IsEndBlock(block))
                        {
                            vineHits.Add(new ChunkFlowerHit(scanScratch.Copy(), block.Code));
                        }

                        if (ChunkColumnWalker.ContinueColumnScan(block))
                        {
                            continue;
                        }

                        break;
                    }

                    if (request.MaxFlowerHits > 0
                        && flowerHits.Count + establishingTallgrassHits.Count < request.MaxFlowerHits
                        && RegistrationColumnFlowerScan.TryFindTopReproducer(
                            view,
                            api,
                            x,
                            z,
                            ChunkColumnWalker.GetFloraRegistrationScanTopY(heightmap, lx, lz, chunkSize, columnTop),
                            out Block flowerBlock,
                            out BlockPos flowerPos,
                            out bool needsEstablishment))
                    {
                        if (needsEstablishment)
                        {
                            establishingTallgrassHits.Add(new ChunkFlowerHit(flowerPos, flowerBlock.Code));
                        }
                        else
                        {
                            flowerHits.Add(new ChunkFlowerHit(flowerPos, flowerBlock.Code));
                        }
                    }
                }
            }

            return new Result(flowerHits, vineHits, treeHits, establishingTallgrassHits, foliageIndexed, foliageChanged, 0, 0, 0, completed: true);
        }

        internal static int ResolveFlowerScanTopY(ushort[] heightmap, int lx, int lz, int chunkSize, int columnTop) =>
            ChunkColumnWalker.GetFloraRegistrationScanTopY(heightmap, lx, lz, chunkSize, columnTop);
    }
}
