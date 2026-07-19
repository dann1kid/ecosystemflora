using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Removes wild foliage with no BFS path to a supporting log-grown trunk.</summary>
    internal static class CanopyOrphanPrune
    {
        internal const int DefaultMaxBfsDepth = 14;
        internal const int DefaultMaxBfsNodes = 192;
        internal const int DefaultMaxChecksPerChunkPass = 64;

        static readonly int[] NeighborDx = { 1, -1, 0, 0, 0, 0 };
        static readonly int[] NeighborDy = { 0, 0, 1, -1, 0, 0 };
        static readonly int[] NeighborDz = { 0, 0, 0, 0, 1, -1 };

        [System.ThreadStatic]
        static Queue<(int x, int y, int z, int depth)> bfsQueue;

        [System.ThreadStatic]
        static HashSet<long> bfsSeen;

        public static bool IsWildPrunableLeaf(Block block)
        {
            if (block?.Code?.Path == null) return false;

            string path = block.Code.Path;
            if (path.Contains("placed")) return false;

            FoliageCellKind kind = CanopyFoliageRules.Classify(block);
            return kind == FoliageCellKind.BranchyLeaf || kind == FoliageCellKind.RegularLeaf;
        }

        public static bool IsOrphan(
            IBlockAccessor acc,
            BlockPos pos,
            Block block,
            int maxDepth = DefaultMaxBfsDepth,
            int maxNodes = DefaultMaxBfsNodes)
        {
            if (acc == null || pos == null || block == null || !IsWildPrunableLeaf(block)) return false;

            string wood = CanopyBlockHelper.GetWoodFromFoliageBlock(block);
            if (string.IsNullOrEmpty(wood) || !CanopyBlockHelper.IsDeciduousTreeWood(wood)) return false;

            return !HasConnectedLogGrown(acc, pos, wood, maxDepth, maxNodes);
        }

        public static bool TryPruneIfOrphan(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos pos,
            Block block,
            FoliageCellIndex index,
            int maxDepth = DefaultMaxBfsDepth,
            int maxNodes = DefaultMaxBfsNodes)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EnableOrphanFoliagePrune || api == null || acc == null || pos == null || block == null)
            {
                return false;
            }

            if (!LandClaimGuard.AllowsEcologyChange(api, pos)) return false;
            if (!IsOrphan(acc, pos, block, maxDepth, maxNodes)) return false;

            return CanopyFoliageRules.TryStripForced(api, acc, pos, index);
        }

        internal static bool HasConnectedLogGrown(
            IBlockAccessor acc,
            BlockPos start,
            string wood,
            int maxDepth,
            int maxNodes)
        {
            if (acc == null || start == null || string.IsNullOrEmpty(wood) || maxDepth <= 0) return false;

            var scratch = new BlockPos(0);
            for (int i = 0; i < 6; i++)
            {
                scratch.Set(
                    start.X + NeighborDx[i],
                    start.Y + NeighborDy[i],
                    start.Z + NeighborDz[i]);
                if (!acc.IsValidPos(scratch)) continue;
                if (IsMatchingLogGrown(acc.GetBlock(scratch), wood)) return true;
            }

            Queue<(int x, int y, int z, int depth)> queue = bfsQueue;
            HashSet<long> seen = bfsSeen;
            if (queue == null)
            {
                queue = new Queue<(int, int, int, int)>(32);
                bfsQueue = queue;
            }
            else
            {
                queue.Clear();
            }

            if (seen == null)
            {
                seen = new HashSet<long>(64);
                bfsSeen = seen;
            }
            else
            {
                seen.Clear();
            }

            queue.Enqueue((start.X, start.Y, start.Z, 0));
            seen.Add(PackCell(start.X, start.Y, start.Z));

            while (queue.Count > 0 && seen.Count <= maxNodes)
            {
                (int x, int y, int z, int depth) = queue.Dequeue();
                if (depth >= maxDepth) continue;

                for (int i = 0; i < 6; i++)
                {
                    int nx = x + NeighborDx[i];
                    int ny = y + NeighborDy[i];
                    int nz = z + NeighborDz[i];
                    scratch.Set(nx, ny, nz);
                    if (!acc.IsValidPos(scratch)) continue;

                    long key = PackCell(nx, ny, nz);
                    if (!seen.Add(key)) continue;

                    Block neighbor = acc.GetBlock(scratch);
                    if (IsMatchingLogGrown(neighbor, wood)) return true;
                    if (!IsMatchingFoliage(neighbor, wood)) continue;

                    queue.Enqueue((nx, ny, nz, depth + 1));
                }
            }

            return false;
        }

        static bool IsMatchingLogGrown(Block block, string wood) =>
            PlantCodeHelper.IsTreeLogGrownBlock(block) && PlantCodeHelper.GetTreeWood(block) == wood;

        static bool IsMatchingFoliage(Block block, string wood)
        {
            if (block == null || block.Id == 0) return false;
            if (!IsWildPrunableLeaf(block)) return false;
            return CanopyBlockHelper.GetWoodFromFoliageBlock(block) == wood;
        }

        static long PackCell(int x, int y, int z) =>
            ((long)(x & 0x3FFFFF) << 42) | ((long)(y & 0xFFF) << 30) | (long)(z & 0x3FFFFF);
    }
}
