using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace WildFarming.Ecosystem.Config
{
    public static class EcosystemConfigSchema
    {
        static readonly HashSet<string> HiddenProperties = new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(EcosystemConfig.MaxCanopyUpdateOpsPerTick),
            nameof(EcosystemConfig.CanopyBudgetMs),
        };

        static readonly HashSet<string> PresetFields = new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(EcosystemConfig.ReproduceAttemptsPerYear),
            nameof(EcosystemConfig.ReproduceChance),
            nameof(EcosystemConfig.MinFitness),
            nameof(EcosystemConfig.DefaultSameSpeciesSpacing),
            nameof(EcosystemConfig.DefaultOtherSpeciesSpacing),
            nameof(EcosystemConfig.UseCalendarScaledSpread),
            nameof(EcosystemConfig.UseSpeciesSpreadRates),
        };

        static readonly HashSet<string> ClientOnlyProperties = new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(EcosystemConfig.EnableCanopyAmbience),
            nameof(EcosystemConfig.CanopyAmbienceMinHeightBlocks),
            nameof(EcosystemConfig.CanopyAmbienceMoteRate),
            nameof(EcosystemConfig.CanopyAmbienceLeafDriftRate),
            nameof(EcosystemConfig.CanopyAmbienceSampleIntervalSeconds),
            nameof(EcosystemConfig.CanopyAmbienceSuppressInRain),
            nameof(EcosystemConfig.EnableEcologyInspect),
            nameof(EcosystemConfig.EcologyInspectCooldownSeconds),
            nameof(EcosystemConfig.EcologyInspectScanRadius),
            nameof(EcosystemConfig.EnableEcologyAreaScan),
        };

        static readonly (string prefix, string category)[] PrefixCategories =
        {
            ("BalancePreset", "master"),
            ("EcosystemEnabled", "master"),
            ("HarshWildPlants", "master"),
            ("ApplyWorldgenRainForest", "master"),
            ("EnableThirdPartyParticipants", "master"),
            ("Reproduce", "spread"),
            ("MinFitness", "spread"),
            ("MinSpeciesReproduce", "spread"),
            ("UseCalendarScaledSpread", "spread"),
            ("UseSpeciesSpreadRates", "spread"),
            ("GrowthHoursMultiplier", "spread"),
            ("EnableFlowerSpreadMaturation", "spread"),
            ("EnableFlowerSpreadAttemptCooldown", "spread"),
            ("FlowerSpreadCooldownHoursMultiplier", "spread"),
            ("MaxPendingFlowerMaturationChecksPerTick", "spread"),
            ("EnableFlowerPhenology", "spread"),
            ("FlowerBloom", "spread"),
            ("FlowerPhenology", "spread"),
            ("MaxFlowerPhenologyChecksPerTick", "spread"),
            ("EnableTallgrassSpreadMaturation", "spread"),
            ("MaxPendingTallgrassPromotionChecksPerTick", "spread"),
            ("EnableFernRhizomeSpread", "spread"),
            ("EnableBerryColonySpread", "spread"),
            ("EnableFernSpreadMaturation", "spread"),
            ("EnableFernSpreadAttemptCooldown", "spread"),
            ("EnableFernSporulationGate", "spread"),
            ("FernSpreadCooldownHoursMultiplier", "spread"),
            ("MaxPendingFernMaturationChecksPerTick", "spread"),
            ("EnableFernPhenology", "spread"),
            ("MaxFernPhenologyChecksPerTick", "spread"),
            ("EnableTallgrassPhenology", "spread"),
            ("MaxTallgrassPhenologyChecksPerTick", "spread"),
            ("EnableBerrySpreadMaturation", "spread"),
            ("MaxPendingBerryMaturationChecksPerTick", "spread"),
            ("EnableStumpDecay", "trees"),
            ("StumpDecayYears", "trees"),
            ("MaxStumpDecayChecksPerTick", "trees"),
            ("EnableEcologyHistoryHint", "master"),
            ("EventWakeRetryHours", "spread"),
            ("Rhizome", "aquatic"),
            ("UseRhizome", "aquatic"),
            ("UseSurfaceMat", "aquatic"),
            ("PlantSpacing", "competition"),
            ("DefaultSameSpeciesSpacing", "competition"),
            ("DefaultOtherSpeciesSpacing", "competition"),
            ("ApplyCrossHabitatSpacing", "competition"),
            ("SpacingVerticalSearch", "competition"),
            ("UseCellDisplacement", "competition"),
            ("Displacement", "competition"),
            ("PreferSpreadToEmptyCells", "competition"),
            ("EnableEmptyFirstSpread", "competition"),
            ("EnableSpreadColumnOccupancyHint", "competition"),
            ("EmptySpreadFitnessMultiplier", "competition"),
            ("UseFloraContext", "competition"),
            ("FloraContext", "competition"),
            ("FloraOpenInteriorPenalty", "competition"),
            ("UseNicheContext", "competition"),
            ("Niche", "competition"),
            ("EnableStressDeath", "stress"),
            ("Stress", "stress"),
            ("MaxFailedSurvivalChecks", "stress"),
            ("EnableSymbiosis", "stress"),
            ("Symbiosis", "stress"),
            ("UseSeasonalEcology", "stress"),
            ("SeasonalStressEnabled", "stress"),
            ("EnableTrampling", "stress"),
            ("Trampling", "stress"),
            ("EnableTree", "trees"),
            ("Tree", "trees"),
            ("EnableTreeSeralSuccession", "trees"),
            ("MaxPendingTreeChecks", "trees"),
            ("EnableCyclicTreeDiscovery", "trees"),
            ("MaxTreeRescanColumns", "trees"),
            ("EnableCyclicFloraDiscovery", "spread"),
            ("MaxFloraRescanColumns", "spread"),
            ("EnableFerntree", "trees"),
            ("Ferntree", "trees"),
            ("EnableWildVine", "trees"),
            ("WildVine", "trees"),
            ("CloneBerryTraits", "trees"),
            ("BerryTrait", "trees"),
            ("EnableSeasonalFoliage", "canopy"),
            ("MaxFoliage", "canopy"),
            ("Foliage", "canopy"),
            ("Canopy", "canopy"),
            ("EnableCanopyFallenSticks", "canopy"),
            ("EnableSpringBranchyAgeBoost", "canopy"),
            ("SpringBranchy", "canopy"),
            ("EnableMycelium", "mycelium"),
            ("Mycelium", "mycelium"),
            ("UseSoilSuccession", "soil"),
            ("SoilSuccession", "soil"),
            ("UseFarmlandNutrientBridge", "soil"),
            ("FarmlandNutrient", "soil"),
            ("EnableFallowRestoration", "soil"),
            ("FallowRestoration", "soil"),
            ("EnableFlowerDrygrass", "harvest"),
            ("RespectLandClaims", "scope"),
            ("OnlyActivateNearPlayers", "scope"),
            ("LimitSpreadNearPlayers", "scope"),
            ("PlayerActivationRadiusBlocks", "scope"),
            ("EnableChunkFairSpread", "perf"),
            ("MaxSpread", "perf"),
            ("EnableEventDrivenSpread", "perf"),
            ("EnableSeasonCoarseWake", "perf"),
            ("EcologyWakeRadiusBlocks", "perf"),
            ("EnableEcologyColumnCache", "perf"),
            ("EnableTwoPhaseSpreadPlacement", "perf"),
            ("MaxReproduceAttemptsPerTick", "perf"),
            ("MaxChunkColumnsScannedPerTick", "perf"),
            ("MaxRegistrationsPerTick", "perf"),
            ("EnablePlayerPriorityRegistration", "perf"),
            ("EnableBurstRegistrationNearPlayers", "perf"),
            ("PlayerRegistrationPriorityRadiusBlocks", "perf"),
            ("MaxPriorityChunkScansPerTick", "perf"),
            ("MaxPriorityRegistrationsPerTick", "perf"),
            ("PriorityRegistrationBudgetMs", "perf"),
            ("BurstRegistrationBudgetMs", "perf"),
            ("MaxBurstRegistrationsPerChunk", "perf"),
            ("MaxRegistryAppliesPerTick", "perf"),
            ("MaxPriorityRegistryAppliesPerTick", "perf"),
            ("MaxRegistryAppliesPerChunkPerTick", "perf"),
            ("RegistrationWorkerCount", "perf"),
            ("EnableBackgroundRegistrationScan", "perf"),
            ("EnableBackgroundSpreadSolve", "perf"),
            ("SpreadWorkerCount", "perf"),
            ("MaxRegistrationSnapshotCellsPerTick", "perf"),
            ("TickBudgetMs", "perf"),
            ("SpreadBudgetMs", "perf"),
            ("RegistrationBudgetMs", "perf"),
            ("StressBudgetMs", "perf"),
            ("EnableReproduceTickProfiling", "perf"),
            ("ReproduceTickProfiling", "perf"),
            ("StressTickIntervalMs", "perf"),
            ("ReproduceTickIntervalMs", "perf"),
            ("ChunkScanTickIntervalMs", "perf"),
            ("MaxTreeGrowthAttemptsPerTick", "perf"),
            ("MaxStressChecksPerTick", "perf"),
            ("ReproduceDebug", "advanced"),
            ("VerboseLogging", "advanced"),
        };

        public static readonly string[] CategoryOrder =
        {
            "master",
            "spread",
            "aquatic",
            "competition",
            "stress",
            "trees",
            "canopy",
            "mycelium",
            "soil",
            "harvest",
            "scope",
            "perf",
            "advanced",
        };

        public static readonly string[] PresetCodes =
        {
            EcosystemBalancePresets.Natural,
            EcosystemBalancePresets.Lush,
            EcosystemBalancePresets.Sparse,
            EcosystemBalancePresets.VanillaMinimal,
            EcosystemBalancePresets.Custom,
        };

        static EcosystemConfigFieldDescriptor[] cachedFields;
        static Dictionary<string, EcosystemConfigFieldDescriptor> cachedByName;

        public static IReadOnlyList<EcosystemConfigFieldDescriptor> Fields =>
            cachedFields ??= BuildFields();

        public static EcosystemConfigFieldDescriptor GetField(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            cachedByName ??= Fields.ToDictionary(f => f.Name, StringComparer.Ordinal);
            cachedByName.TryGetValue(name, out EcosystemConfigFieldDescriptor field);
            return field;
        }

        public static IReadOnlyList<EcosystemConfigFieldDescriptor> GetCategoryFields(string category)
        {
            return Fields
                .Where(f => string.Equals(f.Category, category, StringComparison.Ordinal))
                .OrderBy(f => f.Order)
                .ThenBy(f => f.Name, StringComparer.Ordinal)
                .ToArray();
        }

        static EcosystemConfigFieldDescriptor[] BuildFields()
        {
            var list = new List<EcosystemConfigFieldDescriptor>();
            PropertyInfo[] props = typeof(EcosystemConfig).GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (PropertyInfo prop in props)
            {
                if (!prop.CanRead || !prop.CanWrite) continue;
                if (HiddenProperties.Contains(prop.Name)) continue;

                ConfigFieldKind? kind = ResolveKind(prop.PropertyType);
                if (kind == null) continue;

                ConfigFieldAttribute attr = prop.GetCustomAttribute<ConfigFieldAttribute>();
                if (attr?.Hide == true) continue;

                ConfigFieldScope scope = attr?.Scope
                    ?? (ClientOnlyProperties.Contains(prop.Name) ? ConfigFieldScope.Client : ConfigFieldScope.Server);

                ResolveBounds(prop.Name, kind.Value, attr, out double min, out double max, out string[] allowed);

                list.Add(new EcosystemConfigFieldDescriptor
                {
                    Property = prop,
                    Name = prop.Name,
                    Kind = kind.Value,
                    Category = attr?.Category ?? InferCategory(prop.Name),
                    Order = attr?.Order ?? 100,
                    Scope = scope,
                    Min = min,
                    Max = max,
                    AllowedValues = allowed,
                    IsPresetField = PresetFields.Contains(prop.Name),
                });
            }

            return list.ToArray();
        }

        static ConfigFieldKind? ResolveKind(Type type)
        {
            if (type == typeof(bool)) return ConfigFieldKind.Boolean;
            if (type == typeof(int)) return ConfigFieldKind.Integer;
            if (type == typeof(float)) return ConfigFieldKind.Float;
            if (type == typeof(double)) return ConfigFieldKind.Double;
            if (type == typeof(string)) return ConfigFieldKind.String;
            return null;
        }

        static string InferCategory(string name)
        {
            foreach ((string prefix, string category) in PrefixCategories)
            {
                if (name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return category;
                }
            }

            return "advanced";
        }

        static void ResolveBounds(
            string name,
            ConfigFieldKind kind,
            ConfigFieldAttribute attr,
            out double min,
            out double max,
            out string[] allowed)
        {
            allowed = attr?.AllowedValues;
            min = attr != null && !double.IsNaN(attr.Min) ? attr.Min : double.NaN;
            max = attr != null && !double.IsNaN(attr.Max) ? attr.Max : double.NaN;

            if (name == nameof(EcosystemConfig.BalancePreset))
            {
                allowed = PresetCodes;
                return;
            }

            if (name == nameof(EcosystemConfig.FoliageSyncMode))
            {
                allowed = new[] { "chunk", "hybrid", "random" };
                return;
            }

            if (allowed != null && allowed.Length > 0) return;

            if (kind == ConfigFieldKind.Boolean) return;

            if (name.EndsWith("Chance", StringComparison.Ordinal)
                || name.EndsWith("Fitness", StringComparison.Ordinal)
                || name.EndsWith("Penalty", StringComparison.Ordinal))
            {
                if (double.IsNaN(min)) min = 0;
                if (double.IsNaN(max)) max = 1;
                return;
            }

            if (name.EndsWith("Bonus", StringComparison.Ordinal)
                || name.Contains("Multiplier", StringComparison.Ordinal)
                || name.Contains("Scale", StringComparison.Ordinal)
                || name.Contains("Margin", StringComparison.Ordinal)
                || (name.EndsWith("Rate", StringComparison.Ordinal) && kind == ConfigFieldKind.Float))
            {
                if (double.IsNaN(min)) min = 0;
                if (double.IsNaN(max)) max = 10;
                return;
            }

            if (name.EndsWith("Radius", StringComparison.Ordinal)
                || name.EndsWith("Blocks", StringComparison.Ordinal)
                || name.EndsWith("Ms", StringComparison.Ordinal)
                || name.EndsWith("Hours", StringComparison.Ordinal)
                || name.EndsWith("Days", StringComparison.Ordinal)
                || name.EndsWith("Seconds", StringComparison.Ordinal)
                || name.EndsWith("IntervalMs", StringComparison.Ordinal))
            {
                if (double.IsNaN(min)) min = 0;
                if (double.IsNaN(max)) max = name.EndsWith("Ms", StringComparison.Ordinal) ? 600000 : 4096;
                return;
            }

            if (name.EndsWith("PerTick", StringComparison.Ordinal))
            {
                if (double.IsNaN(min)) min = 0;
                if (double.IsNaN(max)) max = 32768;
                return;
            }

            if (kind == ConfigFieldKind.Integer || kind == ConfigFieldKind.Float || kind == ConfigFieldKind.Double)
            {
                if (double.IsNaN(min)) min = 0;
                if (double.IsNaN(max)) max = kind == ConfigFieldKind.Integer ? 100000 : 100000d;
            }
        }

        public static bool IsPresetControlledField(string propertyName) =>
            PresetFields.Contains(propertyName);

        public static void ApplyPresetSelection(EcosystemConfig cfg, string presetCode)
        {
            if (cfg == null || string.IsNullOrWhiteSpace(presetCode)) return;

            cfg.BalancePreset = presetCode;
            if (EcosystemBalancePresets.IsKnownPreset(presetCode))
            {
                EcosystemBalancePresets.Apply(cfg, presetCode);
            }
        }

        public static void MarkCustomIfPresetFieldEdited(EcosystemConfig cfg, string propertyName)
        {
            if (cfg == null || !IsPresetControlledField(propertyName)) return;
            cfg.BalancePreset = EcosystemBalancePresets.Custom;
        }
    }
}
