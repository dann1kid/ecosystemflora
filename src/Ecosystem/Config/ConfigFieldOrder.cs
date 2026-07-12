using System;
using System.Collections.Generic;

namespace WildFarming.Ecosystem.Config
{
    /// <summary>
    /// Logical category and display order for config UI (and schema sorting).
    /// Fields not listed keep inferred category and sort after listed fields (by name).
    /// </summary>
    internal static class ConfigFieldOrder
    {
        static readonly Dictionary<string, string> CategoryOverrides = new(StringComparer.Ordinal)
        {
            [nameof(EcosystemConfig.MaxFailedSurvivalChecks)] = "stress",
            [nameof(EcosystemConfig.StaggerReproduceAttempts)] = "spread",
            [nameof(EcosystemConfig.ReproduceDebug)] = "advanced",
            [nameof(EcosystemConfig.ReproduceTickIntervalMs)] = "perf",
            [nameof(EcosystemConfig.ReproduceTickProfilingMinRegistry)] = "perf",
            [nameof(EcosystemConfig.ReproduceTickProfilingIntervalMs)] = "perf",
            [nameof(EcosystemConfig.EnableReproduceTickProfiling)] = "perf",
            [nameof(EcosystemConfig.EnableEcologyInspect)] = "master",
            [nameof(EcosystemConfig.EcologyInspectCooldownSeconds)] = "master",
            [nameof(EcosystemConfig.EcologyInspectScanRadius)] = "master",
            [nameof(EcosystemConfig.EnableEcologyAreaScan)] = "master",
            [nameof(EcosystemConfig.CloneBerryTraits)] = "spread",
            [nameof(EcosystemConfig.BerryTraitMutationChance)] = "spread",
        };

        static readonly string[] MasterFields =
        {
            nameof(EcosystemConfig.BalancePreset),
            nameof(EcosystemConfig.EcosystemEnabled),
            nameof(EcosystemConfig.HarshWildPlants),
            nameof(EcosystemConfig.ApplyWorldgenRainForest),
            nameof(EcosystemConfig.EnableThirdPartyParticipants),
            nameof(EcosystemConfig.EnableEcologyInspect),
            nameof(EcosystemConfig.EcologyInspectCooldownSeconds),
            nameof(EcosystemConfig.EcologyInspectScanRadius),
            nameof(EcosystemConfig.EnableEcologyAreaScan),
            nameof(EcosystemConfig.EnableEcologyHistoryHint),
        };

