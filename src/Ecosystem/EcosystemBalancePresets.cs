using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;
using WildFarming.Ecosystem.Config;

namespace WildFarming.Ecosystem
{
    /// <summary>Named tuning bundles for <see cref="EcosystemConfig"/>.</summary>
    public static class EcosystemBalancePresets
    {
        public const string Natural = "natural";
        public const string Lush = "lush";
        public const string Sparse = "sparse";
        public const string VanillaMinimal = "vanilla-minimal";
        public const string Custom = "custom";

        static readonly Dictionary<string, EcosystemConfig> filePresets =
            new Dictionary<string, EcosystemConfig>(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyCollection<string> FilePresetCodes => filePresets.Keys;

        public static bool IsKnownPreset(string preset)
        {
            if (string.IsNullOrWhiteSpace(preset)) return false;
            return preset.Equals(Natural, StringComparison.OrdinalIgnoreCase)
                || preset.Equals(Lush, StringComparison.OrdinalIgnoreCase)
                || preset.Equals(Sparse, StringComparison.OrdinalIgnoreCase)
                || preset.Equals(VanillaMinimal, StringComparison.OrdinalIgnoreCase)
                || filePresets.ContainsKey(preset);
        }

        /// <summary>Load optional user JSON presets from ModConfig/ecosystemflora.presets/*.json.</summary>
        public static void TryLoadFilePresets(ICoreAPI api)
        {
            filePresets.Clear();
            if (api == null) return;

            string dir = Path.Combine(api.GetOrCreateDataPath("ModConfig"), "ecosystemflora.presets");
            if (!Directory.Exists(dir)) return;

            foreach (string file in Directory.GetFiles(dir, "*.json"))
            {
                try
                {
                    EcosystemConfig preset = EcosystemConfigCopier.FromJson(File.ReadAllText(file));
                    if (preset == null) continue;
                    string code = Path.GetFileNameWithoutExtension(file);
                    if (string.IsNullOrWhiteSpace(code)) continue;
                    filePresets[code] = preset;
                }
                catch (Exception ex)
                {
                    if (EcosystemConfig.Loaded?.VerboseLogging == true)
                    {
                        api.Logger.Warning(
                            "[ecosystemflora] Failed to load balance preset {0}: {1}",
                            file,
                            ex.Message);
                    }
                }
            }
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

            if (preset.Equals(VanillaMinimal, StringComparison.OrdinalIgnoreCase))
            {
                ApplyVanillaMinimal(cfg);
                return;
            }

            if (filePresets.TryGetValue(preset, out EcosystemConfig filePreset))
            {
                ApplyFilePreset(cfg, filePreset);
                return;
            }

            if (preset.Equals(Natural, StringComparison.OrdinalIgnoreCase))
            {
                ApplyNatural(cfg);
            }
        }

        static void ApplyFilePreset(EcosystemConfig target, EcosystemConfig source)
        {
            if (target == null || source == null) return;

            target.UseCalendarScaledSpread = source.UseCalendarScaledSpread;
            target.UseSpeciesSpreadRates = source.UseSpeciesSpreadRates;
            target.ReproduceAttemptsPerYear = source.ReproduceAttemptsPerYear;
            target.ReproduceChance = source.ReproduceChance;
            target.MinFitness = source.MinFitness;
            target.DefaultSameSpeciesSpacing = source.DefaultSameSpeciesSpacing;
            target.DefaultOtherSpeciesSpacing = source.DefaultOtherSpeciesSpacing;
            target.EnableFlowerSpreadMaturation = source.EnableFlowerSpreadMaturation;
            target.EnableFernSpreadMaturation = source.EnableFernSpreadMaturation;
            target.EnableTallgrassSpreadMaturation = source.EnableTallgrassSpreadMaturation;
            target.EnableBerrySpreadMaturation = source.EnableBerrySpreadMaturation;
            target.EnableFlowerPhenology = source.EnableFlowerPhenology;
            target.EnableFernPhenology = source.EnableFernPhenology;
            target.EnableTallgrassPhenology = source.EnableTallgrassPhenology;
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

        static void ApplyVanillaMinimal(EcosystemConfig cfg)
        {
            ApplyNatural(cfg);
            cfg.EnableFlowerSpreadMaturation = false;
            cfg.EnableFernSpreadMaturation = false;
            cfg.EnableTallgrassSpreadMaturation = false;
            cfg.EnableBerrySpreadMaturation = false;
            cfg.EnableFlowerPhenology = false;
            cfg.EnableFernPhenology = false;
            cfg.EnableTallgrassPhenology = false;
        }
    }
}
