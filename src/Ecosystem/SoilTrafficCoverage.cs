using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Foot-traffic "compaction": step grass coverage down/up on the same fertility soil tier.
    /// Wear stops at <c>verysparse</c> (not bare <c>none</c>). Does not change fertility tier.
    /// </summary>
    internal static class SoilTrafficCoverage
    {
        static readonly string[] CoverageSteps =
        {
            "normal", "sparse", "verysparse", "none",
        };

        /// <summary>Most worn coverage traffic will apply (<c>none</c> looks too bare).</summary>
        public const int MaxTrafficWearIndex = 2; // verysparse

        /// <summary>One step toward worn coverage (max verysparse); fertility unchanged. Farmland ignored.</summary>
        public static bool TryCompactOneStep(ICoreAPI api, IBlockAccessor acc, BlockPos groundPos)
        {
            if (api == null || acc == null || groundPos == null) return false;

            Block ground = acc.GetBlock(groundPos);
            if (!TryBuildCoverageShift(api, ground, stepDown: true, out Block next)) return false;

            acc.SetBlock(next.BlockId, groundPos);
            acc.MarkBlockDirty(groundPos);
            return true;
        }

        /// <summary>One step toward grassier coverage when traffic eases.</summary>
        public static bool TryRestoreOneStep(ICoreAPI api, IBlockAccessor acc, BlockPos groundPos)
        {
            if (api == null || acc == null || groundPos == null) return false;

            Block ground = acc.GetBlock(groundPos);
            if (!TryBuildCoverageShift(api, ground, stepDown: false, out Block next)) return false;

            acc.SetBlock(next.BlockId, groundPos);
            acc.MarkBlockDirty(groundPos);
            return true;
        }

        /// <summary>
        /// Coverage wear index for traffic sync: 0=normal … 2=verysparse.
        /// Bare <c>none</c> counts as max wear so restore can pull it back up.
        /// </summary>
        public static int GetTrafficWearIndex(Block ground)
        {
            if (ground?.Code?.Path == null) return 0;
            if (!TrySplitSoilPath(ground.Code.Path, out _, out string coverage)) return 0;

            int idx = IndexOfCoverage(coverage);
            if (idx < 0) return 0;
            if (idx > MaxTrafficWearIndex) return MaxTrafficWearIndex;
            return idx;
        }

        internal static bool TryBuildCoverageShift(ICoreAPI api, Block ground, bool stepDown, out Block next)
        {
            next = null;
            if (api == null || ground?.Code?.Path == null || ground.Id == 0) return false;

            string path = ground.Code.Path;
            if (!path.StartsWith("soil-", StringComparison.Ordinal)) return false;
            if (WildSoilGroundRules.IsFarmland(ground)) return false;

            if (!TrySplitSoilPath(path, out string fert, out string coverage)) return false;

            string shifted = stepDown ? StepCoverageDown(coverage) : StepCoverageUp(coverage);
            if (shifted == null || shifted == coverage) return false;

            var code = new AssetLocation(ground.Code.Domain, "soil-" + fert + "-" + shifted);
            Block block = api.World.GetBlock(code);
            if (block == null || block.Id == 0 || block.Id == ground.Id) return false;

            next = block;
            return true;
        }

        internal static string StepCoverageDown(string coverage)
        {
            int idx = IndexOfCoverage(coverage);
            if (idx < 0 || idx >= MaxTrafficWearIndex) return null;
            return CoverageSteps[idx + 1];
        }

        internal static string StepCoverageUp(string coverage)
        {
            int idx = IndexOfCoverage(coverage);
            if (idx <= 0) return null;
            return CoverageSteps[idx - 1];
        }

        static int IndexOfCoverage(string coverage)
        {
            if (string.IsNullOrEmpty(coverage)) return CoverageSteps.Length - 1; // treat unknown as none
            for (int i = 0; i < CoverageSteps.Length; i++)
            {
                if (CoverageSteps[i] == coverage) return i;
            }

            return -1;
        }

        /// <summary>soil-high-normal → fert=high, coverage=normal; soil-verylow-sparse → verylow, sparse.</summary>
        internal static bool TrySplitSoilPath(string path, out string fertility, out string coverage)
        {
            fertility = null;
            coverage = "none";
            if (string.IsNullOrEmpty(path) || !path.StartsWith("soil-", StringComparison.Ordinal)) return false;

            string rest = path.Substring("soil-".Length);
            int lastDash = rest.LastIndexOf('-');
            if (lastDash <= 0 || lastDash >= rest.Length - 1) return false;

            fertility = rest.Substring(0, lastDash);
            coverage = rest.Substring(lastDash + 1);
            if (string.IsNullOrEmpty(fertility)) return false;

            if (coverage != "none" && coverage != "verysparse" && coverage != "sparse" && coverage != "normal")
            {
                return false;
            }

            return true;
        }
    }
}
