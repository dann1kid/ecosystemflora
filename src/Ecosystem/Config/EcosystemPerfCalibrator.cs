using System;
using System.Diagnostics;

namespace WildFarming.Ecosystem.Config
{
    /// <summary>
    /// Light synthetic CPU bench (no world / BlockAccessor) → Performance field tiers.
    /// </summary>
    public static class EcosystemPerfCalibrator
    {
        public enum PerfTier
        {
            Weak = 0,
            Balanced = 1,
            Strong = 2,
        }

        public readonly struct CalibrationResult
        {
            public CalibrationResult(PerfTier tier, double opsPerMs, long elapsedMs, int operations)
            {
                Tier = tier;
                OpsPerMs = opsPerMs;
                ElapsedMs = elapsedMs;
                Operations = operations;
            }

            public PerfTier Tier { get; }
            public double OpsPerMs { get; }
            public long ElapsedMs { get; }
            public int Operations { get; }
        }

        public const int DefaultPoolSize = 80000;
        public const int DefaultOperationCount = 400000;
        public const double WeakOpsPerMsCeiling = 80.0;
        public const double BalancedOpsPerMsCeiling = 220.0;

        /// <summary>
        /// Performance (and foliage budget) knobs exposed in the setup-wizard bench table.
        /// Profiling fields are omitted.
        /// </summary>
        public static readonly string[] WizardEditableFields =
        {
            nameof(EcosystemConfig.ReproduceTickIntervalMs),
            nameof(EcosystemConfig.ChunkScanTickIntervalMs),
            nameof(EcosystemConfig.StressTickIntervalMs),
            nameof(EcosystemConfig.EnablePlayerVicinityRescan),
            nameof(EcosystemConfig.PlayerVicinityRescanIntervalMs),
            nameof(EcosystemConfig.EnableCyclicFloraDiscovery),
            nameof(EcosystemConfig.MaxFloraRescanColumnsPerTick),
            nameof(EcosystemConfig.TickBudgetMs),
            nameof(EcosystemConfig.SpreadBudgetMs),
            nameof(EcosystemConfig.RegistrationBudgetMs),
            nameof(EcosystemConfig.StressBudgetMs),
            nameof(EcosystemConfig.PriorityRegistrationBudgetMs),
            nameof(EcosystemConfig.BurstRegistrationBudgetMs),
            nameof(EcosystemConfig.FoliageBudgetMs),
            nameof(EcosystemConfig.FoliageChunkSyncBudgetMs),
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
            nameof(EcosystemConfig.MaxSpreadSolvePending),
            nameof(EcosystemConfig.MaxSpreadSolveCompleted),
            nameof(EcosystemConfig.MaxSpreadSolveDrainPerTick),
            nameof(EcosystemConfig.MaxPendingSpreadIntents),
            nameof(EcosystemConfig.EnableBackgroundRegistrationScan),
            nameof(EcosystemConfig.RegistrationWorkerCount),
            nameof(EcosystemConfig.MaxRegistrationSnapshotCellsPerTick),
            nameof(EcosystemConfig.RegistrationSnapshotBandBelowSurface),
            nameof(EcosystemConfig.MaxRegistrationSolvePending),
            nameof(EcosystemConfig.MaxRegistrationSolveCompleted),
            nameof(EcosystemConfig.MaxRegistrationSolveDrainPerTick),
            nameof(EcosystemConfig.MaxActiveRegistrationSnapshots),
            nameof(EcosystemConfig.MaxChunkColumnsScannedPerTick),
            nameof(EcosystemConfig.MaxRegistrationsPerTick),
            nameof(EcosystemConfig.MaxRegistryAppliesPerTick),
            nameof(EcosystemConfig.MaxRegistryAppliesPerChunkPerTick),
            nameof(EcosystemConfig.EnablePlayerPriorityRegistration),
            nameof(EcosystemConfig.EnableBurstRegistrationNearPlayers),
            nameof(EcosystemConfig.PlayerRegistrationPriorityRadiusBlocks),
            nameof(EcosystemConfig.MaxPriorityChunkScansPerTick),
            nameof(EcosystemConfig.MaxPriorityRegistrationsPerTick),
            nameof(EcosystemConfig.MaxBurstRegistrationsPerChunk),
            nameof(EcosystemConfig.MaxPriorityRegistryAppliesPerTick),
            nameof(EcosystemConfig.OnlyActivateNearPlayers),
        };

