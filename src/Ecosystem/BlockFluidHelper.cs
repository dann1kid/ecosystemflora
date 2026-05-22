using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class BlockFluidHelper
    {
        public static bool IsFluid(Block block)
        {
            if (block == null || block.Id == 0) return false;
            if (block.LiquidLevel > 0) return true;
            if (block.MatterState == EnumMatterState.Liquid) return true;
            if (block.ForFluidsLayer) return true;
            return block.IsLiquid();
        }

        public static bool TouchesFluid(IBlockAccessor acc, BlockPos plantPos)
        {
            Block space = acc.GetBlock(plantPos);
            Block ground = acc.GetBlock(plantPos.DownCopy());
            Block fluidAt = acc.GetBlock(plantPos, BlockLayersAccess.Fluid);
            Block fluidBelow = acc.GetBlock(plantPos.DownCopy(), BlockLayersAccess.Fluid);

            return IsFluid(space)
                || IsFluid(ground)
                || IsFluid(fluidAt)
                || IsFluid(fluidBelow);
        }
    }
}