        static readonly string[] SpreadFields =
        {
            nameof(EcosystemConfig.ReproduceRadius),
            nameof(EcosystemConfig.ReproduceVerticalSearch),
            nameof(EcosystemConfig.ReproduceAttemptsPerYear),
            nameof(EcosystemConfig.ReproduceIntervalHours),
            nameof(EcosystemConfig.UseCalendarScaledSpread),
            nameof(EcosystemConfig.UseSpeciesSpreadRates),
            nameof(EcosystemConfig.SpeciesSpreadRateScale),
            nameof(EcosystemConfig.MinSpeciesReproduceIntervalDays),
            nameof(EcosystemConfig.MinSpeciesReproduceIntervalHours),
            nameof(EcosystemConfig.ReproduceChance),
            nameof(EcosystemConfig.MinFitness),
            nameof(EcosystemConfig.GrowthHoursMultiplier),
            nameof(EcosystemConfig.StaggerReproduceAttempts),
            nameof(EcosystemConfig.EventWakeRetryHours),
            nameof(EcosystemConfig.EnableCyclicFloraDiscovery),
            nameof(EcosystemConfig.MaxFloraRescanColumnsPerTick),
            nameof(EcosystemConfig.EnableFlowerSpreadMaturation),
            nameof(EcosystemConfig.MaxPendingFlowerMaturationChecksPerTick),
            nameof(EcosystemConfig.EnableFlowerSpreadAttemptCooldown),
            nameof(EcosystemConfig.FlowerSpreadCooldownHoursMultiplier),
            nameof(EcosystemConfig.EnableFlowerPhenology),
            nameof(EcosystemConfig.FlowerBloomMinTemperature),
            nameof(EcosystemConfig.FlowerBloomMaxTemperature),
            nameof(EcosystemConfig.FlowerBloomEnergyThreshold),
            nameof(EcosystemConfig.FlowerPhenologyEnergyGainPerDay),
            nameof(EcosystemConfig.MaxFlowerPhenologyChecksPerTick),
            nameof(EcosystemConfig.EnableTallgrassSpreadMaturation),
            nameof(EcosystemConfig.MaxPendingTallgrassPromotionChecksPerTick),
            nameof(EcosystemConfig.EnableTallgrassPhenology),
            nameof(EcosystemConfig.MaxTallgrassPhenologyChecksPerTick),
            nameof(EcosystemConfig.EnableFernRhizomeSpread),
            nameof(EcosystemConfig.EnableFernSpreadMaturation),
            nameof(EcosystemConfig.MaxPendingFernMaturationChecksPerTick),
            nameof(EcosystemConfig.EnableFernSpreadAttemptCooldown),
            nameof(EcosystemConfig.FernSpreadCooldownHoursMultiplier),
            nameof(EcosystemConfig.EnableFernSporulationGate),
            nameof(EcosystemConfig.EnableFernPhenology),
            nameof(EcosystemConfig.MaxFernPhenologyChecksPerTick),
            nameof(EcosystemConfig.EnableBerryColonySpread),
            nameof(EcosystemConfig.EnableShoreSedgeMatSpread),
            nameof(EcosystemConfig.EnableBerrySpreadMaturation),
            nameof(EcosystemConfig.MaxPendingBerryMaturationChecksPerTick),
            nameof(EcosystemConfig.CloneBerryTraits),
            nameof(EcosystemConfig.BerryTraitMutationChance),
        };

        static readonly string[] AquaticFields =
        {
            nameof(EcosystemConfig.UseRhizomeSpreadForReeds),
            nameof(EcosystemConfig.RhizomeSeedDispersalEnabled),
            nameof(EcosystemConfig.RhizomeSeedDispersalChanceScale),
            nameof(EcosystemConfig.RhizomeSeedDispersalFitnessScale),
            nameof(EcosystemConfig.UseSurfaceMatSpreadForLilies),
        };

        static readonly string[] CompetitionFields =
        {
            nameof(EcosystemConfig.PlantSpacingEnabled),
            nameof(EcosystemConfig.DefaultSameSpeciesSpacing),
            nameof(EcosystemConfig.DefaultOtherSpeciesSpacing),
            nameof(EcosystemConfig.ApplyCrossHabitatSpacing),
            nameof(EcosystemConfig.SpacingVerticalSearch),
            nameof(EcosystemConfig.UseCellDisplacement),
            nameof(EcosystemConfig.DisplacementHoldMargin),
            nameof(EcosystemConfig.PreferSpreadToEmptyCells),
            nameof(EcosystemConfig.EnableEmptyFirstSpreadCollect),
            nameof(EcosystemConfig.EnableSpreadColumnOccupancyHint),
            nameof(EcosystemConfig.EmptySpreadFitnessMultiplier),
            nameof(EcosystemConfig.UseFloraContext),
            nameof(EcosystemConfig.FloraContextNeighborRadius),
            nameof(EcosystemConfig.FloraContextInteriorThreshold),
            nameof(EcosystemConfig.FloraOpenInteriorPenalty),
            nameof(EcosystemConfig.FloraContextCacheHours),
            nameof(EcosystemConfig.UseNicheContext),
            nameof(EcosystemConfig.NicheCacheHours),
            nameof(EcosystemConfig.NicheStressThreshold),
        };

        static readonly string[] StressFields =
        {
            nameof(EcosystemConfig.EnableStressDeath),
            nameof(EcosystemConfig.StressRecheckHours),
            nameof(EcosystemConfig.MaxFailedSurvivalChecks),
            nameof(EcosystemConfig.MaxStressChecksPerTick),
            nameof(EcosystemConfig.UseSeasonalEcology),
            nameof(EcosystemConfig.SeasonalStressEnabled),
            nameof(EcosystemConfig.EnableSymbiosis),
            nameof(EcosystemConfig.SymbiosisCascadeRadius),
            nameof(EcosystemConfig.EnableTrampling),
            nameof(EcosystemConfig.TramplingRadius),
            nameof(EcosystemConfig.TramplingStressThreshold),
            nameof(EcosystemConfig.TramplingSoilDegradation),
        };

