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
            cfg.ReproduceAttemptsPerYear = 42;
            cfg.ReproduceChance = 0.35f;
            cfg.MinFitness = 0.5f;
            cfg.DefaultSameSpeciesSpacing = 1;
            cfg.DefaultOtherSpeciesSpacing = 1;
            cfg.MaxReproduceAttemptsPerTick = 48;
        }

        static void ApplyLush(EcosystemConfig cfg)
        {
            cfg.ReproduceAttemptsPerYear = 60;
            cfg.ReproduceChance = 0.45f;
            cfg.MinFitness = 0.4f;
            cfg.DefaultSameSpeciesSpacing = 1;
            cfg.DefaultOtherSpeciesSpacing = 1;
            cfg.MaxReproduceAttemptsPerTick = 64;
        }

        static void ApplySparse(EcosystemConfig cfg)
        {
            cfg.ReproduceAttemptsPerYear = 24;
            cfg.ReproduceChance = 0.2f;
            cfg.MinFitness = 0.65f;
            cfg.DefaultSameSpeciesSpacing = 2;
            cfg.DefaultOtherSpeciesSpacing = 2;
            cfg.MaxReproduceAttemptsPerTick = 32;
        }
    }
}
