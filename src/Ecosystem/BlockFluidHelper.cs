using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Core fluid/water/substrate detection primitives. Reed → ReedColumnHelper, crowfoot → WaterColumnHelper.</summary>
    internal static class BlockFluidHelper
    {
        static readonly BlockPos touchScratch = new BlockPos(0);

        public static bool IsFluid(Block block)
        {
            if (block == null || block.Id == 0) return false;
            if (block.LiquidLevel > 0) return true;
            if (block.MatterState == EnumMatterState.Liquid) return true;
            if (block.ForFluidsLayer) return true;
            return block.IsLiquid();
        }

        public static bool IsWater(Block block)
        {
            if (!IsFluid(block)) return false;
            string code = block.Code?.Path;
            return code != null && code.StartsWith("water");
        }

        public static bool IsWaterAt(IBlockAccessor acc, BlockPos pos)
        {
            return IsWater(acc.GetBlock(pos)) || IsWater(acc.GetBlock(pos, BlockLayersAccess.Fluid));
        }

        public static bool TouchesFluid(IBlockAccessor acc, BlockPos plantPos)
        {
            Block space = acc.GetBlock(plantPos);
            touchScratch.Set(plantPos.X, plantPos.Y - 1, plantPos.Z);
            Block ground = acc.GetBlock(touchScratch);
            Block fluidAt = acc.GetBlock(plantPos, BlockLayersAccess.Fluid);
            Block fluidBelow = acc.GetBlock(touchScratch, BlockLayersAccess.Fluid);

            return IsFluid(space)
                || IsFluid(ground)
                || IsFluid(fluidAt)
                || IsFluid(fluidBelow);
        }

        public static bool IsPlantInWater(IBlockAccessor acc, BlockPos plantPos)
        {
            return IsWaterAt(acc, plantPos);
        }

        public static bool IsMuddyGravel(Block block)
        {
            string path = block?.Code?.Path;
            if (string.IsNullOrEmpty(path)) return false;
            return path == "muddygravel" || path.StartsWith("muddygravel");
        }

        /// <summary>Lake/river bed for reeds: muddy gravel or rock gravel (e.g. gravel-granite under water).</summary>
        public static bool IsReedBedSubstrate(Block block)
        {
            if (block == null || block.Id == 0) return false;
            if (IsMuddyGravel(block)) return true;

            string path = block.Code?.Path;
            if (string.IsNullOrEmpty(path)) return false;

            if (path == "gravel" || path.StartsWith("gravel-"))
            {
                return block.SideSolid[BlockFacing.UP.Index];
            }

            return false;
        }

        public static bool HasNearbyWater(IBlockAccessor acc, BlockPos center, int radius)
        {
            if (acc == null || center == null || radius < 0) return false;

            var scanPos = new BlockPos(0);
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        scanPos.Set(center.X + dx, center.Y + dy, center.Z + dz);
                        if (IsWaterAt(acc, scanPos))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static bool IsFertileSubstrate(Block block)
        {
            if (block == null || block.Id == 0) return false;
            if (IsReedBedSubstrate(block)) return true;
            return block.Fertility > 0 && block.SideSolid[BlockFacing.UP.Index];
        }

        /// <summary>
        /// A single water column cell (not shore fluid on gravel without a water block).
        /// </summary>
        public static bool IsDedicatedWaterCell(IBlockAccessor acc, BlockPos pos)
        {
            Block solid = acc.GetBlock(pos);
            if (IsReedBedSubstrate(solid) || PlantCodeHelper.IsReedBlock(solid)) return false;

            if (IsWater(solid)) return true;

            if (solid.Replaceable >= SuitabilityEvaluator.ReproduceMinReplaceable)
            {
                return IsWater(acc.GetBlock(pos, BlockLayersAccess.Fluid));
            }

            return false;
        }

        static readonly BlockPos clearanceScratch = new BlockPos(0);

        public static bool HasVerticalClearance(IBlockAccessor acc, BlockPos basePos, int verticalBlocks)
        {
            if (verticalBlocks <= 1) return true;

            for (int i = 1; i < verticalBlocks; i++)
            {
                clearanceScratch.Set(basePos.X, basePos.Y + i, basePos.Z);
                Block above = acc.GetBlock(clearanceScratch);
                if (above.Replaceable >= SuitabilityEvaluator.ReproduceMinReplaceable) continue;
                if (IsFluid(above)) continue;
                return false;
            }

            return true;
        }

        static readonly BlockPos waterSurfScratch = new BlockPos(0);

        public static bool HasWaterSurfaceSupport(IBlockAccessor acc, BlockPos plantPos)
        {
            waterSurfScratch.Set(plantPos.X, plantPos.Y - 1, plantPos.Z);
            Block below = acc.GetBlock(waterSurfScratch);
            Block fluidBelow = acc.GetBlock(waterSurfScratch, BlockLayersAccess.Fluid);
            if (IsFluid(fluidBelow) || IsFluid(below)) return true;
            return IsFluid(acc.GetBlock(plantPos, BlockLayersAccess.Fluid));
        }

        public static bool IsSubmergedWaterCell(IBlockAccessor acc, BlockPos pos)
        {
            return IsWaterAt(acc, pos);
        }
    }
}
