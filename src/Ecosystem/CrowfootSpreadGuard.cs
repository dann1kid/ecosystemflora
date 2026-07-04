using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Placement guards for water-crowfoot columns (no solid devices / block entities).</summary>
    internal static class CrowfootSpreadGuard
    {
        public static bool IsPlantableWaterCell(IBlockAccessor acc, BlockPos pos)
        {
            if (acc == null || pos == null || !acc.IsValidPos(pos)) return false;
            if (acc.GetBlockEntity(pos) != null) return false;
            if (!BlockFluidHelper.IsDedicatedWaterCell(acc, pos)) return false;

            Block solid = acc.GetBlock(pos);
            if (solid == null || solid.Id == 0) return true;
            if (PlantCodeHelper.IsWatercrowfoot(solid.Code)) return false;
            return BlockFluidHelper.IsWater(solid);
        }
    }

}
