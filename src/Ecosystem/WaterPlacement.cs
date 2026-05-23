using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class WaterPlacement
    {
        public static bool TryFindPlantPos(
            IBlockAccessor acc,
            BlockPos origin,
            int dx,
            int dz,
            int verticalSearch,
            PlantRequirements requirements,
            out BlockPos plantPos,
            out string failureReason)
        {
            plantPos = null;
            failureReason = null;

            if (requirements.Habitat == EcologyHabitat.ReedNearWater)
            {
                return ReedPlacement.TryFindPlantPos(
                    acc, origin, dx, dz, verticalSearch, requirements, out plantPos, out failureReason);
            }

            if (requirements.Habitat == EcologyHabitat.UnderwaterColumn)
            {
                return CrowfootPlacement.TryFindPlantPos(
                    acc, origin, dx, dz, verticalSearch, requirements, out plantPos, out failureReason);
            }

            return TryFindWaterLilyPos(acc, origin, dx, dz, verticalSearch, out plantPos, out failureReason);
        }

        static bool TryFindWaterLilyPos(
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
                if (!IsValidWaterLilySite(acc, test, out _)) continue;

                int dist = System.Math.Abs(dy);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = test.Copy();
                }
            }

            if (best == null)
            {
                failureReason = "No valid water lily site";
                return false;
            }

            plantPos = best;
            return true;
        }

        static bool IsValidWaterLilySite(IBlockAccessor acc, BlockPos pos, out string reason)
        {
            reason = null;
            Block space = acc.GetBlock(pos);

            if (space.Replaceable < SuitabilityEvaluator.ReproduceMinReplaceable)
            {
                reason = "Space blocked";
                return false;
            }

            if (!BlockFluidHelper.HasWaterSurfaceSupport(acc, pos))
            {
                reason = "No water surface below";
                return false;
            }

            return true;
        }
    }
}