        public static CalibrationResult Run(
            int poolSize = DefaultPoolSize,
            int operationCount = DefaultOperationCount)
        {
            if (poolSize < 16) poolSize = 16;
            if (operationCount < 1000) operationCount = 1000;

            var fitness = new float[poolSize];
            var keys = new int[poolSize];
            var queue = new int[Math.Min(4096, poolSize)];
            var rng = new Random(0xE50);
            for (int i = 0; i < poolSize; i++)
            {
                fitness[i] = (float)rng.NextDouble();
                keys[i] = rng.Next();
            }

            var sw = Stopwatch.StartNew();
            double acc = 0;
            int qHead = 0;
            int qTail = 0;
            for (int op = 0; op < operationCount; op++)
            {
                int i = op % poolSize;
                int j = (op * 17 + 3) % poolSize;
                float a = fitness[i];
                float b = fitness[j];
                float score = a * 0.65f + b * 0.35f;
                if (score < 0.45f) score *= 0.5f;

                // Registration / priority-queue spirit: push + occasional drain.
                if ((op & 7) == 0)
                {
                    queue[qTail % queue.Length] = i;
                    qTail++;
                }

                if ((op & 31) == 0 && qHead < qTail)
                {
                    int idx = queue[qHead % queue.Length];
                    qHead++;
                    score += fitness[idx] * 0.05f;
                }

                if ((keys[i] ^ keys[j]) > keys[i])
                {
                    int tmp = keys[i];
                    keys[i] = keys[j];
                    keys[j] = tmp;
                    fitness[i] = score;
                }
                else
                {
                    fitness[j] = Math.Max(fitness[j] * 0.99f, score * 0.25f);
                }

                acc += score;
            }

            sw.Stop();
            if (acc < -1) throw new InvalidOperationException("unreachable");

            long elapsedMs = Math.Max(1, sw.ElapsedMilliseconds);
            double opsPerMs = operationCount / (double)elapsedMs;
            return new CalibrationResult(Classify(opsPerMs), opsPerMs, elapsedMs, operationCount);
        }

        public static PerfTier Classify(double opsPerMs)
        {
            if (opsPerMs < WeakOpsPerMsCeiling) return PerfTier.Weak;
            if (opsPerMs < BalancedOpsPerMsCeiling) return PerfTier.Balanced;
            return PerfTier.Strong;
        }

        public static void ApplyTiers(EcosystemConfig cfg, PerfTier tier)
        {
            if (cfg == null) return;

            switch (tier)
            {
                case PerfTier.Weak:
                    ApplyTierCore(
                        cfg,
                        reproduceMs: 5500, chunkScanMs: 3800, stressMs: 12000, vicinityMs: 9000,
                        tickBudget: 2, spreadBudget: 1, regBudget: 6, stressBudget: 2,
                        priorityBudget: 5, burstBudget: 5, foliageBudget: 1, foliageSyncBudget: 1,
                        attempts: 8, spreadPerChunk: 1, spreadChunks: 6, commits: 8, commitChunks: 6, commitsPerChunk: 1,
                        floraRescan: 3, chunkCols: 8, registrations: 32, applies: 48, appliesPerChunk: 12,
                        snapCells: 220, snapBand: 20, regPending: 4, regDone: 4, regDrain: 2, activeSnaps: 2,
                        spreadPending: 4, spreadDone: 4, spreadDrain: 2, spreadIntents: 8,
                        workers: 1, wakeRadius: 64, priorityRadius: 48,
                        priorityScans: 3, priorityRegs: 160, burstPerChunk: 512, priorityApplies: 48,
                        vicinity: true, cyclicFlora: true, fairSpread: true, twoPhase: true,
                        eventWake: true, seasonWake: true, columnCache: true,
                        bgSpread: true, bgReg: true, priorityReg: true, burstReg: true,
                        onlyNearPlayers: false);
                    break;

                case PerfTier.Strong:
                    ApplyTierCore(
                        cfg,
                        reproduceMs: 2800, chunkScanMs: 1800, stressMs: 7000, vicinityMs: 4000,
                        tickBudget: 5, spreadBudget: 4, regBudget: 12, stressBudget: 4,
                        priorityBudget: 10, burstBudget: 10, foliageBudget: 3, foliageSyncBudget: 2,
                        attempts: 20, spreadPerChunk: 1, spreadChunks: 16, commits: 20, commitChunks: 16, commitsPerChunk: 1,
                        floraRescan: 10, chunkCols: 18, registrations: 72, applies: 110, appliesPerChunk: 28,
                        snapCells: 420, snapBand: 28, regPending: 8, regDone: 8, regDrain: 4, activeSnaps: 4,
                        spreadPending: 8, spreadDone: 8, spreadDrain: 4, spreadIntents: 24,
                        workers: 0, wakeRadius: 0, priorityRadius: 80,
                        priorityScans: 8, priorityRegs: 420, burstPerChunk: 2048, priorityApplies: 110,
                        vicinity: true, cyclicFlora: true, fairSpread: true, twoPhase: true,
                        eventWake: true, seasonWake: true, columnCache: true,
                        bgSpread: true, bgReg: true, priorityReg: true, burstReg: true,
                        onlyNearPlayers: false);
                    break;

                default:
                    ApplyTierCore(
                        cfg,
                        reproduceMs: 3500, chunkScanMs: 2300, stressMs: 8500, vicinityMs: 5000,
                        tickBudget: 2, spreadBudget: 1, regBudget: 9, stressBudget: 0,
                        priorityBudget: 8, burstBudget: 8, foliageBudget: 2, foliageSyncBudget: 2,
                        attempts: 14, spreadPerChunk: 1, spreadChunks: 12, commits: 0, commitChunks: 0, commitsPerChunk: 0,
                        floraRescan: 7, chunkCols: 14, registrations: 54, applies: 85, appliesPerChunk: 21,
                        snapCells: 340, snapBand: 24, regPending: 6, regDone: 6, regDrain: 3, activeSnaps: 3,
                        spreadPending: 6, spreadDone: 6, spreadDrain: 3, spreadIntents: 16,
                        workers: 0, wakeRadius: 0, priorityRadius: 64,
                        priorityScans: 6, priorityRegs: 340, burstPerChunk: 2048, priorityApplies: 85,
                        vicinity: true, cyclicFlora: true, fairSpread: true, twoPhase: true,
                        eventWake: true, seasonWake: true, columnCache: true,
                        bgSpread: true, bgReg: true, priorityReg: true, burstReg: true,
                        onlyNearPlayers: false);
                    break;
            }
        }