        static readonly string[] TreeFields =
        {
            nameof(EcosystemConfig.EnableTreeAging),
            nameof(EcosystemConfig.TreeMinSpreadAgeYears),
            nameof(EcosystemConfig.TreeYoungSpreadBypassTrunkHeight),
            nameof(EcosystemConfig.MaxTreeGrowthAttemptsPerTick),
            nameof(EcosystemConfig.MaxTreeGrowthCatchUpYearsPerTick),
            nameof(EcosystemConfig.TreeGrowthActivityScale),
            nameof(EcosystemConfig.EnableTreeSenescence),
            nameof(EcosystemConfig.TreeSenescenceSnagBlocks),
            nameof(EcosystemConfig.EnableTreeSenescenceRemains),
            nameof(EcosystemConfig.TreeSenescenceFallenLogCount),
            nameof(EcosystemConfig.EnableTreeSeralSuccession),
            nameof(EcosystemConfig.EnableCyclicTreeDiscovery),
            nameof(EcosystemConfig.MaxTreeRescanColumnsPerTick),
            nameof(EcosystemConfig.MaxPendingTreeChecksPerTick),
            nameof(EcosystemConfig.EnableStumpDecay),
            nameof(EcosystemConfig.StumpDecayYears),
            nameof(EcosystemConfig.MaxStumpDecayChecksPerTick),
            nameof(EcosystemConfig.EnableFerntreeEcology),
            nameof(EcosystemConfig.FerntreeSenescenceSnagSegments),
            nameof(EcosystemConfig.EnableWildVineEcology),
            nameof(EcosystemConfig.WildVineWallCaptureRadius),
            nameof(EcosystemConfig.WildVineWallCaptureHeight),
            nameof(EcosystemConfig.WildVineMaxHangDepth),
        };

        static readonly string[] CanopyFields =
        {
            nameof(EcosystemConfig.EnableSeasonalFoliage),
            nameof(EcosystemConfig.FoliageSyncMode),
            nameof(EcosystemConfig.FoliageCatchUpOnChunkLoad),
            nameof(EcosystemConfig.FoliageChunkWorkPerTick),
            nameof(EcosystemConfig.FoliageChunkSyncBudgetMs),
            nameof(EcosystemConfig.MaxFoliageCatchUpPerChunk),
            nameof(EcosystemConfig.FoliageColumnScanHeightAboveSurface),
            nameof(EcosystemConfig.MaxFoliageCellsTickedPerTick),
            nameof(EcosystemConfig.FoliageBudgetMs),
            nameof(EcosystemConfig.CanopyActivityScale),
            nameof(EcosystemConfig.CanopyBudMinTemperature),
            nameof(EcosystemConfig.CanopyLatitudeInfluence),
            nameof(EcosystemConfig.FoliagePeakAutumnBranchyStripActivity),
            nameof(EcosystemConfig.EnableCanopyFallenSticks),
            nameof(EcosystemConfig.CanopyFallenStickChance),
            nameof(EcosystemConfig.EnableSpringBranchyAgeBoost),
            nameof(EcosystemConfig.SpringBranchyAgeBoostYearsToMax),
            nameof(EcosystemConfig.SpringBranchyAgeBoostMax),
            nameof(EcosystemConfig.FoliageRestoreBareSkeleton),
            nameof(EcosystemConfig.EnableOrphanFoliagePrune),
            nameof(EcosystemConfig.OrphanFoliageMaxBfsDepth),
            nameof(EcosystemConfig.OrphanFoliageMaxChecksPerChunkPass),
            nameof(EcosystemConfig.OrphanFoliageFireChunkHours),
            nameof(EcosystemConfig.EnableCanopyAmbience),
            nameof(EcosystemConfig.CanopyAmbienceMinHeightBlocks),
            nameof(EcosystemConfig.CanopyAmbienceMoteRate),
            nameof(EcosystemConfig.CanopyAmbienceLeafDriftRate),
            nameof(EcosystemConfig.CanopyAmbienceSampleIntervalSeconds),
            nameof(EcosystemConfig.CanopyAmbienceSuppressInRain),
        };

