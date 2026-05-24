using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class ReedPlacement
    {
        public static bool TryFindPlantPos(
            IBlockAccessor acc,
            BlockPos origin,
            int dx,
            int dz,
            int verticalSearch,
            PlantRequirements requirements,
            out BlockPos plantPos,
            out string failureReason)
        {
            plantPos = null;
            failureReason = null;

            int x = origin.X + dx;
            int z = origin.Z + dz;
            int yMin = origin.Y - verticalSearch;
            int yMax = origin.Y + verticalSearch;

            BlockPos best = null;
            int bestDist = int.MaxValue;
            int maxDepth = requirements.MaxWaterDepth > 0 ? requirements.MaxWaterDepth : 1;

            for (int gy = yMin; gy <= yMax; gy++)
            {
                BlockPos gravelPos = new BlockPos(x, gy, z);
                if (!BlockFluidHelper.IsReedBedSubstrate(acc.GetBlock(gravelPos))) continue;

                int waterLayers = BlockFluidHelper.CountWaterLayersAboveGravel(acc, gravelPos);
                if (waterLayers < 0 || waterLayers > maxDepth) continue;

                // gy+1 = either land on gravel or the single water block (reed goes inside it).
                BlockPos candidate = gravelPos.UpCopy();
                if (waterLayers == 1 && !BlockFluidHelper.IsDedicatedWaterCell(acc, candidate)) continue;

                TryPickCandidate(acc, candidate, origin.Y, ref best, ref bestDist, requirements);
            }

            if (best == null)
            {
                failureReason = "No valid reed site on lake/shore bed";
                return false;
            }

            plantPos = best;
            return true;
        }

        static void TryPickCandidate(
            IBlockAccessor acc,
            BlockPos candidate,
            int originY,
            ref BlockPos best,
            ref int bestDist,
            PlantRequirements requirements)
        {
            if (!BlockFluidHelper.IsValidReedPlantSite(acc, candidate, requirements, out _)) return;

            int dist = System.Math.Abs(candidate.Y - originY);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = candidate.Copy();
            }
        }
    }
}
