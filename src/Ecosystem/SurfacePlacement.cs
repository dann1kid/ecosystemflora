using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class SurfacePlacement
    {
        static readonly BlockPos scanPos = new BlockPos(0);
        static readonly BlockPos groundScratch = new BlockPos(0);

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
                scanPos.Set(x, y, z);
                if (!IsValidPlantSite(acc, scanPos)) continue;

                int dist = System.Math.Abs(dy);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = scanPos.Copy();
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

        static bool IsValidPlantSite(IBlockAccessor acc, BlockPos pos)
        {
            Block space = acc.GetBlock(pos);
            groundScratch.Set(pos.X, pos.Y - 1, pos.Z);
            Block ground = acc.GetBlock(groundScratch);

            Block fluidAt = acc.GetBlock(pos, BlockLayersAccess.Fluid);
            Block fluidBelow = acc.GetBlock(groundScratch, BlockLayersAccess.Fluid);
            if (BlockFluidHelper.IsFluid(space)
                || BlockFluidHelper.IsFluid(ground)
                || BlockFluidHelper.IsFluid(fluidAt)
                || BlockFluidHelper.IsFluid(fluidBelow))
            {
                return false;
            }

            if (!ground.SideSolid[BlockFacing.UP.Index])
            {
                return false;
            }

            if (WildSoilGroundRules.IsFarmland(ground))
            {
                return false;
            }

            if (space.Replaceable < SuitabilityEvaluator.ReproduceMinReplaceable)
            {
                return false;
            }

            return true;
        }
    }
}
