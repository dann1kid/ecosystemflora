using System;
using System.Collections.Generic;

namespace WildFarming.Ecosystem.Config
{
    /// <summary>
    /// Keyboard-friendly direction hint for setup-wizard perf knobs (no hover required).
    /// </summary>
    public enum PerfKnobHint
    {
        /// <summary>No clear CPU direction (or mixed).</summary>
        Neutral = 0,

        /// <summary>Higher numeric value → more CPU / work.</summary>
        HigherHeavier = 1,

        /// <summary>Higher numeric value → less CPU (e.g. longer intervals).</summary>
        HigherLighter = 2,

        /// <summary>Switch on → more CPU / work.</summary>
        OnHeavier = 3,

        /// <summary>Switch on → less CPU (scope / skip work).</summary>
        OnLighter = 4,
    }

    /// <summary>Maps wizard perf fields to value/CPU direction markers for Tab-friendly editing.</summary>
    public static class EcosystemPerfKnobHints
    {
        /// <summary>Higher value → higher CPU.</summary>
        public const string MarkerHigherHeavier = "val→ CPU↑";

        /// <summary>Higher value → lower CPU.</summary>
        public const string MarkerHigherLighter = "val→ CPU↓";

        /// <summary>Switch on → higher CPU.</summary>
        public const string MarkerOnHeavier = "on→ CPU↑";

        /// <summary>Switch on → lower CPU.</summary>
        public const string MarkerOnLighter = "on→ CPU↓";

        public const string MarkerNeutral = "·";

        static readonly Dictionary<string, PerfKnobHint> ByField =
            new Dictionary<string, PerfKnobHint>(StringComparer.Ordinal)
            {
                // Longer interval = less frequent work.
                [nameof(EcosystemConfig.ReproduceTickIntervalMs)] = PerfKnobHint.HigherLighter,
                [nameof(EcosystemConfig.ChunkScanTickIntervalMs)] = PerfKnobHint.HigherLighter,
                [nameof(EcosystemConfig.StressTickIntervalMs)] = PerfKnobHint.HigherLighter,
                [nameof(EcosystemConfig.PlayerVicinityRescanIntervalMs)] = PerfKnobHint.HigherLighter,

                [nameof(EcosystemConfig.EnablePlayerVicinityRescan)] = PerfKnobHint.OnHeavier,
                [nameof(EcosystemConfig.EnableCyclicFloraDiscovery)] = PerfKnobHint.OnHeavier,
                [nameof(EcosystemConfig.MaxFloraRescanColumnsPerTick)] = PerfKnobHint.HigherHeavier,

                [nameof(EcosystemConfig.TickBudgetMs)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.SpreadBudgetMs)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.RegistrationBudgetMs)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.StressBudgetMs)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.PriorityRegistrationBudgetMs)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.BurstRegistrationBudgetMs)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.FoliageBudgetMs)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.FoliageChunkSyncBudgetMs)] = PerfKnobHint.HigherHeavier,

                [nameof(EcosystemConfig.MaxReproduceAttemptsPerTick)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.EnableChunkFairSpread)] = PerfKnobHint.Neutral,
                [nameof(EcosystemConfig.MaxSpreadAttemptsPerChunkPerTick)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.MaxSpreadChunksVisitedPerTick)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.EnableTwoPhaseSpreadPlacement)] = PerfKnobHint.Neutral,
                [nameof(EcosystemConfig.MaxSpreadCommitsPerTick)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.MaxSpreadCommitChunksVisitedPerTick)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.MaxSpreadCommitsPerChunkPerTick)] = PerfKnobHint.HigherHeavier,

                [nameof(EcosystemConfig.EnableEventDrivenSpread)] = PerfKnobHint.OnLighter,
                [nameof(EcosystemConfig.EnableSeasonCoarseWake)] = PerfKnobHint.OnLighter,
                [nameof(EcosystemConfig.EcologyWakeRadiusBlocks)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.EnableEcologyColumnCache)] = PerfKnobHint.OnLighter,

                [nameof(EcosystemConfig.EnableBackgroundSpreadSolve)] = PerfKnobHint.OnHeavier,
                [nameof(EcosystemConfig.SpreadWorkerCount)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.MaxSpreadSolvePending)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.MaxSpreadSolveCompleted)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.MaxSpreadSolveDrainPerTick)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.MaxPendingSpreadIntents)] = PerfKnobHint.HigherHeavier,

                [nameof(EcosystemConfig.EnableBackgroundRegistrationScan)] = PerfKnobHint.OnHeavier,
                [nameof(EcosystemConfig.RegistrationWorkerCount)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.MaxRegistrationSnapshotCellsPerTick)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.RegistrationSnapshotBandBelowSurface)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.MaxRegistrationSolvePending)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.MaxRegistrationSolveCompleted)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.MaxRegistrationSolveDrainPerTick)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.MaxActiveRegistrationSnapshots)] = PerfKnobHint.HigherHeavier,

                [nameof(EcosystemConfig.MaxChunkColumnsScannedPerTick)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.MaxRegistrationsPerTick)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.MaxRegistryAppliesPerTick)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.MaxRegistryAppliesPerChunkPerTick)] = PerfKnobHint.HigherHeavier,

                [nameof(EcosystemConfig.EnablePlayerPriorityRegistration)] = PerfKnobHint.OnHeavier,
                [nameof(EcosystemConfig.EnableBurstRegistrationNearPlayers)] = PerfKnobHint.OnHeavier,
                [nameof(EcosystemConfig.PlayerRegistrationPriorityRadiusBlocks)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.MaxPriorityChunkScansPerTick)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.MaxPriorityRegistrationsPerTick)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.MaxBurstRegistrationsPerChunk)] = PerfKnobHint.HigherHeavier,
                [nameof(EcosystemConfig.MaxPriorityRegistryAppliesPerTick)] = PerfKnobHint.HigherHeavier,

                // Scope: on = skip distant work.
                [nameof(EcosystemConfig.OnlyActivateNearPlayers)] = PerfKnobHint.OnLighter,
            };

        public static PerfKnobHint Get(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName)) return PerfKnobHint.Neutral;
            return ByField.TryGetValue(fieldName, out PerfKnobHint hint) ? hint : PerfKnobHint.Neutral;
        }

        public static string Marker(PerfKnobHint hint)
        {
            switch (hint)
            {
                case PerfKnobHint.HigherHeavier: return MarkerHigherHeavier;
                case PerfKnobHint.HigherLighter: return MarkerHigherLighter;
                case PerfKnobHint.OnHeavier: return MarkerOnHeavier;
                case PerfKnobHint.OnLighter: return MarkerOnLighter;
                default: return MarkerNeutral;
            }
        }

        public static string MarkerForField(string fieldName) => Marker(Get(fieldName));

        /// <summary>Title with a short direction suffix for Tab users (e.g. "Tick budget  val→ CPU↑").</summary>
        public static string FormatTitleWithHint(string title, string fieldName)
        {
            string marker = MarkerForField(fieldName);
            if (string.IsNullOrEmpty(title)) return marker;
            if (marker == MarkerNeutral) return title;
            return title + "  " + marker;
        }
    }
}
