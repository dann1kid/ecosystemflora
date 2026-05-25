using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
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
        /// Bottom = muddy gravel, middle = this cell, top = air/surface.
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

            var gravelPos = new BlockPos(plantPos.X, plantPos.Y - 1, plantPos.Z, plantPos.dimension);
            if (!IsReedBedSubstrate(acc.GetBlock(gravelPos)))
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
                if (!IsDedicatedWaterCell(acc, plantPos))
                {
                    reason = "Reed must be placed inside the water block";
                    return false;
                }

                var abovePos = new BlockPos(plantPos.X, plantPos.Y + 1, plantPos.Z, plantPos.dimension);
                if (IsDedicatedWaterCell(acc, abovePos))
                {
                    reason = "Second water block above (column too deep)";
                    return false;
                }
            }
            else if (waterLayers == 0)
            {
                if (IsWater(acc.GetBlock(plantPos)))
                {
                    reason = "Land reed cannot replace a solid water block";
                    return false;
                }

                if (!HasNearbyWater(acc, plantPos, 3))
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

        /// <summary>Contiguous solid water blocks directly above reed bed (shoreline fluid alone does not count).</summary>
        public static int CountWaterLayersAboveGravel(IBlockAccessor acc, BlockPos gravelPos)
        {
            if (!IsReedBedSubstrate(acc.GetBlock(gravelPos))) return -1;

            int count = 0;
            BlockPos scan = new BlockPos(gravelPos.X, gravelPos.Y + 1, gravelPos.Z, gravelPos.dimension);
            for (int i = 0; i < 6; i++)
            {
                if (PlantCodeHelper.IsReedBlock(acc.GetBlock(scan))) return -1;

                Block solid = acc.GetBlock(scan);
                if (IsWater(solid))
                {
                    count++;
                    scan.Up();
                    continue;
                }

                return count;
            }

            return count;
        }

        public static bool IsValidCrowfootSpreadBase(
            IBlockAccessor acc,
            BlockPos pos,
            PlantRequirements requirements)
        {
            if (!TrySnapCrowfootColumnBase(acc, pos, out BlockPos columnBase))
            {
                return false;
            }

            if (PlantCodeHelper.IsWatercrowfoot(acc.GetBlock(columnBase)?.Code))
            {
                return false;
            }

            if (!TryMeasureWaterColumn(acc, columnBase, out int waterDepth, out _))
            {
                return false;
            }

            int minDepth = requirements != null && requirements.MinWaterDepth > 0
                ? requirements.MinWaterDepth
                : 2;
            int maxDepth = requirements != null ? requirements.MaxWaterDepth : 8;
            return waterDepth >= minDepth && waterDepth <= maxDepth;
        }

        static readonly BlockPos reedColumnScratch = new BlockPos(0);

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
                if (IsReedBedSubstrate(block)) return false;
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

        static readonly BlockPos reedSiltScratch = new BlockPos(0);

        public static bool HasReedSiltSubstrate(IBlockAccessor acc, BlockPos plantPos, int maxDepth)
        {
            reedSiltScratch.Set(plantPos.X, plantPos.Y - 1, plantPos.Z);
            return IsReedBedSubstrate(acc.GetBlock(reedSiltScratch));
        }

        public static bool MeetsReedWaterDepth(PlantRequirements req, IBlockAccessor acc, BlockPos plantPos)
        {
            return IsValidReedPlantSite(acc, plantPos, req, out _);
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

            var peekDown = new BlockPos(0);
            peekDown.Set(scan.X, scan.Y - 1, scan.Z);
            while (IsWaterAt(acc, peekDown))
            {
                scan.Down();
                peekDown.Set(scan.X, scan.Y - 1, scan.Z);
            }

            if (!IsWaterAt(acc, scan)) return false;

            Block below = acc.GetBlock(peekDown);
            if (!IsFertileSubstrate(below))
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
