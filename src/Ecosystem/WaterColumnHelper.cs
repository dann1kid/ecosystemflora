using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Crowfoot water column measurement: snap to base, depth, spread validation.</summary>
    internal static class WaterColumnHelper
    {
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

            if (!CrowfootSpreadGuard.IsPlantableWaterCell(acc, columnBase))
            {
                return false;
            }

            int plantableDepth = CountPlantableWaterLayersUp(acc, columnBase);

            int minDepth = requirements != null && requirements.MinWaterDepth > 0
                ? requirements.MinWaterDepth
                : 2;
            int maxDepth = requirements != null ? requirements.MaxWaterDepth : 8;
            return plantableDepth >= minDepth && plantableDepth <= maxDepth;
        }

        public static int CountPlantableWaterLayersUp(IBlockAccessor acc, BlockPos basePos)
        {
            if (acc == null || basePos == null) return 0;

            int count = 0;
            BlockPos scan = basePos.Copy();
            for (int i = 0; i < 12; i++)
            {
                if (!CrowfootSpreadGuard.IsPlantableWaterCell(acc, scan)) break;
                count++;
                scan.Up();
            }

            return count;
        }

        /// <summary>Lowest submerged cell in the water column (directly above substrate).</summary>
        public static bool TrySnapCrowfootColumnBase(IBlockAccessor acc, BlockPos pos, out BlockPos columnBase)
        {
            columnBase = null;
            if (!BlockFluidHelper.IsWaterAt(acc, pos) && !PlantCodeHelper.IsWatercrowfoot(acc.GetBlock(pos)?.Code))
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
            while (BlockFluidHelper.IsWaterAt(acc, peekDown))
            {
                scan.Down();
                peekDown.Set(scan.X, scan.Y - 1, scan.Z);
            }

            if (!BlockFluidHelper.IsWaterAt(acc, scan)) return false;

            Block below = acc.GetBlock(peekDown);
            if (!BlockFluidHelper.IsFertileSubstrate(below))
            {
                return false;
            }

            columnBase = scan;
            return true;
        }

        /// <summary>Water blocks from base upward, including base.</summary>
        public static int CountContiguousWaterLayersUp(IBlockAccessor acc, BlockPos basePos)
        {
            if (!BlockFluidHelper.IsWaterAt(acc, basePos)) return 0;

            int count = 0;
            BlockPos scan = basePos.Copy();
            for (int i = 0; i < 12; i++)
            {
                if (!BlockFluidHelper.IsWaterAt(acc, scan)) break;
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
    }
}