        static readonly string[] MyceliumFields =
        {
            nameof(EcosystemConfig.EnableMyceliumEcology),
            nameof(EcosystemConfig.EnableMyceliumNiche),
            nameof(EcosystemConfig.MyceliumZoneRadius),
            nameof(EcosystemConfig.MyceliumMeadowSpreadPenalty),
            nameof(EcosystemConfig.MyceliumForestSpreadBonus),
            nameof(EcosystemConfig.MyceliumSkipSoilSuccession),
            nameof(EcosystemConfig.EnableMyceliumCapDisplacement),
            nameof(EcosystemConfig.MyceliumTreeHostRadius),
            nameof(EcosystemConfig.MyceliumForestMinForestCover),
            nameof(EcosystemConfig.MyceliumMeadowMaxForestCover),
            nameof(EcosystemConfig.EnableMyceliumNetworkSpread),
            nameof(EcosystemConfig.MyceliumSpreadRate),
            nameof(EcosystemConfig.MyceliumSpreadAttemptsPerYear),
            nameof(EcosystemConfig.MyceliumSpreadMinFitness),
        };

        static readonly string[] SoilFields =
        {
            nameof(EcosystemConfig.UseSoilSuccession),
            nameof(EcosystemConfig.SoilSuccessionStrength),
            nameof(EcosystemConfig.SoilSuccessionSkipWhenBuiltAbove),
            nameof(EcosystemConfig.UseFarmlandNutrientBridge),
            nameof(EcosystemConfig.FarmlandNutrientBridgeStrength),
            nameof(EcosystemConfig.EnableFallowRestoration),
            nameof(EcosystemConfig.FallowRestorationStrength),
        };

        static readonly string[] HarvestFields =
        {
            nameof(EcosystemConfig.EnableFlowerDrygrass),
        };

        static readonly string[] ScopeFields =
        {
            nameof(EcosystemConfig.RespectLandClaims),
            nameof(EcosystemConfig.OnlyActivateNearPlayers),
            nameof(EcosystemConfig.LimitSpreadNearPlayers),
            nameof(EcosystemConfig.PlayerActivationRadiusBlocks),
        };

