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

        /// <summary>Plant cell itself is water/fluid (invalid for reeds on shore).</summary>
        public static bool IsSubmergedPlantCell(IBlockAccessor acc, BlockPos plantPos)
        {
            if (IsFluid(acc.GetBlock(plantPos))) return true;
            if (IsFluid(acc.GetBlock(plantPos, BlockLayersAccess.Fluid))) return true;
            return false;
        }

        /// <summary>Water within horizontal radius (same Y or one below), for NearWater reeds.</summary>
        public static bool HasAdjacentWater(IBlockAccessor acc, BlockPos plantPos, int horizontalRadius)
        {
            if (horizontalRadius < 1) horizontalRadius = 1;

            for (int dx = -horizontalRadius; dx <= horizontalRadius; dx++)
            {
                for (int dz = -horizontalRadius; dz <= horizontalRadius; dz++)
                {
                    if (dx == 0 && dz == 0) continue;

                    for (int dy = 0; dy >= -1; dy--)
                    {
                        BlockPos check = new BlockPos(plantPos.X + dx, plantPos.Y + dy, plantPos.Z + dz);
                        if (IsFluid(acc.GetBlock(check, BlockLayersAccess.Fluid))) return true;
                        if (IsFluid(acc.GetBlock(check))) return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Water lily: fluid under the cell, air/replaceable at plant Y.</summary>
        public static bool HasWaterSurfaceSupport(IBlockAccessor acc, BlockPos plantPos)
        {
            if (IsSubmergedPlantCell(acc, plantPos)) return false;

            Block below = acc.GetBlock(plantPos.DownCopy());
            Block fluidBelow = acc.GetBlock(plantPos.DownCopy(), BlockLayersAccess.Fluid);
            if (IsFluid(fluidBelow) || IsFluid(below)) return true;

            return IsFluid(acc.GetBlock(plantPos, BlockLayersAccess.Fluid));
        }

        /// <summary>Илистый гравий (game:muddygravel) — дно озёр/берегов в worldgen.</summary>
        public static bool IsMuddyGravel(Block block)
        {
            string path = block?.Code?.Path;
            if (string.IsNullOrEmpty(path)) return false;
            return path == "muddygravel" || path.StartsWith("muddygravel");
        }

        /// <summary>Scan downward through water/air/plants for lake-bed muddy gravel.</summary>
        public static bool HasReedSiltSubstrate(IBlockAccessor acc, BlockPos plantPos, int maxDepth)
        {
            if (maxDepth < 1) maxDepth = 1;

            BlockPos scan = plantPos.DownCopy();
            for (int i = 0; i < maxDepth; i++)
            {
                Block block = acc.GetBlock(scan);
                if (IsMuddyGravel(block)) return true;
                if (!CanScanThroughForReedSubstrate(block)) return false;
                scan.Down();
            }

            return false;
        }

        static bool CanScanThroughForReedSubstrate(Block block)
        {
            if (block == null || block.Id == 0) return true;
            if (IsFluid(block)) return true;
            if (block.Replaceable >= SuitabilityEvaluator.ReproduceMinReplaceable) return true;
            return false;
        }
    }
}
