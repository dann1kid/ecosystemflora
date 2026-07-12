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
        /// <summary>Stress / timelapse: aggressive spread pacing and high per-tick budgets.</summary>
        public const string Timelapse = "timelapse";
        /// <summary>Extra spread-rate multiplier for tallgrass only (table 1.35 vs colonizer flowers ~2.8).</summary>
        public const float TimelapseTallgrassSpreadMultiplier = 3f;
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
                || preset.Equals(Timelapse, StringComparison.OrdinalIgnoreCase)
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

            if (preset.Equals(Timelapse, StringComparison.OrdinalIgnoreCase))
            {
                ApplyTimelapse(cfg);
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
            target.SpeciesSpreadRateScale = source.SpeciesSpreadRateScale;
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
            cfg.SpeciesSpreadRateScale = 1f / 3f;
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
            cfg.SpeciesSpreadRateScale = 0.45f;
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
            cfg.SpeciesSpreadRateScale = 0.2f;
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

        static void ApplyTimelapse(EcosystemConfig cfg)
        {
            cfg.UseCalendarScaledSpread = true;
            cfg.UseSpeciesSpreadRates = true;
            cfg.SpeciesSpreadRateScale = 10f;
            cfg.ReproduceAttemptsPerYear = 100000;
            cfg.ReproduceChance = 1f;
            cfg.MinFitness = 0.1f;
            cfg.DefaultSameSpeciesSpacing = 0;
            cfg.DefaultOtherSpeciesSpacing = 0;
            cfg.ReproduceRadius = 8;
            cfg.MinSpeciesReproduceIntervalDays = 0;
            cfg.MinSpeciesReproduceIntervalHours = 0;
            cfg.StaggerReproduceAttempts = false;
            cfg.GrowthHoursMultiplier = 10f;
            cfg.HarshWildPlants = false;
            cfg.PlantSpacingEnabled = false;
            cfg.UseSeasonalEcology = false;

            cfg.EnableFlowerSpreadMaturation = false;
            cfg.EnableFernSpreadMaturation = false;
            cfg.EnableTallgrassSpreadMaturation = false;
            cfg.EnableBerrySpreadMaturation = false;
            cfg.EnableFlowerPhenology = false;
            cfg.EnableFernPhenology = false;
            cfg.EnableTallgrassPhenology = false;
            cfg.EnableFernSporulationGate = false;
            cfg.EnableFlowerSpreadAttemptCooldown = false;
            cfg.EnableFernSpreadAttemptCooldown = false;

            cfg.EnableTwoPhaseSpreadPlacement = false;
            cfg.EnableBackgroundSpreadSolve = false;
            cfg.EnableChunkFairSpread = true;
            cfg.UseCellDisplacement = true;
            cfg.DisplacementHoldMargin = 1f;
            cfg.EnableEventDrivenSpread = false;

            cfg.ReproduceTickIntervalMs = 100;
            cfg.TickBudgetMs = 0;
            cfg.SpreadBudgetMs = 0;
            cfg.RegistrationBudgetMs = 0;
            cfg.MaxReproduceAttemptsPerTick = 8192;
            cfg.MaxSpreadChunksVisitedPerTick = 512;
            cfg.MaxSpreadAttemptsPerChunkPerTick = 64;
            cfg.MaxSpreadCommitsPerTick = 8192;
            cfg.MaxSpreadCommitChunksVisitedPerTick = 512;
            cfg.MaxSpreadCommitsPerChunkPerTick = 64;
            cfg.MaxRegistrationsPerTick = 8192;
            cfg.MaxRegistryAppliesPerTick = 8192;
            cfg.MaxRegistryAppliesPerChunkPerTick = 512;
            cfg.MaxPriorityRegistrationsPerTick = 8192;
            cfg.MaxPriorityRegistryAppliesPerTick = 8192;
            cfg.MaxFloraRescanColumnsPerTick = 512;
            cfg.MaxPendingFlowerMaturationChecksPerTick = 512;
            cfg.MaxPendingTallgrassPromotionChecksPerTick = 512;
            cfg.MaxPendingFernMaturationChecksPerTick = 512;
            cfg.MaxPendingBerryMaturationChecksPerTick = 512;
            cfg.MaxTreeGrowthAttemptsPerTick = 128;
            cfg.TreeGrowthActivityScale = 10f;
        }
    }
}
