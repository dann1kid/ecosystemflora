using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Shared rules for whether a plant cell is vacant and physically ready for wild spread.
    /// Keeps SurfacePlacement, SpreadPreflight, and suitability checks aligned (air id=0, replaceable debris, etc.).
    /// </summary>
    internal static class PlantVacancyRules
    {
        public static bool IsVacantPlantSpace(Block space)
        {
            if (space == null) return false;
            if (space.Id == 0) return true;
            return space.Replaceable >= SuitabilityEvaluator.ReproduceMinReplaceable;
        }

        /// <summary>Replaceable used in <see cref="IEnvironmentalContext"/> — air must not read as 0.</summary>
        public static int EffectiveSpaceReplaceable(Block space)
        {
            if (space == null) return 0;
            if (space.Id == 0) return int.MaxValue;
            return space.Replaceable;
        }

        public static bool MeetsMinReplaceable(int spaceReplaceable, int minReplaceable) =>
            spaceReplaceable >= minReplaceable;

        public static bool IsSupportingGround(Block ground)
        {
            if (ground == null || ground.Id == 0) return false;
            if (WildSoilGroundRules.IsFarmland(ground)) return true;
            return ground.SideSolid[BlockFacing.UP.Index];
        }

        /// <summary>Fluid layers with id=0 are ignored (empty fluid slot).</summary>
        public static bool TouchesSpreadBlockingFluid(
            Block space,
            Block ground,
            Block fluidAt,
            Block fluidBelow)
        {
            if (BlockFluidHelper.IsFluid(space)) return true;
            if (BlockFluidHelper.IsFluid(ground)) return true;
            if (fluidAt != null && fluidAt.Id != 0 && BlockFluidHelper.IsFluid(fluidAt)) return true;
            if (fluidBelow != null && fluidBelow.Id != 0 && BlockFluidHelper.IsFluid(fluidBelow)) return true;
            return false;
        }

        public static bool TouchesSpreadBlockingFluid(in CellBlockSnapshot snap) =>
            TouchesSpreadBlockingFluid(snap.Space, snap.Ground, snap.FluidAt, snap.FluidBelow);
    }
}
