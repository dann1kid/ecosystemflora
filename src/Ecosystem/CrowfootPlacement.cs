using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class CrowfootPlacement
    {
        public static bool TryFindPlantPos(
            IBlockAccessor acc,
            BlockPos origin,
            int dx,
            int dz,
            int verticalSearch,
            PlantRequirements requirements,
            out BlockPos basePos,
            out string failureReason)
        {
            basePos = null;
            failureReason = null;

            int x = origin.X + dx;
            int z = origin.Z + dz;
            BlockPos best = null;
            int bestDist = int.MaxValue;

            for (int dy = verticalSearch; dy >= -verticalSearch; dy--)
            {
                int y = origin.Y + dy;
                BlockPos test = new BlockPos(x, y, z);
                if (!IsValidCrowfootBase(acc, test, requirements, out _)) continue;

                int dist = System.Math.Abs(dy);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = test.Copy();
                }
            }

            if (best == null)
            {
                failureReason = "No valid water crowfoot base";
                return false;
            }

            basePos = best;
            return true;
        }

        static bool IsValidCrowfootBase(IBlockAccessor acc, BlockPos pos, PlantRequirements requirements, out string reason)
        {
            reason = null;

            Block space = acc.GetBlock(pos);
            if (space.Replaceable < SuitabilityEvaluator.ReproduceMinReplaceable
                && !PlantCodeHelper.IsWatercrowfoot(space.Code))
            {
                reason = "Space blocked";
                return false;
            }

            if (!BlockFluidHelper.IsPlantInWater(acc, pos) && !PlantCodeHelper.IsWatercrowfoot(space.Code))
            {
                reason = "Not underwater";
                return false;
            }

            if (!BlockFluidHelper.TryMeasureUnderwaterColumnDepth(acc, pos, out int waterDepth, out bool hasSubstrate))
            {
                reason = "No substrate below";
                return false;
            }

            if (!hasSubstrate)
            {
                reason = "No fertile bed below";
                return false;
            }

            int minDepth = requirements.MinWaterDepth > 0 ? requirements.MinWaterDepth : 2;
            if (waterDepth < minDepth)
            {
                reason = "Water too shallow";
                return false;
            }

            if (waterDepth > requirements.MaxWaterDepth)
            {
                reason = "Water too deep";
                return false;
            }

            return true;
        }
    }
}
