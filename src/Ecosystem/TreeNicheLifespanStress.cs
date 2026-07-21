using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

// Export-only C# tables: fallback when SpeciesEcologyRegistry is not loaded.
#pragma warning disable CS0618

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Niche mismatch shortens effective calendar lifespan (debt years), then normal senescence runs.
    /// Evaluated on the tree-growth path — not on the flower stress tick.
    /// Climate/forest is sampled at most once per tree per growth tick (reused across catch-up years).
    /// </summary>
    internal static class TreeNicheLifespanStress
    {
        public enum YearOutcome : byte
        {
            Skipped = 0,
            InNiche = 1,
            SoftMiss = 2,
            HardMiss = 3,
        }

        public static int EffectiveHorizon(
            int senescenceAgeYears,
            int lifespanDebtYears,
            EcosystemConfig cfg)
        {
            if (senescenceAgeYears <= 0) return senescenceAgeYears;

            int debt = ClampDebt(senescenceAgeYears, lifespanDebtYears, cfg);
            int effective = senescenceAgeYears - debt;
            return effective < 1 ? 1 : effective;
        }

        public static int EffectiveHorizon(
            WildTreeGrowthProfiles.Profile profile,
            ReproducerEntry entry,
            EcosystemConfig cfg)
        {
            if (profile.SenescenceAgeYears <= 0) return profile.SenescenceAgeYears;
            int debt = entry?.TreeLifespanDebtYears ?? 0;
            if (cfg == null || !cfg.EnableTreeNicheLifespanStress) debt = 0;
            return EffectiveHorizon(profile.SenescenceAgeYears, debt, cfg);
        }

        public static int MaxDebtYears(int senescenceAgeYears, EcosystemConfig cfg)
        {
            if (senescenceAgeYears <= 0 || cfg == null) return 0;
            float frac = cfg.TreeNicheLifespanStressMaxDebtFraction;
            if (frac < 0f) frac = 0f;
            if (frac > 0.9f) frac = 0.9f;
            return Math.Max(0, (int)(senescenceAgeYears * frac));
        }

        public static int ClampDebt(int senescenceAgeYears, int debtYears, EcosystemConfig cfg)
        {
            if (debtYears <= 0) return 0;
            int max = MaxDebtYears(senescenceAgeYears, cfg);
            if (debtYears > max) return max;
            return debtYears;
        }

        public static bool ShouldEvaluate(ReproducerEntry entry, EcosystemConfig cfg)
        {
            if (entry == null || cfg == null || !cfg.EnableTreeNicheLifespanStress) return false;
            if (entry.TreeSenescencePhase != TreeSenescencePhase.None) return false;
            if (entry.TreeAgeYears < Math.Max(0, cfg.TreeNicheLifespanStressGraceYears)) return false;
            if (entry.Requirements?.Habitat != EcologyHabitat.TerrestrialTree) return false;
            return true;
        }

        /// <summary>
        /// Hard miss = temp / rain / forest window fail.
        /// Soft miss = hard OK but seral multiplier below threshold (when seral succession is on).
        /// </summary>
        public static YearOutcome ClassifyYear(
            PlantRequirements req,
            IEnvironmentalContext ctx,
            string wood,
            EcosystemConfig cfg)
        {
            if (req == null || ctx == null || cfg == null) return YearOutcome.Skipped;
            if (!ctx.HasClimate) return YearOutcome.Skipped;

            if (ctx.Temperature < req.MinTemp || ctx.Temperature > req.MaxTemp)
            {
                return YearOutcome.HardMiss;
            }

            if (!MeetsRainfall(req, ctx, cfg)) return YearOutcome.HardMiss;
            if (!MeetsLocalForest(req, ctx)) return YearOutcome.HardMiss;

            if (cfg.EnableTreeSeralSuccession
                && !string.IsNullOrEmpty(wood)
                && cfg.TreeNicheLifespanStressSeralSoftThreshold > 0f)
            {
                float seral = WildTreeEcology.SeralSpreadMultiplier(wood, ctx.LocalForestCover);
                if (seral < cfg.TreeNicheLifespanStressSeralSoftThreshold)
                {
                    return YearOutcome.SoftMiss;
                }
            }

            return YearOutcome.InNiche;
        }

        /// <summary>One world sample for niche classification (reuse across catch-up years in the same tick).</summary>
        public static YearOutcome SampleOutcome(
            ICoreAPI api,
            ReproducerEntry entry,
            string wood,
            EcosystemConfig cfg)
        {
            if (api == null || !ShouldEvaluate(entry, cfg)) return YearOutcome.Skipped;

            PlantRequirements req = entry.Requirements;
            EnvironmentalContext ctx = EnvironmentalContext.SampleForSurvival(api, entry.Origin, req);
            return ClassifyYear(req, ctx, wood, cfg);
        }

        public static void ApplyOutcome(
            ReproducerEntry entry,
            YearOutcome outcome,
            int senescenceAgeYears,
            EcosystemConfig cfg)
        {
            if (entry == null || cfg == null || !cfg.EnableTreeNicheLifespanStress) return;
            if (outcome == YearOutcome.Skipped) return;

            int debt = entry.TreeLifespanDebtYears;
            switch (outcome)
            {
                case YearOutcome.HardMiss:
                    debt += Math.Max(0, cfg.TreeNicheLifespanStressHardDebtPerYear);
                    break;
                case YearOutcome.SoftMiss:
                    debt += Math.Max(0, cfg.TreeNicheLifespanStressSoftDebtPerYear);
                    break;
                case YearOutcome.InNiche:
                    debt -= Math.Max(0, cfg.TreeNicheLifespanStressRecoveryPerYear);
                    break;
            }

            entry.TreeLifespanDebtYears = ClampDebt(senescenceAgeYears, debt, cfg);
        }

        /// <summary>
        /// Sample + apply for a single year. Prefer <see cref="SampleOutcome"/> + <see cref="ApplyOutcome"/>
        /// when catching up multiple years in one tick.
        /// </summary>
        public static YearOutcome ApplyYear(
            ICoreAPI api,
            ReproducerEntry entry,
            string wood,
            int senescenceAgeYears,
            EcosystemConfig cfg)
        {
            YearOutcome outcome = SampleOutcome(api, entry, wood, cfg);
            ApplyOutcome(entry, outcome, senescenceAgeYears, cfg);
            return outcome;
        }

        static bool MeetsRainfall(PlantRequirements req, IEnvironmentalContext ctx, EcosystemConfig cfg)
        {
            if (cfg == null || !cfg.ApplyWorldgenRainForest) return true;
            return InRange(ctx.WorldgenRainfall, req.MinRain, req.MaxRain);
        }

        static bool MeetsLocalForest(PlantRequirements req, IEnvironmentalContext ctx) =>
            InRange(ctx.LocalForestCover, req.MinForest, req.MaxForest);

        static bool InRange(float value, float min, float max) => value >= min && value <= max;
    }
}
