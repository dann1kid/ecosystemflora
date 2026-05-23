using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class TreePlacement
    {
        public static bool TryFindSaplingPos(
            IBlockAccessor acc,
            BlockPos origin,
            int dx,
            int dz,
            int verticalSearch,
            int minSunlight,
            out BlockPos plantPos,
            out string failureReason)
        {
            if (!SurfacePlacement.TryFindPlantPos(acc, origin, dx, dz, verticalSearch, out plantPos, out failureReason))
            {
                return false;
            }

            if (minSunlight > 0 && !HasEnoughSunlight(acc, plantPos, minSunlight))
            {
                failureReason = "Not enough sunlight for sapling";
                plantPos = null;
                return false;
            }

            return true;
        }

        public static bool HasEnoughSunlight(IBlockAccessor acc, BlockPos plantPos, int minSunlight)
        {
            int light = acc.GetLightLevel(plantPos, EnumLightLevelType.OnlySunLight);
            return light >= minSunlight;
        }
    }
}
