using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Pre-fetched solid + fluid layers for a plant cell and the ground below.
    /// Populated once per candidate, then threaded through preflight, context sampling,
    /// and scoring to eliminate redundant GetBlock calls.
    /// </summary>
    internal readonly struct CellBlockSnapshot
    {
        public readonly Block Space;
        public readonly Block Ground;
        public readonly Block FluidAt;
        public readonly Block FluidBelow;

        CellBlockSnapshot(Block space, Block ground, Block fluidAt, Block fluidBelow)
        {
            Space = space;
            Ground = ground;
            FluidAt = fluidAt;
            FluidBelow = fluidBelow;
        }

        internal static CellBlockSnapshot FromBlockIds(
            System.Collections.Generic.IList<Block> blocks,
            int spaceId,
            int groundId)
        {
            Block air = ResolveBlock(blocks, 0);
            return new CellBlockSnapshot(
                ResolveBlock(blocks, spaceId) ?? air,
                ResolveBlock(blocks, groundId) ?? air,
                null,
                null);
        }

        static Block ResolveBlock(System.Collections.Generic.IList<Block> blocks, int id)
        {
            if (blocks == null || id <= 0 || id >= blocks.Count) return null;
            return blocks[id];
        }

        public bool TouchesFluid => PlantVacancyRules.TouchesSpreadBlockingFluid(
            Space, Ground, FluidAt, FluidBelow);

        static readonly BlockPos scratchDown = new BlockPos(0);

        public static CellBlockSnapshot Sample(IBlockAccessor acc, BlockPos plantPos)
        {
            Block space = acc.GetBlock(plantPos);
            scratchDown.Set(plantPos.X, plantPos.Y - 1, plantPos.Z);
            Block ground = acc.GetBlock(scratchDown);
            Block fluidAt = acc.GetBlock(plantPos, BlockLayersAccess.Fluid);
            Block fluidBelow = acc.GetBlock(scratchDown, BlockLayersAccess.Fluid);
            return new CellBlockSnapshot(space, ground, fluidAt, fluidBelow);
        }
    }
}
