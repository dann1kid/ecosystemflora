using System;

namespace WildFarming.Ecosystem
{
    /// <summary>Named tuning bundles for <see cref="EcosystemConfig"/>.</summary>
    public static class EcosystemBalancePresets
    {
        public const string Natural = "natural";
        public const string Lush = "lush";
        public const string Sparse = "sparse";
        public const string Custom = "custom";

        public static bool IsKnownPreset(string preset)
        {
            if (string.IsNullOrWhiteSpace(preset)) return false;
            return preset.Equals(Natural, StringComparison.OrdinalIgnoreCase)
                || preset.Equals(Lush, StringComparison.OrdinalIgnoreCase)
                || preset.Equals(Sparse, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Overwrites spread/spacing fields on <paramref name="cfg"/>.</summary>
        public static void Apply(EcosystemConfig cfg, string preset)
        {
            if (cfg == null || string.IsNullOrWhiteSpace(preset)) return;
            if (preset.Equals(Custom, StringComparison.OrdinalIgnoreCase)) return;

            if (preset.Equals(Lush, StringComparison.OrdinalIgnoreCase))
            {
                ApplyLush(cfg);
                return;
            }

            if (preset.Equals(Sparse, StringComparison.OrdinalIgnoreCase))
            {
                ApplySparse(cfg);
                return;
            }

            if (preset.Equals(Natural, StringComparison.OrdinalIgnoreCase))
            {
                ApplyNatural(cfg);
                return;
            }
        }

        static void ApplyNatural(EcosystemConfig cfg)
        {
            cfg.UseCalendarScaledSpread = true;
            cfg.UseSpeciesSpreadRates = true;
            cfg.ReproduceAttemptsPerYear = 72;
            cfg.ReproduceChance = 0.5f;
            cfg.MinFitness = 0.45f;
            cfg.DefaultSameSpeciesSpacing = 1;
            cfg.DefaultOtherSpeciesSpacing = 1;
        }

        static void ApplyLush(EcosystemConfig cfg)
        {
            cfg.UseCalendarScaledSpread = true;
            cfg.UseSpeciesSpreadRates = true;
            cfg.ReproduceAttemptsPerYear = 120;
            cfg.ReproduceChance = 0.65f;
            cfg.MinFitness = 0.35f;
            cfg.DefaultSameSpeciesSpacing = 1;
            cfg.DefaultOtherSpeciesSpacing = 1;
        }

        static void ApplySparse(EcosystemConfig cfg)
        {
            cfg.UseCalendarScaledSpread = true;
            cfg.UseSpeciesSpreadRates = true;
            cfg.ReproduceAttemptsPerYear = 36;
            cfg.ReproduceChance = 0.3f;
            cfg.MinFitness = 0.6f;
            cfg.DefaultSameSpeciesSpacing = 2;
            cfg.DefaultOtherSpeciesSpacing = 2;
        }
    }
}
