using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Reed-specific column validation: site checks, water layers, column stacking.</summary>
    internal static class ReedColumnHelper
    {
        static readonly BlockPos reedColumnScratch = new BlockPos(0);

        /// <summary>
        /// Shore: land reed on gravel (0 water layers). Shallow: exactly 1 water cell above gravel, reed inside it.
        /// </summary>
        public static bool IsValidReedPlantSite(IBlockAccessor acc, BlockPos plantPos, PlantRequirements req, out string reason)
        {
            reason = null;

            Block space = acc.GetBlock(plantPos);
            if (PlantCodeHelper.IsReedBlock(space))
            {
                reason = "Cell already has reed";
                return false;
            }

            if (space.Replaceable < SuitabilityEvaluator.ReproduceMinReplaceable && !BlockFluidHelper.IsDedicatedWaterCell(acc, plantPos))
            {
                reason = "Space blocked";
                return false;
            }

            var gravelPos = new BlockPos(plantPos.X, plantPos.Y - 1, plantPos.Z, plantPos.dimension);
            if (!BlockFluidHelper.IsReedBedSubstrate(acc.GetBlock(gravelPos)))
            {
                reason = "Water block must sit directly on muddy or rock gravel bed";
                return false;
            }

            int waterLayers = CountWaterLayersAboveGravel(acc, gravelPos);
            if (waterLayers < 0)
            {
                reason = "Reed already on gravel column";
                return false;
            }

            int maxDepth = req.MaxWaterDepth > 0 ? req.MaxWaterDepth : 1;
            if (waterLayers > maxDepth)
            {
                reason = "More than one water block between bottom and surface";
                return false;
            }

            if (waterLayers == 1)
            {
                if (!BlockFluidHelper.IsDedicatedWaterCell(acc, plantPos))
                {
                    reason = "Reed must be placed inside the water block";
                    return false;
                }

                var abovePos = new BlockPos(plantPos.X, plantPos.Y + 1, plantPos.Z, plantPos.dimension);
                if (BlockFluidHelper.IsDedicatedWaterCell(acc, abovePos))
                {
                    reason = "Second water block above (column too deep)";
                    return false;
                }
            }
            else if (waterLayers == 0)
            {
                if (BlockFluidHelper.IsWater(acc.GetBlock(plantPos)))
                {
                    reason = "Land reed cannot replace a solid water block";
                    return false;
                }

                if (!BlockFluidHelper.HasNearbyWater(acc, plantPos, 3))
                {
                    reason = "Land reeds need water within 3 blocks";
                    return false;
                }
            }

            if (req.ExactWaterDepth >= 0 && waterLayers != req.ExactWaterDepth)
            {
                reason = "Need exactly " + req.ExactWaterDepth + " water block(s) above gravel";
                return false;
            }

            if (!BlockFluidHelper.HasVerticalClearance(acc, plantPos, req.VerticalBlocks))
            {
                reason = "Not enough vertical space";
                return false;
            }

            if (HasReedInSameColumn(acc, plantPos, req.Species))
            {
                reason = "Another reed already in this column";
                return false;
            }

            return true;
        }

        /// <summary>Contiguous solid water blocks directly above reed bed (shoreline fluid alone does not count).</summary>
        public static int CountWaterLayersAboveGravel(IBlockAccessor acc, BlockPos gravelPos)
        {
            if (!BlockFluidHelper.IsReedBedSubstrate(acc.GetBlock(gravelPos))) return -1;

            int count = 0;
            BlockPos scan = new BlockPos(gravelPos.X, gravelPos.Y + 1, gravelPos.Z, gravelPos.dimension);
            for (int i = 0; i < 6; i++)
            {
                if (PlantCodeHelper.IsReedBlock(acc.GetBlock(scan))) return -1;

                Block solid = acc.GetBlock(scan);
                if (BlockFluidHelper.IsWater(solid))
                {
                    count++;
                    scan.Up();
                    continue;
                }

                return count;
            }

            return count;
        }

        public static bool HasReedInSameColumn(IBlockAccessor acc, BlockPos plantPos, string species)
        {
            if (string.IsNullOrEmpty(species)) return false;

            reedColumnScratch.Set(plantPos.X, plantPos.Y + 1, plantPos.Z);
            if (IsSameReedSpecies(acc.GetBlock(reedColumnScratch), species)) return true;
            reedColumnScratch.Set(plantPos.X, plantPos.Y - 1, plantPos.Z);
            if (IsSameReedSpecies(acc.GetBlock(reedColumnScratch), species)) return true;

            BlockPos scan = new BlockPos(plantPos.X, plantPos.Y - 1, plantPos.Z, plantPos.dimension);
            for (int i = 0; i < 6; i++)
            {
                Block block = acc.GetBlock(scan);
                if (BlockFluidHelper.IsReedBedSubstrate(block)) return false;
                if (IsSameReedSpecies(block, species)) return true;
                if (!BlockFluidHelper.IsWaterAt(acc, scan) && block.Replaceable < SuitabilityEvaluator.ReproduceMinReplaceable)
                {
                    return false;
                }

                scan.Down();
            }

            return false;
        }

        static bool IsSameReedSpecies(Block block, string species)
        {
            if (block == null || block.Id == 0) return false;
            return PlantCodeHelper.GetEcologySpecies(block.Code) == species;
        }

        static readonly BlockPos reedSiltScratch = new BlockPos(0);

        public static bool HasReedSiltSubstrate(IBlockAccessor acc, BlockPos plantPos, int maxDepth)
        {
            reedSiltScratch.Set(plantPos.X, plantPos.Y - 1, plantPos.Z);
            return BlockFluidHelper.IsReedBedSubstrate(acc.GetBlock(reedSiltScratch));
        }

        public static bool MeetsReedWaterDepth(PlantRequirements req, IBlockAccessor acc, BlockPos plantPos)
        {
            return IsValidReedPlantSite(acc, plantPos, req, out _);
        }
    }
}
