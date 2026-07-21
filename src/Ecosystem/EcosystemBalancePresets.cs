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

        /// <summary>Load optional user JSON presets from ModConfig/ecosystemflora/presets/*.json.</summary>
        public static void TryLoadFilePresets(ICoreAPI api)
        {
            filePresets.Clear();
            if (api == null) return;

            EcosystemConfigPaths.MigrateLegacyLayout(api);
            string dir = EcosystemConfigPaths.GetPresetsFolder(api);
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

            // Full natural ecology feature set (vanilla-minimal turns maturation/phenology back off).
            cfg.EcosystemEnabled = true;
            cfg.HarshWildPlants = true;
            cfg.ApplyWorldgenRainForest = true;
            cfg.EnableThirdPartyParticipants = true;

            cfg.EnableFlowerSpreadMaturation = true;
            cfg.EnableFernSpreadMaturation = true;
            cfg.EnableTallgrassSpreadMaturation = true;
            cfg.EnableBerrySpreadMaturation = true;
            cfg.EnableFlowerPhenology = true;
            cfg.EnableFernPhenology = true;
            cfg.EnableTallgrassPhenology = true;
            cfg.EnableFlowerSpreadAttemptCooldown = true;
            cfg.EnableFernSpreadAttemptCooldown = true;
            cfg.EnableFernSporulationGate = true;
            cfg.EnableFernRhizomeSpread = true;
            cfg.EnableBerryColonySpread = true;
            cfg.EnableShoreSedgeMatSpread = true;

            cfg.UseSeasonalEcology = true;
            cfg.SeasonalStressEnabled = true;
            cfg.EnableSeasonalFoliage = true;
            cfg.EnableSeasonCoarseWake = true;

            cfg.EnableTrampling = false;
            cfg.TramplingSoilDegradation = false;
            // Players-only trails are opt-in; animal hooks + soil SetBlock were SSP hitch sources.
            cfg.EnableAnimalFootTraffic = false;

            cfg.EnableTreeAging = true;
            cfg.EnableTreeSenescence = true;
            cfg.EnableTreeNicheLifespanStress = true;
            cfg.EnableTreeSeralSuccession = true;
            cfg.EnableTreeSenescenceRemains = true;
            cfg.EnableFerntreeEcology = true;
            cfg.EnableWildVineEcology = true;

            cfg.EnableMyceliumEcology = true;
            cfg.EnableMyceliumNiche = true;
            cfg.EnableMyceliumNetworkSpread = true;
            cfg.EnableMyceliumCapDisplacement = true;

            cfg.EnableSymbiosis = true;
            cfg.EnableStressDeath = true;
            cfg.UseNicheContext = true;
            cfg.UseSoilSuccession = true;
            cfg.UseFarmlandNutrientBridge = true;
            cfg.EnableFallowRestoration = true;

            cfg.PlantSpacingEnabled = true;
            cfg.ApplyCrossHabitatSpacing = true;
            cfg.UseCellDisplacement = true;
            cfg.EnableChunkFairSpread = true;
            cfg.EnableEventDrivenSpread = true;
            cfg.EnableBackgroundSpreadSolve = true;
            cfg.EnableTwoPhaseSpreadPlacement = true;
            cfg.EnableBackgroundRegistrationScan = true;
            cfg.EnableCyclicFloraDiscovery = true;
            cfg.EnableCyclicTreeDiscovery = true;

            cfg.UseRhizomeSpreadForReeds = true;
            cfg.UseSurfaceMatSpreadForLilies = true;
            cfg.EnableStumpDecay = true;
            cfg.EnableFlowerDrygrass = true;
            cfg.EnableOrphanFoliagePrune = true;
            cfg.EnableCanopyFallenSticks = true;
            cfg.EnableSpringBranchyAgeBoost = true;

            // Playable tick cadence — never inherit timelapse leftovers into natural/custom merges.
            cfg.ReproduceTickIntervalMs = 3500;
            cfg.ChunkScanTickIntervalMs = 2300;
            cfg.StressTickIntervalMs = 8500;
            cfg.TickBudgetMs = 2;
            cfg.SpreadBudgetMs = 1;
            cfg.RegistrationBudgetMs = 9;
            cfg.MaxReproduceAttemptsPerTick = 14;
            cfg.MaxSpreadAttemptsPerChunkPerTick = 1;
            cfg.MaxSpreadChunksVisitedPerTick = 12;
            cfg.MaxFoliageCellsTickedPerTick = 11;
            cfg.FoliageColumnScanHeightAboveSurface = 48;
            cfg.FoliageCatchUpOnChunkLoad = true;
            cfg.MaxFoliageCatchUpPerChunk = 54;
            cfg.MaxFloraRescanColumnsPerTick = 7;
            cfg.EnablePlayerVicinityRescan = true;
            cfg.PlayerVicinityRescanIntervalMs = 5000;
        }

        static void ApplyLush(EcosystemConfig cfg)
        {
            ApplyNatural(cfg);
            cfg.SpeciesSpreadRateScale = 0.45f;
            cfg.ReproduceAttemptsPerYear = 120;
            cfg.ReproduceChance = 0.65f;
            cfg.MinFitness = 0.35f;
            cfg.DefaultSameSpeciesSpacing = 1;
            cfg.DefaultOtherSpeciesSpacing = 1;
        }

        static void ApplySparse(EcosystemConfig cfg)
        {
            ApplyNatural(cfg);
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
            // PlantSpacingEnabled is off below — keep non-zero fallbacks so a later custom
            // re-enable does not inherit “allow adjacent trunks/plants” from this preset.
            cfg.DefaultSameSpeciesSpacing = 1;
            cfg.DefaultOtherSpeciesSpacing = 1;
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
            cfg.MaxFlowerPhenologyLifeCycles = 0;
            cfg.EnableFlowerSpreadAttemptCooldown = false;
            cfg.EnableFernSpreadAttemptCooldown = false;

            cfg.EnableTwoPhaseSpreadPlacement = false;
            cfg.EnableBackgroundSpreadSolve = false;
            cfg.EnableChunkFairSpread = true;
            cfg.UseCellDisplacement = true;
            cfg.DisplacementHoldMargin = 1f;
            cfg.EnableEventDrivenSpread = false;

            // Timelapse recording: spread/foliage load only — trails add save scans, soil SetBlock, and animal physics hooks.
            cfg.EnableTrampling = false;
            cfg.TramplingSoilDegradation = false;
            cfg.EnableAnimalFootTraffic = false;

            // Smoother CPU: 4× tick rate, ¼ batch caps (~same aggregate spread throughput).
            cfg.ReproduceTickIntervalMs = 25;
            cfg.ChunkScanTickIntervalMs = 17;
            cfg.StressTickIntervalMs = 1375;
            cfg.TickBudgetMs = 0;
            cfg.SpreadBudgetMs = 0;
            cfg.RegistrationBudgetMs = 0;
            cfg.MaxReproduceAttemptsPerTick = 2048;
            cfg.MaxSpreadChunksVisitedPerTick = 512;
            cfg.MaxSpreadAttemptsPerChunkPerTick = 16;
            cfg.MaxSpreadCommitsPerTick = 2048;
            cfg.MaxSpreadCommitChunksVisitedPerTick = 512;
            cfg.MaxSpreadCommitsPerChunkPerTick = 16;
            cfg.MaxRegistrationsPerTick = 2048;
            cfg.MaxRegistryAppliesPerTick = 2048;
            cfg.MaxRegistryAppliesPerChunkPerTick = 128;
            cfg.MaxPriorityRegistrationsPerTick = 2048;
            cfg.MaxPriorityRegistryAppliesPerTick = 2048;
            cfg.MaxChunkColumnsScannedPerTick = 128;
            cfg.MaxPriorityChunkScansPerTick = 48;
            cfg.MaxRegistrationSnapshotCellsPerTick = 2048;
            cfg.MaxStressChecksPerTick = 16;
            cfg.MaxFloraRescanColumnsPerTick = 128;
            cfg.MaxPendingFlowerMaturationChecksPerTick = 128;
            cfg.MaxPendingTallgrassPromotionChecksPerTick = 128;
            cfg.MaxPendingFernMaturationChecksPerTick = 128;
            cfg.MaxPendingBerryMaturationChecksPerTick = 128;
            cfg.MaxTreeGrowthAttemptsPerTick = 32;
            cfg.TreeGrowthActivityScale = 10f;
        }
    }
}
