using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    public readonly struct FoliageSpawnPoint
    {
        public FoliageSpawnPoint(Vec3d pos, BlockPos block, string wood)
        {
            Pos = pos;
            Block = block;
            Wood = wood;
        }

        public Vec3d Pos { get; }
        public BlockPos Block { get; }
        public string Wood { get; }
    }

    /// <summary>Picks random positions inside foliage voxels within client view distance.</summary>
    public static class CanopyAmbienceFoliageSpawn
    {
        const int MaxCandidates = 96;
        const int MinSampleAttempts = 16;
        const int MaxScanUpBlocks = 22;

        public static bool TryPickSpawnPoints(
            IBlockAccessor acc,
            int playerX,
            int playerY,
            int playerZ,
            int horizontalRadius,
            int minHeightBlocks,
            int count,
            Random rand,
            out FoliageSpawnPoint[] points)
        {
            points = null;
            if (acc == null || count < 1 || horizontalRadius < 1) return false;

            int scanBaseY = playerY + minHeightBlocks;
            int topY = playerY + MaxScanUpBlocks;
            if (topY >= acc.MapSizeY) topY = acc.MapSizeY - 1;
            if (scanBaseY > topY) return false;

            var candidates = new List<FoliageSpawnPoint>(MaxCandidates);
            var scratch = new BlockPos(0);
            int attempts = Math.Min(MaxCandidates, MinSampleAttempts + horizontalRadius / 4);

            for (int c = 0; c < attempts && candidates.Count < MaxCandidates; c++)
            {
                int x = playerX + rand.Next(-horizontalRadius, horizontalRadius + 1);
                int z = playerZ + rand.Next(-horizontalRadius, horizontalRadius + 1);
                int y = scanBaseY + rand.Next(0, topY - scanBaseY + 1);

                scratch.Set(x, y, z);
                if (!acc.IsValidPos(scratch)) continue;

                Block block = acc.GetBlock(scratch);
                FoliageCellKind kind = CanopyFoliageRules.Classify(block);
                if (kind != FoliageCellKind.BranchyLeaf && kind != FoliageCellKind.RegularLeaf) continue;

                string wood = CanopyBlockHelper.GetWoodFromFoliageBlock(block);
                if (string.IsNullOrEmpty(wood)) continue;

                candidates.Add(new FoliageSpawnPoint(
                    RandomPointInsideBlock(scratch, rand),
                    scratch.Copy(),
                    wood));
            }

            if (candidates.Count == 0) return false;

            points = new FoliageSpawnPoint[count];
            for (int i = 0; i < count; i++)
            {
                points[i] = candidates[rand.Next(candidates.Count)];
            }

            return true;
        }

        static Vec3d RandomPointInsideBlock(BlockPos block, Random rand)
        {
            return new Vec3d(
                block.X + 0.22 + rand.NextDouble() * 0.56,
                block.Y + 0.22 + rand.NextDouble() * 0.56,
                block.Z + 0.22 + rand.NextDouble() * 0.56);
        }
    }
}
