using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal readonly struct TreeStructureMetrics
    {
        public TreeStructureMetrics(int trunkHeight, int crownRadius, BlockPos trunkTop)
        {
            TrunkHeight = trunkHeight;
            CrownRadius = crownRadius;
            TrunkTop = trunkTop;
        }

        public int TrunkHeight { get; }
        public int CrownRadius { get; }
        public BlockPos TrunkTop { get; }
    }

    /// <summary>Measures wild tree skeleton extent around a registered trunk base.</summary>
    internal static class TreeStructureProbe
    {
        const int MaxScanUp = 48;
        const int MaxScanRadius = 14;

        public static TreeStructureMetrics Measure(IBlockAccessor acc, BlockPos trunkBase, string wood)
        {
            if (acc == null || trunkBase == null || string.IsNullOrEmpty(wood))
            {
                return new TreeStructureMetrics(1, 1, trunkBase?.Copy() ?? new BlockPos(0));
            }

            BlockPos trunkTop = FindTrunkTop(acc, trunkBase, wood);
            int trunkHeight = trunkTop.Y - trunkBase.Y + 1;
            int crownRadius = MeasureCrownRadius(acc, trunkBase, wood, trunkTop.Y);

            return new TreeStructureMetrics(trunkHeight, crownRadius, trunkTop);
        }

        static BlockPos FindTrunkTop(IBlockAccessor acc, BlockPos trunkBase, string wood)
        {
            var top = trunkBase.Copy();
            var scratch = new BlockPos(trunkBase.X, trunkBase.Y, trunkBase.Z);

            for (int y = trunkBase.Y + 1; y < acc.MapSizeY && y <= trunkBase.Y + MaxScanUp; y++)
            {
                scratch.Set(trunkBase.X, y, trunkBase.Z);
                if (!acc.IsValidPos(scratch)) break;

                Block block = acc.GetBlock(scratch);
                if (!PlantCodeHelper.IsTreeLogGrownBlock(block)) break;
                if (PlantCodeHelper.GetTreeWood(block) != wood) break;

                top.Set(scratch);
            }

            return top;
        }

        static int MeasureCrownRadius(IBlockAccessor acc, BlockPos trunkBase, string wood, int trunkTopY)
        {
            int crownStartY = trunkBase.Y + System.Math.Max(2, (trunkTopY - trunkBase.Y) / 3);
            int maxY = System.Math.Min(trunkTopY + 6, acc.MapSizeY - 1);
            int trunkX = trunkBase.X;
            int trunkZ = trunkBase.Z;

            var visited = new HashSet<long>();
            var queue = new Queue<BlockPos>();
            var scratch = new BlockPos(0);

            for (int y = trunkBase.Y; y <= trunkTopY; y++)
            {
                scratch.Set(trunkX, y, trunkZ);
                if (!acc.IsValidPos(scratch)) continue;

                Block block = acc.GetBlock(scratch);
                if (!CanopyBlockHelper.IsSkeletonBlock(block, wood)) continue;

                long key = PackPos(scratch);
                if (!visited.Add(key)) continue;
                queue.Enqueue(scratch.Copy());
            }

            while (queue.Count > 0)
            {
                BlockPos cur = queue.Dequeue();
                for (int i = 0; i < 6; i++)
                {
                    scratch.Set(cur.X + NeighborDx[i], cur.Y + NeighborDy[i], cur.Z + NeighborDz[i]);
                    if (!acc.IsValidPos(scratch)) continue;

                    int dx = scratch.X - trunkX;
                    int dz = scratch.Z - trunkZ;
                    if (dx * dx + dz * dz > MaxScanRadius * MaxScanRadius) continue;

                    if (scratch.Y < crownStartY
                        && (scratch.X != trunkX || scratch.Z != trunkZ))
                    {
                        continue;
                    }

                    if (scratch.Y > maxY) continue;

                    long key = PackPos(scratch);
                    if (visited.Contains(key)) continue;

                    Block block = acc.GetBlock(scratch);
                    if (!CanopyBlockHelper.IsSkeletonBlock(block, wood)) continue;

                    visited.Add(key);
                    queue.Enqueue(scratch.Copy());
                }
            }

            int maxHorizSq = 0;
            foreach (long packed in visited)
            {
                UnpackPos(packed, scratch);
                if (scratch.Y < crownStartY) continue;

                int dx = scratch.X - trunkX;
                int dz = scratch.Z - trunkZ;
                int horizSq = dx * dx + dz * dz;
                if (horizSq > maxHorizSq) maxHorizSq = horizSq;
            }

            return maxHorizSq <= 0 ? 0 : (int)GameMath.Sqrt(maxHorizSq);
        }

        static readonly int[] NeighborDx = { 1, -1, 0, 0, 0, 0 };
        static readonly int[] NeighborDy = { 0, 0, 1, -1, 0, 0 };
        static readonly int[] NeighborDz = { 0, 0, 0, 0, 1, -1 };

        static long PackPos(BlockPos pos) =>
            ((long)pos.X << 24) | ((long)(pos.Y & 0xFFF) << 12) | (long)(pos.Z & 0xFFF);

        static void UnpackPos(long packed, BlockPos into)
        {
            into.X = (int)(packed >> 24);
            into.Y = (int)((packed >> 12) & 0xFFF);
            into.Z = (int)(packed & 0xFFF);
        }
    }
}
