using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class SurfacePlacement
    {
        public static bool TryFindPlantPos(
            IBlockAccessor acc,
            BlockPos origin,
            int dx,
            int dz,
            int verticalSearch,
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
                if (!IsValidPlantSite(acc, test, out _)) continue;

                int dist = System.Math.Abs(dy);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = test.Copy();
                }
            }

            if (best == null)
            {
                failureReason = "No valid surface near column";
                return false;
            }

            plantPos = best;
            return true;
        }

        static bool IsValidPlantSite(IBlockAccessor acc, BlockPos pos, out string reason)
        {
            reason = null;
            Block space = acc.GetBlock(pos);
            Block ground = acc.GetBlock(pos.DownCopy());

            if (BlockFluidHelper.TouchesFluid(acc, pos))
            {
                reason = "Underwater or fluid present";
                return false;
            }

            if (!ground.SideSolid[BlockFacing.UP.Index])
            {
                reason = "No solid ground";
                return false;
            }

            if (WildSoilGroundRules.IsFarmland(ground))
            {
                reason = "Farmland excluded";
                return false;
            }

            if (space.Replaceable < SuitabilityEvaluator.ReproduceMinReplaceable)
            {
                reason = "Space blocked";
                return false;
            }

            return true;
        }
    }
}
