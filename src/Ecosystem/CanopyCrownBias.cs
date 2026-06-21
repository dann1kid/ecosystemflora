using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Soft crown-direction bias for seasonal strip (periphery first) and spring dress (interior first).</summary>
    internal static class CanopyCrownBias
    {
        internal const float StripPeripheryInfluence = 0.35f;
        internal const float BudInteriorInfluence = 0.35f;
        internal const int MinReferenceHorizRadius = 4;
        internal const int MaxReferenceHorizRadius = 10;

        internal static float StripActivityScaleForPeriphery(float peripheryNorm) =>
            1f + StripPeripheryInfluence * Clamp01(peripheryNorm);

        internal static float BudActivityScaleForPeriphery(float peripheryNorm) =>
            1f + BudInteriorInfluence * (1f - Clamp01(peripheryNorm));

        public static float StripActivityScale(IBlockAccessor acc, BlockPos leafPos, string wood)
        {
            if (!TryGetPeripheryNorm(acc, leafPos, wood, out float peripheryNorm)) return 1f;
            return StripActivityScaleForPeriphery(peripheryNorm);
        }

        public static float BudActivityScale(IBlockAccessor acc, BlockPos budTargetPos, string wood)
        {
            if (!TryGetPeripheryNorm(acc, budTargetPos, wood, out float peripheryNorm)) return 1f;
            return BudActivityScaleForPeriphery(peripheryNorm);
        }

        static bool TryGetPeripheryNorm(IBlockAccessor acc, BlockPos pos, string wood, out float peripheryNorm)
        {
            peripheryNorm = 0f;
            if (!TryFindTrunkBase(acc, pos, wood, out BlockPos trunkBase, out int horizDist)) return false;

            int crownRadius = System.Math.Max(
                MinReferenceHorizRadius,
                TreeStructureProbe.Measure(acc, trunkBase, wood).CrownRadius);
            if (crownRadius > MaxReferenceHorizRadius) crownRadius = MaxReferenceHorizRadius;

            peripheryNorm = Clamp01(horizDist / (float)crownRadius);
            return true;
        }

        static bool TryFindTrunkBase(
            IBlockAccessor acc,
            BlockPos pos,
            string wood,
            out BlockPos trunkBase,
            out int horizDist)
        {
            trunkBase = null;
            horizDist = 0;
            if (acc == null || pos == null || string.IsNullOrEmpty(wood)) return false;

            BlockPos bestTrunk = null;
            int bestDistSq = int.MaxValue;
            var scratch = new BlockPos(0);

            TryConsiderColumn(acc, pos.X, pos.Z, pos.Y, wood, pos, scratch, ref bestTrunk, ref bestDistSq);

            for (int ring = 1; ring <= 5; ring++)
            {
                for (int dx = -ring; dx <= ring; dx++)
                {
                    for (int dz = -ring; dz <= ring; dz++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dz)) != ring) continue;

                        TryConsiderColumn(
                            acc,
                            pos.X + dx,
                            pos.Z + dz,
                            pos.Y,
                            wood,
                            pos,
                            scratch,
                            ref bestTrunk,
                            ref bestDistSq);
                    }
                }
            }

            if (bestTrunk == null) return false;

            trunkBase = bestTrunk;
            horizDist = (int)Math.Round(Math.Sqrt(bestDistSq));
            return true;
        }

        static void TryConsiderColumn(
            IBlockAccessor acc,
            int x,
            int z,
            int startY,
            string wood,
            BlockPos measureFrom,
            BlockPos scratch,
            ref BlockPos bestTrunk,
            ref int bestDistSq)
        {
            if (!TryFindLogInColumn(acc, x, z, startY, wood, scratch, out BlockPos logPos))
            {
                return;
            }

            BlockPos basePos = PlantCodeHelper.GetTreeTrunkBase(acc, logPos);
            int distSq = HorizDistSq(measureFrom, basePos);
            if (distSq >= bestDistSq) return;

            bestDistSq = distSq;
            bestTrunk = basePos;
        }

        static bool TryFindLogInColumn(
            IBlockAccessor acc,
            int x,
            int z,
            int startY,
            string wood,
            BlockPos scratch,
            out BlockPos logPos)
        {
            logPos = null;
            int yMin = Math.Max(0, startY - 28);
            int yMax = Math.Min(acc.MapSizeY - 1, startY + 6);

            for (int y = startY; y >= yMin; y--)
            {
                scratch.Set(x, y, z);
                if (!acc.IsValidPos(scratch)) continue;

                Block block = acc.GetBlock(scratch);
                if (PlantCodeHelper.IsTreeLogGrownBlock(block)
                    && PlantCodeHelper.GetTreeWood(block) == wood)
                {
                    logPos = scratch.Copy();
                    return true;
                }
            }

            for (int y = startY + 1; y <= yMax; y++)
            {
                scratch.Set(x, y, z);
                if (!acc.IsValidPos(scratch)) continue;

                Block block = acc.GetBlock(scratch);
                if (PlantCodeHelper.IsTreeLogGrownBlock(block)
                    && PlantCodeHelper.GetTreeWood(block) == wood)
                {
                    logPos = scratch.Copy();
                    return true;
                }
            }

            return false;
        }

        static int HorizDistSq(BlockPos from, BlockPos trunkBase)
        {
            int dx = from.X - trunkBase.X;
            int dz = from.Z - trunkBase.Z;
            return dx * dx + dz * dz;
        }

        static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }
    }
}
