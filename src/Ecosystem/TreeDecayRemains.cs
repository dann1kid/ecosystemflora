using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Vanilla log blocks left when senescent snag collapses (not log-grown — no re-registration).</summary>
    internal static class TreeDecayRemains
    {
        /// <summary>Vanilla pillar rotations for horizontal debarked logs (ns / we).</summary>
        static readonly string[] HorizontalVariants = { "ns", "we" };

        /// <summary>Replace snag with stump + scattered ground logs; returns blocks changed.</summary>
        public static int CollapseSnagToRemains(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos trunkBase,
            string wood,
            EcosystemConfig cfg)
        {
            if (api == null || acc == null || trunkBase == null || string.IsNullOrEmpty(wood) || cfg == null)
            {
                return 0;
            }

            if (!LandClaimGuard.AllowsEcologyChange(api, trunkBase))
            {
                return 0;
            }

            var blocks = new List<BlockPos>(32);
            TreeSenescence.CollectTreeBlocks(acc, trunkBase, wood, blocks);
            if (blocks.Count == 0) return 0;

            Block trunkBlock = acc.GetBlock(trunkBase);
            MyceliumTreeCascade.OnTreeRemoved(api, trunkBase, trunkBlock);

            Block stumpBlock = ResolveStumpBlock(acc, wood);
            int changed = 0;
            FoliageCellScheduler foliage = EcosystemSystem.Instance?.FoliageCells;

            blocks.Sort((a, b) => b.Y.CompareTo(a.Y));

            foreach (BlockPos pos in blocks)
            {
                if (!LandClaimGuard.AllowsEcologyChange(api, pos)) continue;

                Block block = acc.GetBlock(pos);
                if (block == null || block.Id == 0) continue;
                if (!PlantCodeHelper.IsTreeLogGrownBlock(block)) continue;
                if (PlantCodeHelper.GetTreeWood(block) != wood) continue;

                if (pos.Equals(trunkBase) && stumpBlock != null)
                {
                    if (foliage != null && CanopyFoliageRules.IsSeasonalFoliageBlock(block))
                    {
                        foliage.OnBlockRemoved(pos);
                    }

                    acc.SetBlock(stumpBlock.BlockId, pos);
                    acc.MarkBlockDirty(pos);
                    if (cfg.EnableStumpDecay)
                    {
                        EcosystemSystem.Instance?.StumpDecay.Enqueue(api, pos, wood);
                    }

                    changed++;
                    continue;
                }

                if (TreeSenescenceRemoveBlock(acc, pos, block, foliage))
                {
                    changed++;
                }
            }

            changed += ScatterFallenLogs(api, acc, trunkBase, wood, cfg, foliage);
            return changed;
        }

        public static int ScatterFallenLogs(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos trunkBase,
            string wood,
            EcosystemConfig cfg,
            FoliageCellScheduler foliage = null)
        {
            if (cfg.TreeSenescenceFallenLogCount <= 0) return 0;

            var candidates = new List<BlockPos>(16);
            BuildScatterCandidates(trunkBase, candidates);
            ShuffleCandidates(trunkBase, wood, candidates);

            int placed = 0;
            int variant = 0;

            foreach (BlockPos target in candidates)
            {
                if (placed >= cfg.TreeSenescenceFallenLogCount) break;
                if (!LandClaimGuard.AllowsEcologyChange(api, target)) continue;
                if (!CanPlaceFallenLog(acc, target)) continue;

                Block fallen = ResolveFallenLogBlock(acc, wood, variant++);
                if (fallen == null || fallen.Id == 0) continue;

                acc.SetBlock(fallen.BlockId, target);
                acc.MarkBlockDirty(target);
                placed++;
            }

            return placed;
        }

        internal static void BuildScatterCandidates(BlockPos trunkBase, List<BlockPos> output)
        {
            output.Clear();
            if (trunkBase == null) return;

            int y = trunkBase.Y;
            int dim = trunkBase.dimension;

            for (int ring = 1; ring <= 2; ring++)
            {
                for (int dx = -ring; dx <= ring; dx++)
                {
                    for (int dz = -ring; dz <= ring; dz++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        if (System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dz)) != ring) continue;

                        output.Add(new BlockPos(trunkBase.X + dx, y, trunkBase.Z + dz, dim));
                    }
                }
            }
        }

        internal static void ShuffleCandidates(BlockPos trunkBase, string wood, List<BlockPos> candidates)
        {
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = PositiveMod(ScatterHash(trunkBase, wood, i), i + 1);
                BlockPos tmp = candidates[i];
                candidates[i] = candidates[j];
                candidates[j] = tmp;
            }
        }

        static bool CanPlaceFallenLog(IBlockAccessor acc, BlockPos pos)
        {
            if (!acc.IsValidPos(pos)) return false;

            Block at = acc.GetBlock(pos);
            if (at == null || at.Id != 0) return false;

            BlockPos below = pos.DownCopy();
            if (!acc.IsValidPos(below)) return false;

            Block under = acc.GetBlock(below);
            if (under == null || under.Id == 0) return false;

            return under.SideSolid[BlockFacing.UP.Index];
        }

        public static Block ResolveStumpBlock(IBlockAccessor acc, string wood)
        {
            if (acc == null || string.IsNullOrEmpty(wood)) return null;

            Block block = acc.GetBlock(new AssetLocation("game:log-" + wood + "-ud"));
            if (block != null && block.Id != 0) return block;

            block = acc.GetBlock(new AssetLocation("game:debarkedlog-" + wood + "-ud"));
            return block != null && block.Id != 0 ? block : null;
        }

        public static Block ResolveFallenLogBlock(IBlockAccessor acc, string wood, int variantIndex)
        {
            if (acc == null || string.IsNullOrEmpty(wood)) return null;

            string rot = HorizontalVariants[PositiveMod(variantIndex, HorizontalVariants.Length)];

            Block block = acc.GetBlock(new AssetLocation("game:debarkedlog-rotten-" + rot));
            if (block != null && block.Id != 0) return block;

            block = acc.GetBlock(new AssetLocation("game:debarkedlog-" + wood + "-" + rot));
            if (block != null && block.Id != 0) return block;

            block = acc.GetBlock(new AssetLocation("game:log-" + wood + "-" + rot));
            if (block != null && block.Id != 0) return block;

            return ResolveStumpBlock(acc, wood);
        }

        static int ScatterHash(BlockPos basePos, string wood, int slot)
        {
            unchecked
            {
                // NOTE: do not use string.GetHashCode() (randomized between processes).
                // We want a stable shuffle for save/load parity and unit tests.
                uint h = 2166136261u;
                h = (h ^ (uint)basePos.X) * 16777619u;
                h = (h ^ (uint)basePos.Y) * 16777619u;
                h = (h ^ (uint)basePos.Z) * 16777619u;
                h = (h ^ (uint)slot) * 16777619u;
                if (wood != null)
                {
                    for (int i = 0; i < wood.Length; i++)
                    {
                        h = (h ^ wood[i]) * 16777619u;
                    }
                }

                return (int)h;
            }
        }

        static int PositiveMod(int value, int modulus)
        {
            if (modulus <= 0) return 0;
            int r = value % modulus;
            return r < 0 ? r + modulus : r;
        }

        static bool TreeSenescenceRemoveBlock(
            IBlockAccessor acc,
            BlockPos pos,
            Block block,
            FoliageCellScheduler foliage)
        {
            if (foliage != null && CanopyFoliageRules.IsSeasonalFoliageBlock(block))
            {
                foliage.OnBlockRemoved(pos);
            }

            acc.SetBlock(0, pos);
            acc.MarkBlockDirty(pos);
            EcosystemSystem.Instance?.NotifyWildVineHostChanged(pos);
            return true;
        }
    }
}
