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
            float activityScale,
            int treeAgeYears = 0)
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
            float trunkVsRef = TreeGrowthTargets.TrunkVsReference(metrics.TrunkHeight, profile);
            float crownVsRef = TreeGrowthTargets.CrownVsReference(metrics.CrownRadius, profile);
            bool crownLags = TreeGrowthTargets.CrownLagsTrunk(trunkVsRef, crownVsRef);

            // Seasonal canopy: do not push leaves/branchy during autumn strip or winter bare
            // window — otherwise yearly aging fights Dec–Feb force-strip and looks like waves.
            bool allowFoliageGrowth = AllowsSeasonalFoliageGrowth(api, trunkBase, wood);

            int placed = 0;
            // Worldgen / mature oaks often stop extending while the tip stays a bare stick above
            // a leafy mid canopy — repair that shelf even when yearly ops are zero.
            if (allowFoliageGrowth
                && metrics.TrunkTop != null
                && IsTrunkTipUndressed(acc, metrics.TrunkTop, wood))
            {
                TryDressNewTrunkTip(api, acc, metrics.TrunkTop, wood, profile, metrics, trunkBase);
                if (!IsTrunkTipUndressed(acc, metrics.TrunkTop, wood)) placed++;
            }

            int ops = OpsForSizeIndex(sizeIndex, trunkBase, wood, gameYear, crownLags, treeAgeYears);
            if (ops <= 0) return placed;

            bool canExtendTrunk = metrics.TrunkTop.Y + 1 < acc.MapSizeY - 1;
            bool belowCrownSoftTarget = metrics.CrownRadius < profile.ReferenceCrownRadius;
            bool needSpread = allowFoliageGrowth
                && (belowCrownSoftTarget || metrics.CrownRadius < TreeStructureProbe.MaxCrownScanRadius);

            for (int i = 0; i < ops; i++)
            {
                bool preferHeight = canExtendTrunk
                    && !crownLags
                    && (!needSpread || PreferHeightPass(i, trunkVsRef, trunkBase, wood, gameYear, profile.CrownForm));
                if (preferHeight)
                {
                    if (TryExtendTrunk(api, acc, metrics.TrunkTop, wood, profile, metrics, trunkBase))
                    {
                        placed++;
                        metrics = TreeStructureProbe.Measure(acc, trunkBase, wood);
                        canExtendTrunk = metrics.TrunkTop.Y + 1 < acc.MapSizeY - 1;
                        trunkVsRef = TreeGrowthTargets.TrunkVsReference(metrics.TrunkHeight, profile);
                        crownVsRef = TreeGrowthTargets.CrownVsReference(metrics.CrownRadius, profile);
                        crownLags = TreeGrowthTargets.CrownLagsTrunk(trunkVsRef, crownVsRef);
                        belowCrownSoftTarget = metrics.CrownRadius < profile.ReferenceCrownRadius;
                        needSpread = allowFoliageGrowth
                            && (belowCrownSoftTarget || metrics.CrownRadius < TreeStructureProbe.MaxCrownScanRadius);
                        continue;
                    }
                }

                if (!allowFoliageGrowth) continue;

                if (needSpread && TrySpreadBranchy(api, acc, trunkBase, wood, metrics, profile, gameYear, crownLags))
                {
                    placed++;
                    metrics = TreeStructureProbe.Measure(acc, trunkBase, wood);
                    trunkVsRef = TreeGrowthTargets.TrunkVsReference(metrics.TrunkHeight, profile);
                    crownVsRef = TreeGrowthTargets.CrownVsReference(metrics.CrownRadius, profile);
                    crownLags = TreeGrowthTargets.CrownLagsTrunk(trunkVsRef, crownVsRef);
                    belowCrownSoftTarget = metrics.CrownRadius < profile.ReferenceCrownRadius;
                    needSpread = allowFoliageGrowth
                        && (belowCrownSoftTarget || metrics.CrownRadius < TreeStructureProbe.MaxCrownScanRadius);
                    continue;
                }

                if (TrySpreadRegularLeaf(api, acc, trunkBase, wood, metrics, profile, gameYear, crownLags))
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
            int gameYear,
            bool crownLags,
            int treeAgeYears)
        {
            int ops;
            if (sizeIndex >= 2.5f)
            {
                float gate = CanopyBlockHelper.DeterministicNoise(trunkBase, wood, gameYear + 900);
                ops = gate < 0.12f ? 1 : 0;
            }
            else if (sizeIndex >= 1.25f)
            {
                float gate = CanopyBlockHelper.DeterministicNoise(trunkBase, wood, gameYear + 900);
                ops = gate < 0.3f ? 1 : 0;
            }
            else if (sizeIndex < 0.35f) ops = 2;
            else if (sizeIndex < 0.85f) ops = 2;
            else ops = 1;

            // Tall-but-skinny trees (typical after vanilla sapling treegen): spend the year on crown.
            if (crownLags)
            {
                if (ops < 2) ops = 2;
                if (treeAgeYears >= 15) ops++;
                if (treeAgeYears >= 40) ops++;
            }

            return ops;
        }

        static bool PreferHeightPass(
            int opIndex,
            float trunkVsReference,
            BlockPos trunkBase,
            string wood,
            int gameYear,
            TreeCrownForm form)
        {
            if (trunkVsReference > 0.85f) return false;
            float gate = CanopyBlockHelper.DeterministicNoise(trunkBase, wood, gameYear + 700 + opIndex);
            float threshold = form == TreeCrownForm.Column ? 0.72f : 0.62f;
            return gate < threshold;
        }

        static bool TryExtendTrunk(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos trunkTop,
            string wood,
            WildTreeGrowthProfiles.Profile profile,
            TreeStructureMetrics metrics,
            BlockPos trunkBase)
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

            TreeStructureMetrics after = TreeStructureProbe.Measure(acc, trunkBase, wood);
            TryDressNewTrunkTip(api, acc, above, wood, profile, after, trunkBase);
            return true;
        }

        /// <summary>True when the tip has no branchy at the same level or above (foliage below does not count).</summary>
        internal static bool IsTrunkTipUndressed(IBlockAccessor acc, BlockPos trunkTop, string wood)
        {
            if (acc == null || trunkTop == null || string.IsNullOrEmpty(wood)) return false;
            return !CanopyFoliageRules.HasAdjacentBranchyLeaf(acc, trunkTop, wood, ignoreBelow: true);
        }

        static void TryDressNewTrunkTip(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos newTrunkPos,
            string wood,
            WildTreeGrowthProfiles.Profile profile,
            TreeStructureMetrics metrics,
            BlockPos trunkBase)
        {
            if (api == null || acc == null || newTrunkPos == null || string.IsNullOrEmpty(wood)) return;
            if (CanopyBurnGuard.SuppressesFoliagePlacement(acc, newTrunkPos)) return;

            Block anchor = acc.GetBlock(newTrunkPos);
            if (!PlantCodeHelper.IsTreeLogGrownBlock(anchor)) return;

            var scratch = new BlockPos(0);
            int start = (int)(CanopyBlockHelper.DeterministicNoise(newTrunkPos, wood, 31337) * 4f) % 4;
            if (start < 0) start += 4;
            int placed = 0;
            int maxDress = profile.CrownForm == TreeCrownForm.Column ? 2 : 3;

            for (int i = 0; i < 4 && placed < maxDress; i++)
            {
                int dir = (start + i) % 4;
                scratch.Set(
                    newTrunkPos.X + (dir == 0 ? 1 : dir == 1 ? -1 : 0),
                    newTrunkPos.Y,
                    newTrunkPos.Z + (dir == 2 ? 1 : dir == 3 ? -1 : 0));
                scratch.dimension = newTrunkPos.dimension;

                if (!TreeCrownEnvelope.AllowsCell(profile, metrics, trunkBase, scratch)) continue;
                if (!TryPlaceTipFoliage(api, acc, scratch, newTrunkPos, anchor, wood, preferBranchy: true))
                {
                    continue;
                }

                placed++;
            }

            if (placed < maxDress && TreeCrownEnvelope.DressAboveTip(profile.CrownForm))
            {
                scratch.Set(newTrunkPos.X, newTrunkPos.Y + 1, newTrunkPos.Z);
                scratch.dimension = newTrunkPos.dimension;
                if (TreeCrownEnvelope.AllowsCell(profile, metrics, trunkBase, scratch))
                {
                    TryPlaceTipFoliage(api, acc, scratch, newTrunkPos, anchor, wood, preferBranchy: true);
                }
            }
        }

        static bool TryPlaceTipFoliage(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos target,
            BlockPos anchorPos,
            Block anchor,
            string wood,
            bool preferBranchy)
        {
            if (!acc.IsValidPos(target)) return false;
            if (!LandClaimGuard.AllowsEcologyChange(api, target)) return false;
            if (!PlantVacancyRules.IsVacantPlantSpace(acc.GetBlock(target))) return false;

            Block leaf = preferBranchy
                ? CanopyBlockHelper.ResolveBranchyLeafBlock(api.World, wood, target, anchorPos, anchor)
                : CanopyBlockHelper.ResolveGrownLeafBlock(api.World, wood, target, anchorPos, anchor);
            if (leaf == null || leaf.Id == 0)
            {
                leaf = CanopyBlockHelper.ResolveGrownLeafBlock(api.World, wood, target, anchorPos, anchor);
            }

            if (leaf == null || leaf.Id == 0) return false;

            acc.SetBlock(leaf.BlockId, target);
            acc.MarkBlockDirty(target);
            EcosystemSystem.Instance?.FoliageCells?.OnBlockAdded(target);
            return true;
        }

        static bool TrySpreadBranchy(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos trunkBase,
            string wood,
            TreeStructureMetrics metrics,
            WildTreeGrowthProfiles.Profile profile,
            int gameYear,
            bool crownLags)
        {
            var candidates = new List<BlockPos>(32);
            CollectCrownAnchors(acc, trunkBase, wood, metrics, profile, candidates, branchyOnly: false);
            if (candidates.Count == 0) return false;

            OrderCandidatesByForm(candidates, metrics, profile, trunkBase);
            ShuffleWithinPriorityBands(candidates, wood, gameYear + 11, metrics, profile, trunkBase);

            for (int i = 0; i < candidates.Count; i++)
            {
                BlockPos source = candidates[i];
                Block sourceBlock = acc.GetBlock(source);
                if (!CanopyBlockHelper.IsBudAnchorBlock(sourceBlock, wood)) continue;

                if (TryPlaceAdjacent(
                        api, acc, source, sourceBlock, wood, profile, metrics, trunkBase,
                        budBranchy: true, gameYear, salt: 31, crownLags: crownLags))
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
            WildTreeGrowthProfiles.Profile profile,
            int gameYear,
            bool crownLags)
        {
            var candidates = new List<BlockPos>(32);
            CollectCrownAnchors(acc, trunkBase, wood, metrics, profile, candidates, branchyOnly: true);
            if (candidates.Count == 0) return false;

            OrderCandidatesByForm(candidates, metrics, profile, trunkBase);
            ShuffleWithinPriorityBands(candidates, wood, gameYear + 23, metrics, profile, trunkBase);

            for (int i = 0; i < candidates.Count; i++)
            {
                BlockPos source = candidates[i];
                Block sourceBlock = acc.GetBlock(source);
                if (CanopyFoliageRules.Classify(sourceBlock) != FoliageCellKind.BranchyLeaf) continue;

                if (TryPlaceAdjacent(
                        api, acc, source, sourceBlock, wood, profile, metrics, trunkBase,
                        budBranchy: false, gameYear, salt: 47, crownLags: crownLags))
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
            WildTreeGrowthProfiles.Profile profile,
            List<BlockPos> output,
            bool branchyOnly)
        {
            int crownStartY = TreeCrownEnvelope.CrownStartY(trunkBase, metrics, profile.CrownForm);
            int crownTopY = TreeCrownEnvelope.CrownTopY(metrics, profile.CrownForm);
            int radius = System.Math.Min(
                TreeStructureProbe.MaxCrownScanRadius,
                System.Math.Max(profile.ReferenceCrownRadius + 1, metrics.CrownRadius + 3));
            var scratch = new BlockPos(0);

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    for (int y = crownStartY; y <= crownTopY && y < acc.MapSizeY; y++)
                    {
                        scratch.Set(trunkBase.X + dx, y, trunkBase.Z + dz);
                        if (!acc.IsValidPos(scratch)) continue;
                        if (!TreeCrownEnvelope.AllowsCell(profile, metrics, trunkBase, scratch)) continue;

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

        /// <summary>Form-aware anchor order (spreading/umbrella: top first; oval: mid-band first).</summary>
        internal static void OrderCandidatesByForm(
            List<BlockPos> candidates,
            TreeStructureMetrics metrics,
            WildTreeGrowthProfiles.Profile profile,
            BlockPos trunkBase)
        {
            if (candidates == null || candidates.Count < 2) return;

            candidates.Sort((a, b) =>
            {
                int pa = TreeCrownEnvelope.AnchorPriority(profile.CrownForm, metrics, trunkBase, a);
                int pb = TreeCrownEnvelope.AnchorPriority(profile.CrownForm, metrics, trunkBase, b);
                int cmp = pb.CompareTo(pa);
                if (cmp != 0) return cmp;
                return b.Y.CompareTo(a.Y);
            });
        }

        /// <summary>Legacy helper used by tests — spreading-style top-first order.</summary>
        internal static void OrderCandidatesUpperCrownFirst(List<BlockPos> candidates, TreeStructureMetrics metrics)
        {
            var profile = new WildTreeGrowthProfiles.Profile(14, 7, 120, crownForm: TreeCrownForm.Spreading);
            var trunkBase = new BlockPos(metrics.TrunkTop.X, metrics.TrunkTop.Y - metrics.TrunkHeight + 1, metrics.TrunkTop.Z);
            OrderCandidatesByForm(candidates, metrics, profile, trunkBase);
        }

        static void ShuffleWithinPriorityBands(
            List<BlockPos> candidates,
            string wood,
            int salt,
            TreeStructureMetrics metrics,
            WildTreeGrowthProfiles.Profile profile,
            BlockPos trunkBase)
        {
            if (candidates == null || candidates.Count < 2) return;

            int i = 0;
            while (i < candidates.Count)
            {
                int band = TreeCrownEnvelope.AnchorPriority(profile.CrownForm, metrics, trunkBase, candidates[i]) / 20;
                int j = i + 1;
                while (j < candidates.Count)
                {
                    int other = TreeCrownEnvelope.AnchorPriority(profile.CrownForm, metrics, trunkBase, candidates[j]) / 20;
                    if (other != band) break;
                    j++;
                }

                ShuffleRange(candidates, i, j - 1, wood, salt + band * 17);
                i = j;
            }
        }

        static void ShuffleRange(List<BlockPos> candidates, int from, int to, string wood, int salt)
        {
            for (int i = to; i > from; i--)
            {
                float gate = CanopyBlockHelper.DeterministicNoise(candidates[i], wood, salt + i);
                int span = i - from + 1;
                int j = from + (int)(gate * span);
                if (j < from) j = from;
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
            WildTreeGrowthProfiles.Profile profile,
            TreeStructureMetrics metrics,
            BlockPos trunkBase,
            bool budBranchy,
            int gameYear,
            int salt,
            bool crownLags)
        {
            if (CanopyBurnGuard.SuppressesFoliagePlacement(acc, sourcePos)) return false;

            var scratch = new BlockPos(0);
            bool horizontalFirst = TreeCrownEnvelope.PreferHorizontalFill(profile.CrownForm, crownLags);
            int[] dirOrder = horizontalFirst ? HorizontalFirstDirs : SequentialDirs;
            int start = (int)(CanopyBlockHelper.DeterministicNoise(sourcePos, wood, gameYear + salt) * dirOrder.Length) % dirOrder.Length;
            if (start < 0) start += dirOrder.Length;
            float placeGate = crownLags || horizontalFirst ? 0.9f : 0.72f;

            for (int n = 0; n < dirOrder.Length; n++)
            {
                int i = dirOrder[(start + n) % dirOrder.Length];
                scratch.Set(
                    sourcePos.X + NeighborDx[i],
                    sourcePos.Y + NeighborDy[i],
                    sourcePos.Z + NeighborDz[i]);
                if (!acc.IsValidPos(scratch)) continue;
                if (!TreeCrownEnvelope.AllowsCell(profile, metrics, trunkBase, scratch)) continue;
                if (!LandClaimGuard.AllowsEcologyChange(api, scratch)) continue;
                if (CanopyBurnGuard.SuppressesBudTarget(acc, scratch)) continue;
                if (!PlantVacancyRules.IsVacantPlantSpace(acc.GetBlock(scratch))) continue;

                if (CanopyFoliageRules.BlocksPlayerClearedVacancy(api, scratch)) continue;

                float gate = CanopyBlockHelper.DeterministicNoise(scratch, wood, gameYear + salt + i);
                if (gate > placeGate) continue;

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

        // Neighbor indices: 0=+X 1=-X 2=+Y 3=-Y 4=+Z 5=-Z
        static readonly int[] SequentialDirs = { 0, 1, 2, 3, 4, 5 };
        static readonly int[] HorizontalFirstDirs = { 0, 1, 4, 5, 2, 3 };

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
