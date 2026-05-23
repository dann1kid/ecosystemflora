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
            Block ground = acc.GetBlock(plantPos.DownCopy());
            Block fluidAt = acc.GetBlock(plantPos, BlockLayersAccess.Fluid);
            Block fluidBelow = acc.GetBlock(plantPos.DownCopy(), BlockLayersAccess.Fluid);

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

        public static bool IsFertileSubstrate(Block block)
        {
            if (block == null || block.Id == 0) return false;
            if (IsMuddyGravel(block)) return true;
            return block.Fertility > 0 && block.SideSolid[BlockFacing.UP.Index];
        }

        /// <summary>
        /// A single water column cell (not shore fluid on gravel without a water block).
        /// Bottom = muddy gravel, middle = this cell, top = air/surface.
        /// </summary>
        public static bool IsDedicatedWaterCell(IBlockAccessor acc, BlockPos pos)
        {
            Block solid = acc.GetBlock(pos);
            if (IsMuddyGravel(solid) || PlantCodeHelper.IsReedBlock(solid)) return false;

            if (IsWater(solid)) return true;

            if (solid.Replaceable >= SuitabilityEvaluator.ReproduceMinReplaceable)
            {
                return IsWater(acc.GetBlock(pos, BlockLayersAccess.Fluid));
            }

            return false;
        }

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

            if (space.Replaceable < SuitabilityEvaluator.ReproduceMinReplaceable && !IsDedicatedWaterCell(acc, plantPos))
            {
                reason = "Space blocked";
                return false;
            }

            BlockPos gravelPos = plantPos.DownCopy();
            if (!IsMuddyGravel(acc.GetBlock(gravelPos)))
            {
                reason = "Water block must sit directly on muddy gravel";
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
                if (!IsDedicatedWaterCell(acc, plantPos))
                {
                    reason = "Reed must be placed inside the water block";
                    return false;
                }

                if (IsDedicatedWaterCell(acc, plantPos.UpCopy()))
                {
                    reason = "Second water block above (column too deep)";
                    return false;
                }
            }
            else if (waterLayers == 0)
            {
                if (IsDedicatedWaterCell(acc, plantPos))
                {
                    reason = "Single water layer required for submerged reed";
                    return false;
                }
            }

            if (req.ExactWaterDepth >= 0 && waterLayers != req.ExactWaterDepth)
            {
                reason = "Need exactly " + req.ExactWaterDepth + " water block(s) above gravel";
                return false;
            }

            if (!HasVerticalClearance(acc, plantPos, req.VerticalBlocks))
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

        /// <summary>Contiguous dedicated water cells directly above gravel.</summary>
        public static int CountWaterLayersAboveGravel(IBlockAccessor acc, BlockPos gravelPos)
        {
            if (!IsMuddyGravel(acc.GetBlock(gravelPos))) return -1;

            int count = 0;
            BlockPos scan = gravelPos.UpCopy();
            for (int i = 0; i < 6; i++)
            {
                if (PlantCodeHelper.IsReedBlock(acc.GetBlock(scan))) return -1;

                if (IsDedicatedWaterCell(acc, scan))
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

            if (IsSameReedSpecies(acc.GetBlock(plantPos.UpCopy()), species)) return true;
            if (IsSameReedSpecies(acc.GetBlock(plantPos.DownCopy()), species)) return true;

            BlockPos scan = plantPos.DownCopy();
            for (int i = 0; i < 6; i++)
            {
                Block block = acc.GetBlock(scan);
                if (IsMuddyGravel(block)) return false;
                if (IsSameReedSpecies(block, species)) return true;
                if (!IsWaterAt(acc, scan) && block.Replaceable < SuitabilityEvaluator.ReproduceMinReplaceable)
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

        public static bool HasReedSiltSubstrate(IBlockAccessor acc, BlockPos plantPos, int maxDepth)
        {
            return IsMuddyGravel(acc.GetBlock(plantPos.DownCopy()));
        }

        public static bool MeetsReedWaterDepth(PlantRequirements req, IBlockAccessor acc, BlockPos plantPos)
        {
            return IsValidReedPlantSite(acc, plantPos, req, out _);
        }

        public static bool HasVerticalClearance(IBlockAccessor acc, BlockPos basePos, int verticalBlocks)
        {
            if (verticalBlocks <= 1) return true;

            for (int i = 1; i < verticalBlocks; i++)
            {
                Block above = acc.GetBlock(basePos.UpCopy(i));
                if (above.Replaceable >= SuitabilityEvaluator.ReproduceMinReplaceable) continue;
                if (IsFluid(above)) continue;
                return false;
            }

            return true;
        }

        public static bool HasWaterSurfaceSupport(IBlockAccessor acc, BlockPos plantPos)
        {
            Block below = acc.GetBlock(plantPos.DownCopy());
            Block fluidBelow = acc.GetBlock(plantPos.DownCopy(), BlockLayersAccess.Fluid);
            if (IsFluid(fluidBelow) || IsFluid(below)) return true;
            return IsFluid(acc.GetBlock(plantPos, BlockLayersAccess.Fluid));
        }

        public static bool TryMeasureUnderwaterColumnDepth(IBlockAccessor acc, BlockPos basePos, out int waterDepth, out bool hasSubstrate)
        {
            waterDepth = 0;
            hasSubstrate = false;

            if (!TryMeasureWaterColumn(acc, basePos, out int totalDepth, out BlockPos _))
            {
                return false;
            }

            waterDepth = totalDepth;
            hasSubstrate = true;
            return true;
        }

        /// <summary>Lowest submerged cell in the water column (directly above substrate).</summary>
        public static bool TrySnapCrowfootColumnBase(IBlockAccessor acc, BlockPos pos, out BlockPos columnBase)
        {
            columnBase = null;
            if (!IsWaterAt(acc, pos) && !PlantCodeHelper.IsWatercrowfoot(acc.GetBlock(pos)?.Code))
            {
                return false;
            }

            BlockPos scan = pos.Copy();
            if (PlantCodeHelper.IsWatercrowfoot(acc.GetBlock(scan)?.Code))
            {
                scan = PlantCodeHelper.GetColumnBase(acc, scan);
            }

            while (IsWaterAt(acc, scan.DownCopy()))
            {
                scan.Down();
            }

            if (!IsWaterAt(acc, scan)) return false;

            Block below = acc.GetBlock(scan.DownCopy());
            if (!IsFertileSubstrate(below) && !IsMuddyGravel(below))
            {
                return false;
            }

            columnBase = scan;
            return true;
        }

        /// <summary>Water blocks from base upward, including base.</summary>
        public static int CountContiguousWaterLayersUp(IBlockAccessor acc, BlockPos basePos)
        {
            if (!IsWaterAt(acc, basePos)) return 0;

            int count = 0;
            BlockPos scan = basePos.Copy();
            for (int i = 0; i < 12; i++)
            {
                if (!IsWaterAt(acc, scan)) break;
                count++;
                scan.Up();
            }

            return count;
        }

        /// <summary>Total water column height above fertile/muddy bed; <paramref name="columnBase"/> is the lowest water cell.</summary>
        public static bool TryMeasureWaterColumn(IBlockAccessor acc, BlockPos anyPosInColumn, out int totalDepth, out BlockPos columnBase)
        {
            totalDepth = 0;
            columnBase = null;

            if (!TrySnapCrowfootColumnBase(acc, anyPosInColumn, out columnBase))
            {
                return false;
            }

            totalDepth = CountContiguousWaterLayersUp(acc, columnBase);
            return totalDepth > 0;
        }

        public static bool IsSubmergedWaterCell(IBlockAccessor acc, BlockPos pos)
        {
            return IsWaterAt(acc, pos);
        }
    }
}
