using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Places one year of wild tree maturation (log-grown / branchy / leaves-grown).</summary>
    internal static class TreeGrowthApplier
    {
        static readonly int[] NeighborDx = { 1, -1, 0, 0, 0, 0 };
        static readonly int[] NeighborDy = { 0, 0, 1, -1, 0, 0 };
        static readonly int[] NeighborDz = { 0, 0, 0, 0, 1, -1 };

        public static int TryGrowYear(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos trunkBase,
            string wood,
            int gameYear,
            float activityScale)
        {
            if (api == null || acc == null || trunkBase == null || string.IsNullOrEmpty(wood)) return 0;
            if (!LandClaimGuard.AllowsEcologyChange(api, trunkBase)) return 0;

            WildTreeGrowthProfiles.Profile profile = WildTreeGrowthProfiles.Resolve(wood);
            TreeStructureMetrics metrics = TreeStructureProbe.Measure(acc, trunkBase, wood);

            int fireRadius = System.Math.Min(10, System.Math.Max(CanopyBurnGuard.SourceRadius, metrics.CrownRadius + 2));
            BlockPos fireCheck = metrics.TrunkTop ?? trunkBase;
            if (CanopyBurnGuard.SuppressesFoliagePlacement(acc, fireCheck, fireRadius)) return 0;

            float pace = activityScale < 0.25f ? 0.25f : activityScale;
            float sizeIndex = TreeGrowthTargets.SizeIndexFraction(
                metrics.TrunkHeight,
                metrics.CrownRadius,
                profile) / pace;
            int ops = OpsForSizeIndex(sizeIndex, trunkBase, wood, gameYear);
            if (ops <= 0) return 0;

            // Seasonal canopy: do not push leaves/branchy during autumn strip or winter bare
            // window — otherwise yearly aging fights Dec–Feb force-strip and looks like waves.
            bool allowFoliageGrowth = AllowsSeasonalFoliageGrowth(api, trunkBase, wood);

            int placed = 0;
            bool canExtendTrunk = metrics.TrunkTop.Y + 1 < acc.MapSizeY - 1;
            bool needSpread = allowFoliageGrowth
                && metrics.CrownRadius < TreeStructureProbe.MaxCrownScanRadius;
            float trunkVsRef = TreeGrowthTargets.TrunkVsReference(metrics.TrunkHeight, profile);

            for (int i = 0; i < ops; i++)
            {
                bool preferHeight = canExtendTrunk
                    && (!needSpread || PreferHeightPass(i, trunkVsRef, trunkBase, wood, gameYear));
                if (preferHeight)
                {
                    if (TryExtendTrunk(api, acc, metrics.TrunkTop, wood))
                    {
                        placed++;
                        metrics = TreeStructureProbe.Measure(acc, trunkBase, wood);
                        canExtendTrunk = metrics.TrunkTop.Y + 1 < acc.MapSizeY - 1;
                        trunkVsRef = TreeGrowthTargets.TrunkVsReference(metrics.TrunkHeight, profile);
                        continue;
                    }
                }

                if (!allowFoliageGrowth) continue;

                if (needSpread && TrySpreadBranchy(api, acc, trunkBase, wood, metrics, gameYear))
                {
                    placed++;
                    metrics = TreeStructureProbe.Measure(acc, trunkBase, wood);
                    needSpread = metrics.CrownRadius < TreeStructureProbe.MaxCrownScanRadius;
                    continue;
                }

                if (TrySpreadRegularLeaf(api, acc, trunkBase, wood, metrics, gameYear))
                {
                    placed++;
                }
            }

            return placed;
        }

        static bool AllowsSeasonalFoliageGrowth(ICoreAPI api, BlockPos trunkBase, string wood)
        {
            if (!EcosystemConfig.Loaded.EnableSeasonalFoliage) return true;
            if (!CanopyBlockHelper.IsDeciduousTreeWood(wood)) return true;

            CanopySeasonPhase phase = CanopyEcology.ResolvePhase(api, trunkBase, wood, out _);
            if (phase == CanopySeasonPhase.Autumn) return false;
            if (CanopyFoliageRules.IsBareCrownSeason(api, trunkBase, wood)) return false;
            return true;
        }

        static int OpsForSizeIndex(
            float sizeIndex,
            BlockPos trunkBase,
            string wood,
            int gameYear)
        {
            if (sizeIndex >= 2.5f)
            {
                float gate = CanopyBlockHelper.DeterministicNoise(trunkBase, wood, gameYear + 900);
                return gate < 0.12f ? 1 : 0;
            }

            if (sizeIndex >= 1.25f)
            {
                float gate = CanopyBlockHelper.DeterministicNoise(trunkBase, wood, gameYear + 900);
                return gate < 0.3f ? 1 : 0;
            }

            if (sizeIndex < 0.35f) return 2;
            if (sizeIndex < 0.85f) return 2;
            return 1;
        }

        static bool PreferHeightPass(
            int opIndex,
            float trunkVsReference,
            BlockPos trunkBase,
            string wood,
            int gameYear)
        {
            if (trunkVsReference > 0.85f) return false;
            float gate = CanopyBlockHelper.DeterministicNoise(trunkBase, wood, gameYear + 700 + opIndex);
            return gate < 0.62f;
        }

        static bool TryExtendTrunk(ICoreAPI api, IBlockAccessor acc, BlockPos trunkTop, string wood)
        {
            if (trunkTop == null) return false;

            var above = trunkTop.UpCopy();
            if (!acc.IsValidPos(above)) return false;
            if (!LandClaimGuard.AllowsEcologyChange(api, above)) return false;
            Block aboveBlock = acc.GetBlock(above);
            if (!PlantVacancyRules.IsVacantPlantSpace(aboveBlock))
            {
                // Allow trunk to grow through its own crown by replacing its own foliage only.
                // Never replace solid blocks, other species' leaves, or any non-foliage blocks.
                bool ownLeaf =
                    (CanopyBlockHelper.IsBranchyLeaf(aboveBlock) || CanopyBlockHelper.IsRegularLeaf(aboveBlock))
                    && CanopyBlockHelper.GetWoodFromFoliageBlock(aboveBlock) == wood;
                if (!ownLeaf) return false;
            }

            Block log = ResolveLogBlock(api.World, wood);
            if (log == null || log.Id == 0) return false;

            acc.SetBlock(log.BlockId, above);
            acc.MarkBlockDirty(above);
            EcosystemSystem.Instance?.FoliageCells?.OnBlockAdded(above);

            // Add a small amount of foliage with upward growth, but never overwrite blocks.
            TryPlaceOneLeafNearNewTrunk(api, acc, above, wood);
            return true;
        }

        static void TryPlaceOneLeafNearNewTrunk(ICoreAPI api, IBlockAccessor acc, BlockPos newTrunkPos, string wood)
        {
            if (api == null || acc == null || newTrunkPos == null || string.IsNullOrEmpty(wood)) return;
            if (CanopyBurnGuard.SuppressesFoliagePlacement(acc, newTrunkPos)) return;

            Block anchor = acc.GetBlock(newTrunkPos);
            if (!PlantCodeHelper.IsTreeLogGrownBlock(anchor)) return;

            var scratch = new BlockPos(0);
            int start = (int)(CanopyBlockHelper.DeterministicNoise(newTrunkPos, wood, 31337) * 4f) % 4;
            if (start < 0) start += 4;

            for (int i = 0; i < 4; i++)
            {
                int dir = (start + i) % 4;
                scratch.Set(
                    newTrunkPos.X + (dir == 0 ? 1 : dir == 1 ? -1 : 0),
                    newTrunkPos.Y,
                    newTrunkPos.Z + (dir == 2 ? 1 : dir == 3 ? -1 : 0));
                scratch.dimension = newTrunkPos.dimension;

                if (!acc.IsValidPos(scratch)) continue;
                if (!LandClaimGuard.AllowsEcologyChange(api, scratch)) continue;
                if (!PlantVacancyRules.IsVacantPlantSpace(acc.GetBlock(scratch))) continue;

                Block leaf = CanopyBlockHelper.ResolveGrownLeafBlock(api.World, wood, scratch, newTrunkPos, anchor);
                if (leaf == null || leaf.Id == 0) continue;

                acc.SetBlock(leaf.BlockId, scratch);
                acc.MarkBlockDirty(scratch);
                EcosystemSystem.Instance?.FoliageCells?.OnBlockAdded(scratch);
                return;
            }
        }

        static bool TrySpreadBranchy(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos trunkBase,
            string wood,
            TreeStructureMetrics metrics,
            int gameYear)
        {
            var candidates = new List<BlockPos>(32);
            CollectCrownAnchors(acc, trunkBase, wood, metrics, candidates, branchyOnly: false);
            if (candidates.Count == 0) return false;

            ShuffleCandidates(candidates, trunkBase, wood, gameYear + 11);

            for (int i = 0; i < candidates.Count; i++)
            {
                BlockPos source = candidates[i];
                Block sourceBlock = acc.GetBlock(source);
                if (!CanopyBlockHelper.IsBudAnchorBlock(sourceBlock, wood)) continue;

                if (TryPlaceAdjacent(
                        api, acc, source, sourceBlock, wood,
                        budBranchy: true, gameYear, salt: 31))
                {
                    return true;
                }
            }

            return false;
        }

        static bool TrySpreadRegularLeaf(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos trunkBase,
            string wood,
            TreeStructureMetrics metrics,
            int gameYear)
        {
            var candidates = new List<BlockPos>(32);
            CollectCrownAnchors(acc, trunkBase, wood, metrics, candidates, branchyOnly: true);
            if (candidates.Count == 0) return false;

            ShuffleCandidates(candidates, trunkBase, wood, gameYear + 23);

            for (int i = 0; i < candidates.Count; i++)
            {
                BlockPos source = candidates[i];
                Block sourceBlock = acc.GetBlock(source);
                if (CanopyFoliageRules.Classify(sourceBlock) != FoliageCellKind.BranchyLeaf) continue;

                if (TryPlaceAdjacent(
                        api, acc, source, sourceBlock, wood,
                        budBranchy: false, gameYear, salt: 47))
                {
                    return true;
                }
            }

            return false;
        }

        static void CollectCrownAnchors(
            IBlockAccessor acc,
            BlockPos trunkBase,
            string wood,
            TreeStructureMetrics metrics,
            List<BlockPos> output,
            bool branchyOnly)
        {
            int crownStartY = trunkBase.Y + System.Math.Max(2, metrics.TrunkHeight / 3);
            int radius = System.Math.Min(14, metrics.CrownRadius + 3);
            var scratch = new BlockPos(0);

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    for (int y = crownStartY; y <= metrics.TrunkTop.Y + 5 && y < acc.MapSizeY; y++)
                    {
                        scratch.Set(trunkBase.X + dx, y, trunkBase.Z + dz);
                        if (!acc.IsValidPos(scratch)) continue;

                        Block block = acc.GetBlock(scratch);
                        if (branchyOnly)
                        {
                            if (CanopyFoliageRules.Classify(block) != FoliageCellKind.BranchyLeaf) continue;
                            if (CanopyBlockHelper.GetWoodFromFoliageBlock(block) != wood) continue;
                        }
                        else if (!CanopyBlockHelper.IsBudAnchorBlock(block, wood))
                        {
                            continue;
                        }

                        output.Add(scratch.Copy());
                    }
                }
            }
        }

        static void ShuffleCandidates(List<BlockPos> candidates, BlockPos trunkBase, string wood, int salt)
        {
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                float gate = CanopyBlockHelper.DeterministicNoise(candidates[i], wood, salt + i);
                int j = (int)(gate * (i + 1));
                if (j < 0) j = 0;
                if (j > i) j = i;

                BlockPos tmp = candidates[i];
                candidates[i] = candidates[j];
                candidates[j] = tmp;
            }
        }

        static bool TryPlaceAdjacent(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos sourcePos,
            Block sourceBlock,
            string wood,
            bool budBranchy,
            int gameYear,
            int salt)
        {
            if (CanopyBurnGuard.SuppressesFoliagePlacement(acc, sourcePos)) return false;

            var scratch = new BlockPos(0);
            int start = (int)(CanopyBlockHelper.DeterministicNoise(sourcePos, wood, gameYear + salt) * 6f) % 6;
            if (start < 0) start += 6;

            for (int n = 0; n < 6; n++)
            {
                int i = (start + n) % 6;
                scratch.Set(
                    sourcePos.X + NeighborDx[i],
                    sourcePos.Y + NeighborDy[i],
                    sourcePos.Z + NeighborDz[i]);
                if (!acc.IsValidPos(scratch)) continue;
                if (!LandClaimGuard.AllowsEcologyChange(api, scratch)) continue;
                if (CanopyBurnGuard.SuppressesBudTarget(acc, scratch)) continue;
                if (!PlantVacancyRules.IsVacantPlantSpace(acc.GetBlock(scratch))) continue;

                if (CanopyFoliageRules.BlocksPlayerClearedVacancy(api, scratch)) continue;

                float gate = CanopyBlockHelper.DeterministicNoise(scratch, wood, gameYear + salt + i);
                if (gate > 0.72f) continue;

                Block placed = budBranchy
                    ? CanopyBlockHelper.ResolveBranchyLeafBlock(api.World, wood, scratch, sourcePos, sourceBlock)
                    : CanopyBlockHelper.ResolveGrownLeafBlock(api.World, wood, scratch, sourcePos, sourceBlock);

                if (placed == null || placed.Id == 0) continue;

                acc.SetBlock(placed.BlockId, scratch);
                acc.MarkBlockDirty(scratch);
                EcosystemSystem.Instance?.FoliageCells?.OnBlockAdded(scratch);
                return true;
            }

            return false;
        }

        static Block ResolveLogBlock(IWorldAccessor world, string wood)
        {
            if (world == null || string.IsNullOrEmpty(wood)) return null;

            Block block = world.GetBlock(new AssetLocation("game", "log-grown-" + wood + "-ud"));
            if (block != null && block.Id != 0) return block;

            block = world.GetBlock(new AssetLocation("game", "log-grown-" + wood + "-north"));
            return block != null && block.Id != 0 ? block : null;
        }
    }
}