        static readonly string[] PerfFields =
        {
            nameof(EcosystemConfig.ReproduceTickIntervalMs),
            nameof(EcosystemConfig.ChunkScanTickIntervalMs),
            nameof(EcosystemConfig.StressTickIntervalMs),
            nameof(EcosystemConfig.TickBudgetMs),
            nameof(EcosystemConfig.SpreadBudgetMs),
            nameof(EcosystemConfig.RegistrationBudgetMs),
            nameof(EcosystemConfig.StressBudgetMs),
            nameof(EcosystemConfig.MaxReproduceAttemptsPerTick),
            nameof(EcosystemConfig.EnableChunkFairSpread),
            nameof(EcosystemConfig.MaxSpreadAttemptsPerChunkPerTick),
            nameof(EcosystemConfig.MaxSpreadChunksVisitedPerTick),
            nameof(EcosystemConfig.EnableTwoPhaseSpreadPlacement),
            nameof(EcosystemConfig.MaxSpreadCommitsPerTick),
            nameof(EcosystemConfig.MaxSpreadCommitChunksVisitedPerTick),
            nameof(EcosystemConfig.MaxSpreadCommitsPerChunkPerTick),
            nameof(EcosystemConfig.EnableEventDrivenSpread),
            nameof(EcosystemConfig.EnableSeasonCoarseWake),
            nameof(EcosystemConfig.EcologyWakeRadiusBlocks),
            nameof(EcosystemConfig.EnableEcologyColumnCache),
            nameof(EcosystemConfig.EnableBackgroundSpreadSolve),
            nameof(EcosystemConfig.SpreadWorkerCount),
            nameof(EcosystemConfig.EnableBackgroundRegistrationScan),
            nameof(EcosystemConfig.RegistrationWorkerCount),
            nameof(EcosystemConfig.MaxRegistrationSnapshotCellsPerTick),
            nameof(EcosystemConfig.MaxChunkColumnsScannedPerTick),
            nameof(EcosystemConfig.MaxRegistrationsPerTick),
            nameof(EcosystemConfig.MaxRegistryAppliesPerTick),
            nameof(EcosystemConfig.MaxRegistryAppliesPerChunkPerTick),
            nameof(EcosystemConfig.EnablePlayerPriorityRegistration),
            nameof(EcosystemConfig.EnableBurstRegistrationNearPlayers),
            nameof(EcosystemConfig.PlayerRegistrationPriorityRadiusBlocks),
            nameof(EcosystemConfig.MaxPriorityChunkScansPerTick),
            nameof(EcosystemConfig.MaxPriorityRegistrationsPerTick),
            nameof(EcosystemConfig.PriorityRegistrationBudgetMs),
            nameof(EcosystemConfig.BurstRegistrationBudgetMs),
            nameof(EcosystemConfig.MaxBurstRegistrationsPerChunk),
            nameof(EcosystemConfig.MaxPriorityRegistryAppliesPerTick),
            nameof(EcosystemConfig.EnableReproduceTickProfiling),
            nameof(EcosystemConfig.ReproduceTickProfilingMinRegistry),
            nameof(EcosystemConfig.ReproduceTickProfilingIntervalMs),
        };

        static readonly string[] AdvancedFields =
        {
            nameof(EcosystemConfig.ReproduceDebug),
            nameof(EcosystemConfig.VerboseLogging),
        };

        static readonly string[][] CategoryFieldSequences =
        {
            MasterFields,
            SpreadFields,
            AquaticFields,
            CompetitionFields,
            StressFields,
            TreeFields,
            CanopyFields,
            MyceliumFields,
            SoilFields,
            HarvestFields,
            ScopeFields,
            PerfFields,
            AdvancedFields,
        };

        static readonly Dictionary<string, int> OrderByName = BuildOrderMap();
        static readonly Dictionary<string, string> CategoryByName = BuildCategoryMap();

        public static bool TryGetOrder(string fieldName, out int order)
        {
            return OrderByName.TryGetValue(fieldName, out order);
        }

        public static bool TryGetCategory(string fieldName, out string category)
        {
            return CategoryByName.TryGetValue(fieldName, out category);
        }

        static Dictionary<string, int> BuildOrderMap()
        {
            var map = new Dictionary<string, int>(StringComparer.Ordinal);
            int order = 0;
            foreach (string[] sequence in CategoryFieldSequences)
            {
                foreach (string name in sequence)
                {
                    map[name] = order;
                    order += 10;
                }
            }

            return map;
        }

        static Dictionary<string, string> BuildCategoryMap()
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string[] sequence in CategoryFieldSequences)
            {
                string category = SequenceCategory(sequence);
                foreach (string name in sequence)
                {
                    map[name] = category;
                }
            }

            foreach (KeyValuePair<string, string> pair in CategoryOverrides)
            {
                map[pair.Key] = pair.Value;
            }

            return map;
        }

        static string SequenceCategory(string[] sequence)
        {
            if (sequence == MasterFields) return "master";
            if (sequence == SpreadFields) return "spread";
            if (sequence == AquaticFields) return "aquatic";
            if (sequence == CompetitionFields) return "competition";
            if (sequence == StressFields) return "stress";
            if (sequence == TreeFields) return "trees";
            if (sequence == CanopyFields) return "canopy";
            if (sequence == MyceliumFields) return "mycelium";
            if (sequence == SoilFields) return "soil";
            if (sequence == HarvestFields) return "harvest";
            if (sequence == ScopeFields) return "scope";
            if (sequence == PerfFields) return "perf";
            if (sequence == AdvancedFields) return "advanced";
            return "advanced";
        }
    }
}
