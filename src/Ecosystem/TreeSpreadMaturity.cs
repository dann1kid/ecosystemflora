using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

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

            WildTreeGrowthProfiles.Profile profile = WildTreeGrowthProfiles.Resolve(wood);
            TreeStructureMetrics metrics = TreeStructureProbe.Measure(acc, entry.Origin, wood);
            int estimated = TreeRegistrationAge.EstimateFromStructure(acc, entry.Origin, wood);

            return AllowsSpread(
                entry.TreeAgeYears,
                entry.TreeStructuralSpreadBypass,
                estimated,
                metrics,
                profile,
                cfg,
                maturityYears);
        }

        /// <summary>
        /// Calendar maturity is required for ecology seedlings. Soft size bypass applies only when the
        /// tree was already worldgen-sized at registration (<see cref="ReproducerEntry.TreeStructuralSpreadBypass"/>),
        /// or when structure alone estimates full maturity age.
        /// </summary>
        public static bool AllowsSpread(
            int treeAgeYears,
            bool structuralBypassEligible,
            int estimatedAgeYears,
            in TreeStructureMetrics metrics,
            in WildTreeGrowthProfiles.Profile profile,
            EcosystemConfig cfg,
            int maturityYears)
        {
            if (maturityYears <= 0) return true;

            int calendarAge = treeAgeYears < 0 ? 0 : treeAgeYears;
            if (calendarAge >= maturityYears) return true;

            if (estimatedAgeYears >= maturityYears) return true;

            return structuralBypassEligible
                && MeetsStructuralBypass(metrics, profile, cfg);
        }

        /// <summary>
        /// Years until calendar spread maturity (0 when already eligible by age). Does not account for
        /// structural bypass — callers that need the live gate should use <see cref="AllowsSpread"/>.
        /// </summary>
        public static int YearsUntilCalendarMaturity(int treeAgeYears, int maturityYears)
        {
            if (maturityYears <= 0) return 0;
            int calendarAge = treeAgeYears < 0 ? 0 : treeAgeYears;
            return calendarAge >= maturityYears ? 0 : maturityYears - calendarAge;
        }

        /// <summary>Prefer a full game year between polls while a tree is still too young to spread.</summary>
        public static double RescheduleIntervalHours(
            ICoreAPI api,
            ReproducerEntry entry,
            EcosystemConfig cfg,
            double normalIntervalHours)
        {
            if (api?.World?.Calendar == null || entry == null || cfg == null)
            {
                return normalIntervalHours;
            }

            if (entry.Requirements?.Habitat != EcologyHabitat.TerrestrialTree)
            {
                return normalIntervalHours;
            }

            if (AllowsSpread(api, entry, cfg))
            {
                return normalIntervalHours;
            }

            IGameCalendar cal = api.World.Calendar;
            if (cal.DaysPerYear <= 0 || cal.HoursPerDay <= 0)
            {
                return normalIntervalHours;
            }

            double yearHours = cal.DaysPerYear * cal.HoursPerDay;
            return yearHours > normalIntervalHours ? yearHours : normalIntervalHours;
        }

        /// <summary>True when this trunk was large enough at registration to use soft size bypass.</summary>
        public static bool EvaluateStructuralBypassEligibility(
            IBlockAccessor acc,
            BlockPos trunkBase,
            string wood,
            EcosystemConfig cfg)
        {
            if (acc == null || trunkBase == null || string.IsNullOrEmpty(wood) || cfg == null)
            {
                return false;
            }

            WildTreeGrowthProfiles.Profile profile = WildTreeGrowthProfiles.Resolve(wood);
            TreeStructureMetrics metrics = TreeStructureProbe.Measure(acc, trunkBase, wood);
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
