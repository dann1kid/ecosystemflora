using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Phased senescence for registered ferntrees after calendar lifespan.</summary>
    internal static class FerntreeSenescence
    {
        public static bool IsPastHorizon(int ageYears, WildFerntreeEcology.Profile profile, EcosystemConfig cfg)
        {
            if (cfg == null || !cfg.EnableTreeSenescence || !cfg.EnableFerntreeEcology) return false;
            if (profile.SenescenceAgeYears <= 0) return false;
            return ageYears >= profile.SenescenceAgeYears;
        }

        public static bool SuppressesSpread(ReproducerEntry entry, EcosystemConfig cfg)
        {
            if (entry == null || cfg == null || !cfg.EnableTreeSenescence || !cfg.EnableFerntreeEcology) return false;
            if (entry.Requirements?.Habitat != EcologyHabitat.Ferntree) return false;
            if (entry.TreeSenescencePhase != TreeSenescencePhase.None) return true;

            WildFerntreeEcology.Profile profile = WildFerntreeEcology.Resolve();
            return IsPastHorizon(entry.TreeAgeYears, profile, cfg);
        }

        public static TreeSenescence.YearAdvanceResult AdvanceSenescenceYear(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos trunkBase,
            TreeSenescencePhase phase,
            EcosystemConfig cfg)
        {
            if (api == null || acc == null || trunkBase == null || cfg == null) return default;
            if (!LandClaimGuard.AllowsEcologyChange(api, trunkBase)) return default;

            if (!FerntreeStructure.IsTrunkBlock(acc.GetBlock(trunkBase)))
            {
                return default;
            }

            int removed = 0;
            switch (phase)
            {
                case TreeSenescencePhase.None:
                    removed = StripFoliage(acc, trunkBase);
                    return new TreeSenescence.YearAdvanceResult(
                        removed, TreeSenescencePhase.Declining, false, default);

                case TreeSenescencePhase.Declining:
                    removed = RemoveTop(acc, trunkBase);
                    return new TreeSenescence.YearAdvanceResult(
                        removed, TreeSenescencePhase.DeadCrown, false, default);

                case TreeSenescencePhase.DeadCrown:
                    removed = ReduceToSnag(acc, trunkBase, cfg.FerntreeSenescenceSnagSegments);
                    return new TreeSenescence.YearAdvanceResult(
                        removed, TreeSenescencePhase.Snag, false, default);

                case TreeSenescencePhase.Snag:
                    removed = FerntreeStructure.RemoveColumn(acc, trunkBase);
                    if (removed <= 0)
                    {
                        return new TreeSenescence.YearAdvanceResult(0, TreeSenescencePhase.Snag, false, default);
                    }

                    var pending = new TreeSenescence.PendingRemoval(
                        trunkBase.Copy(), WildFerntreeEcology.Species, removed);
                    return new TreeSenescence.YearAdvanceResult(
                        removed, TreeSenescencePhase.None, true, pending);

                default:
                    return default;
            }
        }

        static int StripFoliage(IBlockAccessor acc, BlockPos trunkBase)
        {
            var blocks = new List<BlockPos>(16);
            FerntreeStructure.CollectColumnBlocks(acc, trunkBase, blocks);
            int removed = 0;

            for (int i = 0; i < blocks.Count; i++)
            {
                BlockPos pos = blocks[i];
                Block block = acc.GetBlock(pos);
                if (!FerntreeStructure.IsFoliageBlock(block)) continue;

                acc.SetBlock(0, pos);
                acc.MarkBlockDirty(pos);
                removed++;
            }

            return removed;
        }

        static int RemoveTop(IBlockAccessor acc, BlockPos trunkBase)
        {
            BlockPos topPos = FerntreeStructure.FindTopPos(acc, trunkBase);
            Block top = acc.GetBlock(topPos);
            if (!FerntreeStructure.IsTopBlock(top)) return 0;

            acc.SetBlock(0, topPos);
            acc.MarkBlockDirty(topPos);
            return 1;
        }

        static int ReduceToSnag(IBlockAccessor acc, BlockPos trunkBase, int snagSegments)
        {
            if (snagSegments < 1) snagSegments = 1;

            var keep = new List<BlockPos>(8);
            BlockPos scan = trunkBase.Copy();
            for (int i = 0; i < snagSegments; i++)
            {
                if (!acc.IsValidPos(scan)) break;
                if (!FerntreeStructure.IsTrunkBlock(acc.GetBlock(scan))) break;
                keep.Add(scan.Copy());
                scan.Up();
            }

            var blocks = new List<BlockPos>(32);
            FerntreeStructure.CollectColumnBlocks(acc, trunkBase, blocks);
            int removed = 0;

            for (int i = 0; i < blocks.Count; i++)
            {
                BlockPos pos = blocks[i];
                if (IsKeptSnagSegment(keep, pos)) continue;

                Block block = acc.GetBlock(pos);
                if (block.Id == 0) continue;

                acc.SetBlock(0, pos);
                acc.MarkBlockDirty(pos);
                removed++;
            }

            return removed;
        }

        static bool IsKeptSnagSegment(List<BlockPos> keep, BlockPos pos)
        {
            for (int i = 0; i < keep.Count; i++)
            {
                BlockPos k = keep[i];
                if (k.X == pos.X && k.Y == pos.Y && k.Z == pos.Z && k.dimension == pos.dimension)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
