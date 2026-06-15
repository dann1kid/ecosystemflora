using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Calendar senescence — full removal of wild tree skeleton when age exceeds species horizon.</summary>
    internal static class TreeSenescence
    {
        internal readonly struct PendingRemoval
        {
            public PendingRemoval(BlockPos trunkBase, string wood, int blocksRemoved)
            {
                TrunkBase = trunkBase;
                Wood = wood;
                BlocksRemoved = blocksRemoved;
            }

            public BlockPos TrunkBase { get; }
            public string Wood { get; }
            public int BlocksRemoved { get; }
        }

        static readonly int[] NeighborDx = { 1, -1, 0, 0, 0, 0 };
        static readonly int[] NeighborDy = { 0, 0, 1, -1, 0, 0 };
        static readonly int[] NeighborDz = { 0, 0, 0, 0, 1, -1 };

        public static bool IsSenescent(int ageYears, WildTreeGrowthProfiles.Profile profile, EcosystemConfig cfg)
        {
            if (cfg == null || !cfg.EnableTreeAging || !cfg.EnableTreeSenescence) return false;
            if (profile.SenescenceAgeYears <= 0) return false;
            return ageYears >= profile.SenescenceAgeYears;
        }

        /// <summary>Removes trunk, branchy, and regular foliage blocks. Returns count cleared (0 if blocked).</summary>
        public static int RemoveWholeTree(ICoreAPI api, IBlockAccessor acc, BlockPos trunkBase, string wood)
        {
            if (api == null || acc == null || trunkBase == null || string.IsNullOrEmpty(wood)) return 0;
            if (!LandClaimGuard.AllowsEcologyChange(api, trunkBase)) return 0;

            Block trunkBlock = acc.GetBlock(trunkBase);
            if (!PlantCodeHelper.IsTreeLogGrownBlock(trunkBlock)) return 0;

            var blocks = new List<BlockPos>(128);
            CollectTreeBlocks(acc, trunkBase, wood, blocks);
            if (blocks.Count == 0) return 0;

            MyceliumTreeCascade.OnTreeRemoved(api, trunkBase, trunkBlock);

            blocks.Sort((a, b) => b.Y.CompareTo(a.Y));

            int removed = 0;
            FoliageCellScheduler foliage = EcosystemSystem.Instance?.FoliageCells;

            foreach (BlockPos pos in blocks)
            {
                if (!LandClaimGuard.AllowsEcologyChange(api, pos)) continue;

                Block block = acc.GetBlock(pos);
                if (block == null || block.Id == 0) continue;

                if (foliage != null && CanopyFoliageRules.IsSeasonalFoliageBlock(block))
                {
                    foliage.OnBlockRemoved(pos);
                }

                acc.SetBlock(0, pos);
                acc.MarkBlockDirty(pos);
                removed++;
            }

            return removed;
        }

        public static void CollectTreeBlocks(
            IBlockAccessor acc,
            BlockPos trunkBase,
            string wood,
            List<BlockPos> output)
        {
            output.Clear();
            if (acc == null || trunkBase == null || string.IsNullOrEmpty(wood)) return;

            TreeStructureMetrics metrics = TreeStructureProbe.Measure(acc, trunkBase, wood);
            int trunkX = trunkBase.X;
            int trunkZ = trunkBase.Z;
            int maxY = System.Math.Min(metrics.TrunkTop.Y + 8, acc.MapSizeY - 1);
            int maxHorizSq = TreeStructureProbe.MaxCrownScanRadius * TreeStructureProbe.MaxCrownScanRadius;

            var visited = new HashSet<BlockPos>();
            var queue = new Queue<BlockPos>();
            var scratch = new BlockPos(trunkBase.dimension);

            for (int y = trunkBase.Y; y <= metrics.TrunkTop.Y; y++)
            {
                scratch.Set(trunkX, y, trunkZ);
                if (!acc.IsValidPos(scratch)) continue;
                if (!IsConnectedTreeBlock(acc.GetBlock(scratch), wood)) continue;

                EnqueueTreeBlock(scratch, visited, queue, output);
            }

            while (queue.Count > 0)
            {
                BlockPos cur = queue.Dequeue();
                for (int i = 0; i < 6; i++)
                {
                    scratch.Set(cur.X + NeighborDx[i], cur.Y + NeighborDy[i], cur.Z + NeighborDz[i]);
                    if (!acc.IsValidPos(scratch)) continue;
                    if (scratch.Y < trunkBase.Y || scratch.Y > maxY) continue;

                    int dx = scratch.X - trunkX;
                    int dz = scratch.Z - trunkZ;
                    if (dx * dx + dz * dz > maxHorizSq) continue;
                    if (visited.Contains(scratch)) continue;

                    Block block = acc.GetBlock(scratch);
                    if (!IsConnectedTreeBlock(block, wood)) continue;

                    EnqueueTreeBlock(scratch, visited, queue, output);
                }
            }
        }

        static bool IsConnectedTreeBlock(Block block, string wood)
        {
            if (block?.Code == null || string.IsNullOrEmpty(wood)) return false;

            if (PlantCodeHelper.IsTreeLogGrownBlock(block))
            {
                return string.Equals(PlantCodeHelper.GetTreeWood(block), wood, System.StringComparison.OrdinalIgnoreCase);
            }

            if (CanopyBlockHelper.IsBranchyLeaf(block) || CanopyBlockHelper.IsRegularLeaf(block))
            {
                return string.Equals(CanopyBlockHelper.GetWoodFromFoliageBlock(block), wood, System.StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        static void EnqueueTreeBlock(
            BlockPos scratch,
            HashSet<BlockPos> visited,
            Queue<BlockPos> queue,
            List<BlockPos> output)
        {
            BlockPos copy = scratch.Copy();
            if (!visited.Add(copy)) return;

            queue.Enqueue(copy);
            output.Add(copy);
        }
    }
}
