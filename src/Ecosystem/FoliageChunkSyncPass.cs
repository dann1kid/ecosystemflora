using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Deterministic per-cell season sync during chunk column passes.</summary>
    internal static class CanopySeasonSync
    {
        public static bool TrySyncCell(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos pos,
            Block block,
            FoliageCellIndex index,
            int gameYear,
            out bool isFoliageCell)
        {
            isFoliageCell = false;
            if (api == null || acc == null || pos == null || block == null) return false;
            if (!EcosystemConfig.Loaded.EnableSeasonalFoliage) return false;

            FoliageCellKind kind = CanopyFoliageRules.Classify(block);
            if (kind == FoliageCellKind.None) return false;

            isFoliageCell = true;

            string wood = kind == FoliageCellKind.LogGrown
                ? PlantCodeHelper.GetTreeWood(block)
                : CanopyBlockHelper.GetWoodFromFoliageBlock(block);
            if (string.IsNullOrEmpty(wood)) return false;

            WildCanopySeason.Profile profile = WildCanopySeason.Resolve(wood);

            if (kind == FoliageCellKind.RegularLeaf)
            {
                if (!CanopyFoliageRules.ShouldCatchUpStripRegularLeaf(api, pos, wood, out float activity)) return false;
                if (!PassesDeterministicGate(pos, wood, gameYear, activity, stripScale: 0.55f)) return false;
                return CanopyFoliageRules.TryStripForced(api, acc, pos, index);
            }

            if (kind == FoliageCellKind.BranchyLeaf)
            {
                CanopySeasonPhase phase = CanopyEcology.ResolvePhase(api, pos, wood, out float activity);

                if (phase == CanopySeasonPhase.Autumn
                    && EcosystemConfig.Loaded.FoliagePeakAutumnBranchyStripActivity > 0f
                    && activity >= EcosystemConfig.Loaded.FoliagePeakAutumnBranchyStripActivity
                    && PassesDeterministicGate(pos, wood, gameYear + 3, activity, stripScale: 0.22f))
                {
                    return CanopyFoliageRules.TryStripForced(api, acc, pos, index);
                }

                if (!CanopyFoliageRules.ShouldCatchUpBud(api, pos, wood, kind, out activity)) return false;
                if (!CanopyFoliageRules.NeedsSpringCatchUp(acc, pos, wood, kind)) return false;

                activity *= profile.LeafCatchUpScale;
                return CanopyFoliageRules.TryPlaceSeasonBudDeterministic(
                    api, acc, pos, block, wood, budBranchy: false, activity, gameYear, index);
            }

            if (kind == FoliageCellKind.LogGrown)
            {
                if (EcosystemConfig.Loaded.FoliageRestoreBareSkeleton
                    && CanopyFoliageRules.IsBareCrownSeason(api, pos, wood)
                    && !CanopyFoliageRules.HasAdjacentBranchyLeaf(acc, pos, wood))
                {
                    CanopyEcology.ResolvePhase(api, pos, wood, out float bareActivity);
                    if (bareActivity <= 0f)
                    {
                        CanopyFoliageRules.ShouldCatchUpStripRegularLeaf(api, pos, wood, out bareActivity);
                    }

                    if (bareActivity > 0f
                        && PassesDeterministicGate(pos, wood, gameYear + 11, bareActivity, stripScale: 0.14f))
                    {
                        return CanopyFoliageRules.TryPlaceSeasonBudDeterministic(
                            api, acc, pos, block, wood, budBranchy: true, bareActivity, gameYear, index);
                    }
                }

                if (!CanopyFoliageRules.ShouldCatchUpBud(api, pos, wood, kind, out float activity)) return false;
                if (!CanopyFoliageRules.NeedsSpringCatchUp(acc, pos, wood, kind)) return false;

                activity *= profile.BranchyCatchUpScale;
                if (!PassesDeterministicGate(pos, wood, gameYear + 7, activity, stripScale: 0.52f)) return false;

                return CanopyFoliageRules.TryPlaceSeasonBudDeterministic(
                    api, acc, pos, block, wood, budBranchy: true, activity, gameYear, index);
            }

            return false;
        }

        static bool PassesDeterministicGate(BlockPos pos, string wood, int salt, float activity, float stripScale)
        {
            if (activity <= 0f) return false;

            float noise = 0.55f + CanopyBlockHelper.DeterministicNoise(pos, wood, salt) * 0.45f;
            float threshold = activity * noise * stripScale;
            if (threshold > 1f) threshold = 1f;
            if (threshold < 0f) threshold = 0f;

            float gate = CanopyBlockHelper.DeterministicNoise(pos, wood, salt + 1000);
            return gate < threshold;
        }
    }

    internal static class FoliageChunkSyncPass
    {
        static readonly BlockPos scanScratch = new BlockPos(0);

        public readonly struct Result
        {
            public readonly int Indexed;
            public readonly int Changed;
            public readonly int ResumeLx;
            public readonly int ResumeLz;
            public readonly int ResumeY;
            public readonly bool Completed;

            public Result(int indexed, int changed, int resumeLx, int resumeLz, int resumeY, bool completed)
            {
                Indexed = indexed;
                Changed = changed;
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
            FoliageCellIndex index,
            int resumeLx,
            int resumeLz,
            int resumeY,
            long budgetDeadlineTicks)
        {
            if (acc == null || api == null)
            {
                return new Result(0, 0, resumeLx, resumeLz, resumeY, completed: true);
            }

            int chunkSize = GlobalConstants.ChunkSize;
            int x0 = chunkCoord.X * chunkSize;
            int z0 = chunkCoord.Y * chunkSize;
            IMapChunk mapChunk = acc.GetMapChunk(chunkCoord.X, chunkCoord.Y);
            if (mapChunk == null)
            {
                return new Result(0, 0, 0, 0, 0, completed: true);
            }

            int columnTop = acc.MapSizeY - 1;
            int indexed = 0;
            int changed = 0;
            int gameYear = CanopyEcology.GameYear(api.World.Calendar);
            bool maintainIndex = FoliageSyncModeHelper.UsesRandomTick(EcosystemConfig.Loaded);

            for (int lx = resumeLx; lx < chunkSize; lx++)
            {
                int lzStart = lx == resumeLx ? resumeLz : 0;
                for (int lz = lzStart; lz < chunkSize; lz++)
                {
                    int x = x0 + lx;
                    int z = z0 + lz;
                    int topY = ChunkColumnWalker.GetColumnTopY(mapChunk, lx, lz, chunkSize, columnTop);
                    int yStart = (lx == resumeLx && lz == resumeLz) ? resumeY : topY;

                    for (int y = yStart; y >= 0; y--)
                    {
                        if (budgetDeadlineTicks > 0 && Stopwatch.GetTimestamp() >= budgetDeadlineTicks)
                        {
                            return new Result(indexed, changed, lx, lz, y, completed: false);
                        }

                        scanScratch.Set(x, y, z);
                        Block block = acc.GetBlock(scanScratch);
                        if (block.Id == 0) continue;

                        if (CanopyFoliageRules.IsSeasonalFoliageBlock(block))
                        {
                            if (CanopySeasonSync.TrySyncCell(
                                    api, acc, scanScratch, block, index, gameYear, out _))
                            {
                                changed++;
                            }

                            if (maintainIndex)
                            {
                                index?.Add(scanScratch);
                            }

                            indexed++;
                        }

                        if (ChunkColumnWalker.ContinueColumnScan(block))
                        {
                            continue;
                        }

                        break;
                    }
                }
            }

            return new Result(indexed, changed, 0, 0, 0, completed: true);
        }
    }
}
