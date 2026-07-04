using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Whether spread may place into a cell that is not air (water, shoreline fluid, etc.).</summary>
    internal static class SpreadVacancy
    {
        public static bool CanOccupy(
            IBlockAccessor acc,
            BlockPos plantPos,
            PlantRequirements requirements,
            Block occupant,
            bool isEmpty)
        {
            if (isEmpty) return true;
            if (acc == null || plantPos == null || requirements == null) return false;

            switch (requirements.Habitat)
            {
                case EcologyHabitat.ReedNearWater:
                    return ReedColumnHelper.IsValidReedPlantSite(acc, plantPos, requirements, out _)
                        && !PlantCodeHelper.IsReedBlock(occupant);

                case EcologyHabitat.WaterSurface:
                    return WaterPlacement.IsValidWaterLilySpreadSite(acc, plantPos);

                case EcologyHabitat.UnderwaterColumn:
                    return CrowfootSpreadGuard.IsPlantableWaterCell(acc, plantPos)
                        && WaterColumnHelper.IsValidCrowfootSpreadBase(acc, plantPos, requirements);

                default:
                    return false;
            }
        }
    }
}
