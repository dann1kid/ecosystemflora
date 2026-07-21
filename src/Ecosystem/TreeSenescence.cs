using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Phased calendar senescence after species lifespan.</summary>
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

        internal readonly struct YearAdvanceResult
        {
            public YearAdvanceResult(
                int blocksRemoved,
                TreeSenescencePhase newPhase,
                bool completed,
                PendingRemoval removal)
            {
                BlocksRemoved = blocksRemoved;
                NewPhase = newPhase;
                Completed = completed;
                Removal = removal;
            }

            public int BlocksRemoved { get; }
            public TreeSenescencePhase NewPhase { get; }
            public bool Completed { get; }
            public PendingRemoval Removal { get; }
        }

        static readonly int[] NeighborDx = { 1, -1, 0, 0, 0, 0 };
        static readonly int[] NeighborDy = { 0, 0, 1, -1, 0, 0 };
        static readonly int[] NeighborDz = { 0, 0, 0, 0, 1, -1 };

        public static bool IsPastHorizon(
            int ageYears,
            WildTreeGrowthProfiles.Profile profile,
            EcosystemConfig cfg,
            int lifespanDebtYears = 0)
        {
            if (cfg == null || !cfg.EnableTreeAging || !cfg.EnableTreeSenescence) return false;
            if (profile.SenescenceAgeYears <= 0) return false;
            int horizon = TreeNicheLifespanStress.EffectiveHorizon(
                profile.SenescenceAgeYears,
                cfg.EnableTreeNicheLifespanStress ? lifespanDebtYears : 0,
                cfg);
            return ageYears >= horizon;
        }

        public static bool IsPastHorizon(
            ReproducerEntry entry,
            WildTreeGrowthProfiles.Profile profile,
            EcosystemConfig cfg)
        {
            int debt = entry?.TreeLifespanDebtYears ?? 0;
            return IsPastHorizon(entry?.TreeAgeYears ?? 0, profile, cfg, debt);
        }

        /// <summary>Legacy name — age at or past species lifespan.</summary>
        public static bool IsSenescent(
            int ageYears,
            WildTreeGrowthProfiles.Profile profile,
            EcosystemConfig cfg) =>
            IsPastHorizon(ageYears, profile, cfg);

        public static bool SuppressesSpread(ReproducerEntry entry, EcosystemConfig cfg)
        {
            if (entry == null || cfg == null || !cfg.EnableTreeSenescence) return false;
            if (entry.Requirements?.Habitat == EcologyHabitat.Ferntree)
            {
                return FerntreeSenescence.SuppressesSpread(entry, cfg);
            }

            if (entry.TreeSenescencePhase != TreeSenescencePhase.None) return true;

            string wood = entry.Requirements?.Species;
            if (string.IsNullOrEmpty(wood)) return false;

            WildTreeGrowthProfiles.Profile profile = WildTreeGrowthProfiles.Resolve(wood);
            return IsPastHorizon(entry, profile, cfg);
        }

        public static bool BlocksSeasonalCanopy(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos pos,
            string wood)
        {
            if (api == null || acc == null || pos == null || string.IsNullOrEmpty(wood)) return false;
            if (!EcosystemConfig.Loaded.EnableTreeSenescence) return false;

            if (!TryResolveTrunkBaseForTreeBlock(acc, pos, wood, out BlockPos trunkBase)) return false;
            if (EcosystemSystem.Instance?.TryGetReproducer(trunkBase, out ReproducerEntry entry) != true) return false;

            if (entry.TreeSenescencePhase != TreeSenescencePhase.None) return true;

            WildTreeGrowthProfiles.Profile profile = WildTreeGrowthProfiles.Resolve(wood);
            return IsPastHorizon(entry, profile, EcosystemConfig.Loaded);
        }

        /// <summary>One senescence stage per game year after lifespan.</summary>
        public static YearAdvanceResult AdvanceSenescenceYear(
            ICoreAPI api,
            IBlockAccessor acc,
            ReproducerEntry entry,
            BlockPos trunkBase,
            string wood,
            EcosystemConfig cfg)
        {
            if (api == null || acc == null || entry == null || trunkBase == null
                || string.IsNullOrEmpty(wood) || cfg == null || !cfg.EnableTreeSenescence)
            {
                return default;
            }

            if (!LandClaimGuard.AllowsEcologyChange(api, trunkBase))
            {
                return new YearAdvanceResult(0, entry.TreeSenescencePhase, false, default);
            }

            Block trunkBlock = acc.GetBlock(trunkBase);
            if (!PlantCodeHelper.IsTreeLogGrownBlock(trunkBlock)) return default;

            TreeSenescencePhase phase = entry.TreeSenescencePhase;
            int removed;

            switch (phase)
            {
                case TreeSenescencePhase.None:
                    removed = StripFoliageKind(api, acc, trunkBase, wood, regularLeaves: true, branchy: false);
                    return new YearAdvanceResult(removed, TreeSenescencePhase.Declining, false, default);

                case TreeSenescencePhase.Declining:
                    removed = StripFoliageKind(api, acc, trunkBase, wood, regularLeaves: false, branchy: true);
                    return new YearAdvanceResult(removed, TreeSenescencePhase.DeadCrown, false, default);

                case TreeSenescencePhase.DeadCrown:
                    removed = ReduceToSnag(api, acc, trunkBase, wood, cfg.TreeSenescenceSnagBlocks);
                    return new YearAdvanceResult(removed, TreeSenescencePhase.Snag, false, default);

                case TreeSenescencePhase.Snag:
                    removed = cfg.EnableTreeSenescenceRemains
                        ? TreeDecayRemains.CollapseSnagToRemains(api, acc, trunkBase, wood, cfg)
                        : RemoveRemainingTrunk(api, acc, trunkBase, wood);
                    if (removed <= 0)
                    {
                        return new YearAdvanceResult(0, TreeSenescencePhase.Snag, false, default);
                    }

                    var pending = new PendingRemoval(trunkBase.Copy(), wood, removed);
                    return new YearAdvanceResult(removed, TreeSenescencePhase.None, true, pending);

                default:
                    return default;
            }
        }

        public static int StripFoliageKind(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos trunkBase,
            string wood,
            bool regularLeaves,
            bool branchy)
        {
            var blocks = new List<BlockPos>(128);
            CollectTreeBlocks(acc, trunkBase, wood, blocks);
            if (blocks.Count == 0) return 0;

            blocks.Sort((a, b) => b.Y.CompareTo(a.Y));
            return RemoveMatchingBlocks(api, acc, blocks, wood, regularLeaves, branchy, trunk: false);
        }

        public static int ReduceToSnag(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos trunkBase,
            string wood,
            int snagBlocks)
        {
            if (snagBlocks < 1) snagBlocks = 1;

            var blocks = new List<BlockPos>(128);
            CollectTreeBlocks(acc, trunkBase, wood, blocks);
            if (blocks.Count == 0) return 0;

            int trunkX = trunkBase.X;
            int trunkZ = trunkBase.Z;
            int keepMaxY = trunkBase.Y + snagBlocks - 1;

            blocks.Sort((a, b) => b.Y.CompareTo(a.Y));
            int removed = 0;

            foreach (BlockPos pos in blocks)
            {
                if (!LandClaimGuard.AllowsEcologyChange(api, pos)) continue;

                Block block = acc.GetBlock(pos);
                if (block == null || block.Id == 0) continue;

                bool isTrunkColumn = pos.X == trunkX && pos.Z == trunkZ
                    && PlantCodeHelper.IsTreeLogGrownBlock(block);

                if (isTrunkColumn && pos.Y <= keepMaxY) continue;

                if (RemoveTreeBlock(api, acc, pos, block)) removed++;
            }

            return removed;
        }

        public static int RemoveRemainingTrunk(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos trunkBase,
            string wood)
        {
            var blocks = new List<BlockPos>(32);
            CollectTreeBlocks(acc, trunkBase, wood, blocks);
            if (blocks.Count == 0) return 0;

            MyceliumTreeCascade.OnTreeRemoved(api, trunkBase, acc.GetBlock(trunkBase));

            blocks.Sort((a, b) => b.Y.CompareTo(a.Y));
            return RemoveMatchingBlocks(api, acc, blocks, wood, regularLeaves: true, branchy: true, trunk: true);
        }

        static int RemoveMatchingBlocks(
            ICoreAPI api,
            IBlockAccessor acc,
            List<BlockPos> blocks,
            string wood,
            bool regularLeaves,
            bool branchy,
            bool trunk)
        {
            int removed = 0;
            FoliageCellScheduler foliage = EcosystemSystem.Instance?.FoliageCells;

            foreach (BlockPos pos in blocks)
            {
                if (!LandClaimGuard.AllowsEcologyChange(api, pos)) continue;

                Block block = acc.GetBlock(pos);
                if (block == null || block.Id == 0) continue;
                if (!ShouldRemoveBlock(block, wood, regularLeaves, branchy, trunk)) continue;

                if (RemoveTreeBlock(api, acc, pos, block, foliage)) removed++;
            }

            return removed;
        }

        static bool ShouldRemoveBlock(
            Block block,
            string wood,
            bool regularLeaves,
            bool branchy,
            bool trunk)
        {
            if (regularLeaves && CanopyBlockHelper.IsRegularLeaf(block)
                && CanopyBlockHelper.GetWoodFromFoliageBlock(block) == wood)
            {
                return true;
            }

            if (branchy && CanopyBlockHelper.IsBranchyLeaf(block)
                && CanopyBlockHelper.GetWoodFromFoliageBlock(block) == wood)
            {
                return true;
            }

            if (trunk && PlantCodeHelper.IsTreeLogGrownBlock(block)
                && PlantCodeHelper.GetTreeWood(block) == wood)
            {
                return true;
            }

            return false;
        }

        static bool RemoveTreeBlock(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos pos,
            Block block,
            FoliageCellScheduler foliage = null)
        {
            foliage ??= EcosystemSystem.Instance?.FoliageCells;

            if (foliage != null && CanopyFoliageRules.IsSeasonalFoliageBlock(block))
            {
                foliage.OnBlockRemoved(pos);
            }

            acc.SetBlock(0, pos);
            acc.MarkBlockDirty(pos);
            EcosystemSystem.Instance?.NotifyWildVineHostChanged(pos);
            return true;
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

        internal static bool TryResolveTrunkBaseForTreeBlock(
            IBlockAccessor acc,
            BlockPos pos,
            string wood,
            out BlockPos trunkBase)
        {
            trunkBase = null;
            if (acc == null || pos == null || string.IsNullOrEmpty(wood)) return false;

            Block block = acc.GetBlock(pos);
            if (PlantCodeHelper.IsTreeLogGrownBlock(block)
                && PlantCodeHelper.GetTreeWood(block) == wood)
            {
                trunkBase = PlantCodeHelper.GetTreeTrunkBase(acc, pos);
                return true;
            }

            var scratch = new BlockPos(pos.dimension);
            for (int i = 0; i < 6; i++)
            {
                scratch.Set(pos.X + NeighborDx[i], pos.Y + NeighborDy[i], pos.Z + NeighborDz[i]);
                if (!acc.IsValidPos(scratch)) continue;

                Block neighbor = acc.GetBlock(scratch);
                if (!PlantCodeHelper.IsTreeLogGrownBlock(neighbor)) continue;
                if (PlantCodeHelper.GetTreeWood(neighbor) != wood) continue;

                trunkBase = PlantCodeHelper.GetTreeTrunkBase(acc, scratch);
                return true;
            }

            return false;
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
