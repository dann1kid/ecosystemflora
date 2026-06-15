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
            int ageYears,
            int gameYear,
            float activityScale)
        {
            if (api == null || acc == null || trunkBase == null || string.IsNullOrEmpty(wood)) return 0;
            if (!LandClaimGuard.AllowsEcologyChange(api, trunkBase)) return 0;

            WildTreeGrowthProfiles.Profile profile = WildTreeGrowthProfiles.Resolve(wood);
            TreeStructureMetrics metrics = TreeStructureProbe.Measure(acc, trunkBase, wood);

            int targetHeight = TreeGrowthTargets.TargetTrunkHeight(ageYears, profile, activityScale);
            int targetRadius = TreeGrowthTargets.TargetCrownRadius(ageYears, profile, activityScale);

            int ops = OpsForAge(ageYears, profile, trunkBase, wood, gameYear);
            if (ops <= 0) return 0;

            int placed = 0;
            bool needHeight = metrics.TrunkHeight < targetHeight;
            bool needSpread = metrics.CrownRadius < targetRadius;

            for (int i = 0; i < ops; i++)
            {
                bool preferHeight = needHeight && (!needSpread || PreferHeightPass(i, ageYears, profile, trunkBase, wood, gameYear));
                if (preferHeight)
                {
                    if (TryExtendTrunk(api, acc, metrics.TrunkTop, wood))
                    {
                        placed++;
                        metrics = TreeStructureProbe.Measure(acc, trunkBase, wood);
                        needHeight = metrics.TrunkHeight < targetHeight;
                        continue;
                    }
                }

                if (needSpread && TrySpreadBranchy(api, acc, trunkBase, wood, metrics, gameYear))
                {
                    placed++;
                    metrics = TreeStructureProbe.Measure(acc, trunkBase, wood);
                    needSpread = metrics.CrownRadius < targetRadius;
                    continue;
                }

                if (TrySpreadRegularLeaf(api, acc, trunkBase, wood, metrics, gameYear))
                {
                    placed++;
                }
            }

            return placed;
        }

        static int OpsForAge(
            int ageYears,
            WildTreeGrowthProfiles.Profile profile,
            BlockPos trunkBase,
            string wood,
            int gameYear)
        {
            float mature = TreeGrowthTargets.GrowthFraction(ageYears, profile.MaxAgeYears);
            if (mature >= 0.98f)
            {
                float gate = CanopyBlockHelper.DeterministicNoise(trunkBase, wood, gameYear + 900);
                return gate < 0.25f ? 1 : 0;
            }

            if (ageYears < profile.MaxAgeYears * 0.35f) return 2;
            if (mature < 0.75f) return 2;
            return 1;
        }

        static bool PreferHeightPass(
            int opIndex,
            int ageYears,
            WildTreeGrowthProfiles.Profile profile,
            BlockPos trunkBase,
            string wood,
            int gameYear)
        {
            if (ageYears > profile.MaxAgeYears * 0.45f) return false;
            float gate = CanopyBlockHelper.DeterministicNoise(trunkBase, wood, gameYear + 700 + opIndex);
            return gate < 0.62f;
        }

        static bool TryExtendTrunk(ICoreAPI api, IBlockAccessor acc, BlockPos trunkTop, string wood)
        {
            if (trunkTop == null) return false;

            var above = trunkTop.UpCopy();
            if (!acc.IsValidPos(above)) return false;
            if (!LandClaimGuard.AllowsEcologyChange(api, above)) return false;
            if (!PlantVacancyRules.IsVacantPlantSpace(acc.GetBlock(above))) return false;

            Block log = ResolveLogBlock(api.World, wood);
            if (log == null || log.Id == 0) return false;

            acc.SetBlock(log.BlockId, above);
            acc.MarkBlockDirty(above);
            EcosystemSystem.Instance?.FoliageCells?.OnBlockAdded(above);
            return true;
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
                if (!PlantVacancyRules.IsVacantPlantSpace(acc.GetBlock(scratch))) continue;

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
