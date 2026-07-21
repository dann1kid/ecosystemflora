using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Structure→age heuristics for spread maturity only. Calendar <see cref="ReproducerEntry.TreeAgeYears"/>
    /// on first register stays 0 (see docs/TREE_AGING.md) — size must not invent a lifespan.
    /// </summary>
    internal static class TreeRegistrationAge
    {
        /// <summary>Estimate used by spread gates — not written into TreeAgeYears on register.</summary>
        public static int EstimateFromStructure(IBlockAccessor acc, BlockPos trunkBase, string wood)
        {
            if (acc == null || trunkBase == null || string.IsNullOrEmpty(wood)) return 0;

            TreeStructureMetrics metrics = TreeStructureProbe.Measure(acc, trunkBase, wood);
            WildTreeGrowthProfiles.Profile profile = WildTreeGrowthProfiles.Resolve(wood);
            int sizePct = TreeGrowthTargets.SizeIndexPercent(
                metrics.TrunkHeight,
                metrics.CrownRadius,
                profile);

            if (sizePct <= 5) return 0;

            if (profile.SpreadMaturityAgeYears > 0)
            {
                float t = (sizePct - 5f) / 75f;
                t = GameMath.Clamp(t, 0f, 1f);
                return (int)System.Math.Round(t * profile.SpreadMaturityAgeYears);
            }

            return System.Math.Max(0, sizePct / 8);
        }

        /// <summary>
        /// Stale calendar-age save at a trunk base (previous tree) must not kill a new seedling
        /// that just grew into that column.
        /// </summary>
        public static bool ShouldRejectRestoredAge(
            IBlockAccessor acc,
            BlockPos trunkBase,
            string wood,
            ReproducerEntry entry)
        {
            if (acc == null || trunkBase == null || entry == null || string.IsNullOrEmpty(wood))
            {
                return true;
            }

            // Mid-senescence reload is intentional continuity.
            if (entry.TreeSenescencePhase != TreeSenescencePhase.None) return false;

            TreeStructureMetrics metrics = TreeStructureProbe.Measure(acc, trunkBase, wood);
            int age = entry.TreeAgeYears < 0 ? 0 : entry.TreeAgeYears;
            if (age <= 0) return false;

            // Fresh seedlings / young sapling trunks cannot carry decades of calendar age.
            if (metrics.TrunkHeight <= 3)
            {
                int maturity = WildTreeGrowthProfiles.Resolve(wood).SpreadMaturityAgeYears;
                if (maturity <= 0) maturity = 8;
                if (age >= System.Math.Min(maturity, 8)) return true;

                WildTreeGrowthProfiles.Profile profile = WildTreeGrowthProfiles.Resolve(wood);
                if (TreeSenescence.IsPastHorizon(entry, profile, EcosystemConfig.Loaded)) return true;
            }

            return false;
        }
    }
}
