using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Reeds on muddy gravel (lake bed), including standing in shallow water.</summary>
    internal static class ReedPlacement
    {
        public static bool TryFindPlantPos(
            IBlockAccessor acc,
            BlockPos origin,
            int dx,
            int dz,
            int verticalSearch,
            int substrateSearchDepth,
            out BlockPos plantPos,
            out string failureReason)
        {
            plantPos = null;
            failureReason = null;

            int x = origin.X + dx;
            int z = origin.Z + dz;
            BlockPos best = null;
            int bestDist = int.MaxValue;

            for (int dy = verticalSearch; dy >= -verticalSearch; dy--)
            {
                int y = origin.Y + dy;
                BlockPos test = new BlockPos(x, y, z);
                if (!IsValidReedSite(acc, test, substrateSearchDepth, out _)) continue;

                int dist = System.Math.Abs(dy);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = test.Copy();
                }
            }

            if (best == null)
            {
                failureReason = "No valid reed site (need muddy gravel below)";
                return false;
            }

            plantPos = best;
            return true;
        }

        static bool IsValidReedSite(IBlockAccessor acc, BlockPos pos, int substrateSearchDepth, out string reason)
        {
            reason = null;

            Block space = acc.GetBlock(pos);
            if (space.Replaceable < SuitabilityEvaluator.ReproduceMinReplaceable)
            {
                reason = "Space blocked";
                return false;
            }

            if (!BlockFluidHelper.HasReedSiltSubstrate(acc, pos, substrateSearchDepth))
            {
                reason = "No muddy gravel below";
                return false;
            }

            return true;
        }
    }
}
