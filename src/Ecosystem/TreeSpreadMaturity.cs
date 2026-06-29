using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    /// <summary>Whether a registered wild tree may spread offspring (calendar age + structure).</summary>
    internal static class TreeSpreadMaturity
    {
        /// <summary>Size index % at or above which structure alone allows spread (worldgen trees at age 0).</summary>
        internal const int StructuralBypassSizePercent = 40;

        public static bool AllowsSpread(ICoreAPI api, ReproducerEntry entry, EcosystemConfig cfg)
        {
            if (api?.World?.BlockAccessor == null || entry?.Origin == null || cfg == null) return true;

            IBlockAccessor acc = api.World.BlockAccessor;
            Block block = acc.GetBlock(entry.Origin);
            if (!PlantCodeHelper.IsTreeLogGrownBlock(block)) return true;

            string wood = PlantCodeHelper.GetTreeWood(block);
            if (string.IsNullOrEmpty(wood)) return true;

            int maturityYears = ResolveMaturityYears(wood, cfg);
            if (maturityYears <= 0) return true;

            int effectiveAge = EffectiveAgeYears(acc, entry, wood);
            if (effectiveAge >= maturityYears) return true;

            WildTreeGrowthProfiles.Profile profile = WildTreeGrowthProfiles.Resolve(wood);
            TreeStructureMetrics metrics = TreeStructureProbe.Measure(acc, entry.Origin, wood);
            return MeetsStructuralBypass(metrics, profile, cfg);
        }

        public static int EffectiveAgeYears(IBlockAccessor acc, ReproducerEntry entry, string wood)
        {
            if (entry == null) return 0;

            int estimated = TreeRegistrationAge.EstimateFromStructure(acc, entry.Origin, wood);
            return entry.TreeAgeYears > estimated ? entry.TreeAgeYears : estimated;
        }

        public static int ResolveMaturityYears(string wood, EcosystemConfig cfg)
        {
            int maturityYears = WildTreeGrowthProfiles.Resolve(wood).SpreadMaturityAgeYears;
            if (maturityYears <= 0) maturityYears = cfg?.TreeMinSpreadAgeYears ?? 0;
            return maturityYears;
        }

        public static bool MeetsStructuralBypass(
            TreeStructureMetrics metrics,
            in WildTreeGrowthProfiles.Profile profile,
            EcosystemConfig cfg)
        {
            if (cfg?.TreeYoungSpreadBypassTrunkHeight > 0
                && metrics.TrunkHeight >= cfg.TreeYoungSpreadBypassTrunkHeight)
            {
                return true;
            }

            // Log-grown seedlings are a single trunk block; never treat them as worldgen-mature.
            if (metrics.TrunkHeight < 4) return false;

            if (profile.ReferenceTrunkHeight > 0
                && metrics.TrunkHeight >= profile.ReferenceTrunkHeight * 0.55f)
            {
                return true;
            }

            int sizePct = TreeGrowthTargets.SizeIndexPercent(
                metrics.TrunkHeight,
                metrics.CrownRadius,
                profile);
            return sizePct >= StructuralBypassSizePercent;
        }
    }
}
