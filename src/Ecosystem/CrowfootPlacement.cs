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
            int bestY = int.MaxValue;

            for (int dy = verticalSearch; dy >= -verticalSearch; dy--)
            {
                int y = origin.Y + dy;
                BlockPos test = new BlockPos(x, y, z);
                if (!IsValidCrowfootBase(acc, test, requirements, out BlockPos columnBase, out _)) continue;

                if (columnBase.Y < bestY)
                {
                    bestY = columnBase.Y;
                    best = columnBase.Copy();
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

        static bool IsValidCrowfootBase(
            IBlockAccessor acc,
            BlockPos pos,
            PlantRequirements requirements,
            out BlockPos columnBase,
            out string reason)
        {
            columnBase = null;
            reason = null;

            if (!BlockFluidHelper.TrySnapCrowfootColumnBase(acc, pos, out columnBase))
            {
                reason = "Not underwater or no bed below";
                return false;
            }

            if (!BlockFluidHelper.TryMeasureWaterColumn(acc, columnBase, out int waterDepth, out _))
            {
                reason = "No water column";
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