        public static void ApplySuperMinimal(EcosystemConfig cfg)
        {
            if (cfg == null) return;
            ApplyTierCore(
                cfg,
                reproduceMs: 10000, chunkScanMs: 7500, stressMs: 20000, vicinityMs: 15000,
                tickBudget: 1, spreadBudget: 1, regBudget: 3, stressBudget: 1,
                priorityBudget: 2, burstBudget: 2, foliageBudget: 1, foliageSyncBudget: 1,
                attempts: 3, spreadPerChunk: 1, spreadChunks: 3, commits: 3, commitChunks: 3, commitsPerChunk: 1,
                floraRescan: 1, chunkCols: 3, registrations: 12, applies: 20, appliesPerChunk: 6,
                snapCells: 120, snapBand: 16, regPending: 2, regDone: 2, regDrain: 1, activeSnaps: 1,
                spreadPending: 2, spreadDone: 2, spreadDrain: 1, spreadIntents: 4,
                workers: 1, wakeRadius: 48, priorityRadius: 32,
                priorityScans: 1, priorityRegs: 48, burstPerChunk: 256, priorityApplies: 24,
                vicinity: true, cyclicFlora: true, fairSpread: true, twoPhase: true,
                eventWake: true, seasonWake: false, columnCache: true,
                bgSpread: true, bgReg: true, priorityReg: true, burstReg: true,
                onlyNearPlayers: true);
        }

        static void ApplyTierCore(
            EcosystemConfig cfg,
            int reproduceMs, int chunkScanMs, int stressMs, int vicinityMs,
            int tickBudget, int spreadBudget, int regBudget, int stressBudget,
            int priorityBudget, int burstBudget, int foliageBudget, int foliageSyncBudget,
            int attempts, int spreadPerChunk, int spreadChunks, int commits, int commitChunks, int commitsPerChunk,
            int floraRescan, int chunkCols, int registrations, int applies, int appliesPerChunk,
            int snapCells, int snapBand, int regPending, int regDone, int regDrain, int activeSnaps,
            int spreadPending, int spreadDone, int spreadDrain, int spreadIntents,
            int workers, int wakeRadius, int priorityRadius,
            int priorityScans, int priorityRegs, int burstPerChunk, int priorityApplies,
            bool vicinity, bool cyclicFlora, bool fairSpread, bool twoPhase,
            bool eventWake, bool seasonWake, bool columnCache,
            bool bgSpread, bool bgReg, bool priorityReg, bool burstReg,
            bool onlyNearPlayers)
        {
            cfg.ReproduceTickIntervalMs = reproduceMs;
            cfg.ChunkScanTickIntervalMs = chunkScanMs;
            cfg.StressTickIntervalMs = stressMs;
            cfg.EnablePlayerVicinityRescan = vicinity;
            cfg.PlayerVicinityRescanIntervalMs = vicinityMs;
            cfg.EnableCyclicFloraDiscovery = cyclicFlora;
            cfg.MaxFloraRescanColumnsPerTick = floraRescan;

            cfg.TickBudgetMs = tickBudget;
            cfg.SpreadBudgetMs = spreadBudget;
            cfg.RegistrationBudgetMs = regBudget;
            cfg.StressBudgetMs = stressBudget;
            cfg.PriorityRegistrationBudgetMs = priorityBudget;
            cfg.BurstRegistrationBudgetMs = burstBudget;
            cfg.FoliageBudgetMs = foliageBudget;
            cfg.FoliageChunkSyncBudgetMs = foliageSyncBudget;

            cfg.MaxReproduceAttemptsPerTick = attempts;
            cfg.EnableChunkFairSpread = fairSpread;
            cfg.MaxSpreadAttemptsPerChunkPerTick = spreadPerChunk;
            cfg.MaxSpreadChunksVisitedPerTick = spreadChunks;
            cfg.EnableTwoPhaseSpreadPlacement = twoPhase;
            cfg.MaxSpreadCommitsPerTick = commits;
            cfg.MaxSpreadCommitChunksVisitedPerTick = commitChunks;
            cfg.MaxSpreadCommitsPerChunkPerTick = commitsPerChunk;

            cfg.EnableEventDrivenSpread = eventWake;
            cfg.EnableSeasonCoarseWake = seasonWake;
            cfg.EcologyWakeRadiusBlocks = wakeRadius;
            cfg.EnableEcologyColumnCache = columnCache;

            cfg.EnableBackgroundSpreadSolve = bgSpread;
            cfg.SpreadWorkerCount = workers;
            cfg.MaxSpreadSolvePending = spreadPending;
            cfg.MaxSpreadSolveCompleted = spreadDone;
            cfg.MaxSpreadSolveDrainPerTick = spreadDrain;
            cfg.MaxPendingSpreadIntents = spreadIntents;

            cfg.EnableBackgroundRegistrationScan = bgReg;
            cfg.RegistrationWorkerCount = workers;
            cfg.MaxRegistrationSnapshotCellsPerTick = snapCells;
            cfg.RegistrationSnapshotBandBelowSurface = snapBand;
            cfg.MaxRegistrationSolvePending = regPending;
            cfg.MaxRegistrationSolveCompleted = regDone;
            cfg.MaxRegistrationSolveDrainPerTick = regDrain;
            cfg.MaxActiveRegistrationSnapshots = activeSnaps;

            cfg.MaxChunkColumnsScannedPerTick = chunkCols;
            cfg.MaxRegistrationsPerTick = registrations;
            cfg.MaxRegistryAppliesPerTick = applies;
            cfg.MaxRegistryAppliesPerChunkPerTick = appliesPerChunk;

            cfg.EnablePlayerPriorityRegistration = priorityReg;
            cfg.EnableBurstRegistrationNearPlayers = burstReg;
            cfg.PlayerRegistrationPriorityRadiusBlocks = priorityRadius;
            cfg.MaxPriorityChunkScansPerTick = priorityScans;
            cfg.MaxPriorityRegistrationsPerTick = priorityRegs;
            cfg.MaxBurstRegistrationsPerChunk = burstPerChunk;
            cfg.MaxPriorityRegistryAppliesPerTick = priorityApplies;

            cfg.OnlyActivateNearPlayers = onlyNearPlayers;
        }

        public static CalibrationResult RunAndApply(EcosystemConfig cfg)
        {
            CalibrationResult result = Run();
            ApplyTiers(cfg, result.Tier);
            RecordResult(cfg, result);
            return result;
        }

        public static void RecordResult(EcosystemConfig cfg, CalibrationResult result)
        {
            if (cfg == null) return;
            cfg.LastAutoTuneTier = result.Tier.ToString();
            cfg.LastAutoTuneOpsPerMs = result.OpsPerMs;
            cfg.LastAutoTuneElapsedMs = (int)Math.Min(int.MaxValue, result.ElapsedMs);
            cfg.LastAutoTuneUtc = DateTime.UtcNow.ToString("o");
        }

        public static void CopyWizardFields(EcosystemConfig source, EcosystemConfig target)
        {
            if (source == null || target == null) return;
            foreach (string name in WizardEditableFields)
            {
                EcosystemConfigFieldDescriptor field = EcosystemConfigSchema.GetField(name);
                if (field == null) continue;
                field.SetValue(target, field.GetValue(source));
            }
        }
    }
}
