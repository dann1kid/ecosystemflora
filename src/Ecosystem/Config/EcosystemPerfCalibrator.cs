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

        /// <summary>Synthetic plant pool size (memory + scoring work).</summary>
        public const int DefaultPoolSize = 80000;

        /// <summary>Fixed operation count for timing (deterministic workload).</summary>
        public const int DefaultOperationCount = 400000;

        /// <summary>ops/ms below this → Weak.</summary>
        public const double WeakOpsPerMsCeiling = 80.0;

        /// <summary>ops/ms below this (and ≥ Weak ceiling) → Balanced; else Strong.</summary>
        public const double BalancedOpsPerMsCeiling = 220.0;

        public static CalibrationResult Run(
            int poolSize = DefaultPoolSize,
            int operationCount = DefaultOperationCount)
        {
            if (poolSize < 16) poolSize = 16;
            if (operationCount < 1000) operationCount = 1000;

            var fitness = new float[poolSize];
            var keys = new int[poolSize];
            var rng = new Random(0xE50);
            for (int i = 0; i < poolSize; i++)
            {
                fitness[i] = (float)rng.NextDouble();
                keys[i] = rng.Next();
            }

            var sw = Stopwatch.StartNew();
            double acc = 0;
            for (int op = 0; op < operationCount; op++)
            {
                int i = op % poolSize;
                int j = (op * 17 + 3) % poolSize;
                float a = fitness[i];
                float b = fitness[j];
                float score = a * 0.65f + b * 0.35f;
                if (score < 0.45f) score *= 0.5f;
                // Cheap heap-like swap / key mix (registration / spread queue spirit).
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
            // Prevent dead-code elimination of acc.
            if (acc < -1)
            {
                throw new InvalidOperationException("unreachable");
            }

            long elapsedMs = Math.Max(1, sw.ElapsedMilliseconds);
            double opsPerMs = operationCount / (double)elapsedMs;
            PerfTier tier = Classify(opsPerMs);
            return new CalibrationResult(tier, opsPerMs, elapsedMs, operationCount);
        }

        public static PerfTier Classify(double opsPerMs)
        {
            if (opsPerMs < WeakOpsPerMsCeiling) return PerfTier.Weak;
            if (opsPerMs < BalancedOpsPerMsCeiling) return PerfTier.Balanced;
            return PerfTier.Strong;
        }

        /// <summary>Apply Performance-only knobs for a tier. Does not change ecology balance fields.</summary>
        public static void ApplyTier(EcosystemConfig cfg, PerfTier tier)
        {
            if (cfg == null) return;

            switch (tier)
            {
                case PerfTier.Weak:
                    cfg.ReproduceTickIntervalMs = 5500;
                    cfg.ChunkScanTickIntervalMs = 3800;
                    cfg.StressTickIntervalMs = 12000;
                    cfg.TickBudgetMs = 2;
                    cfg.SpreadBudgetMs = 1;
                    cfg.RegistrationBudgetMs = 6;
                    cfg.PriorityRegistrationBudgetMs = 5;
                    cfg.BurstRegistrationBudgetMs = 5;
                    cfg.FoliageBudgetMs = 1;
                    cfg.FoliageChunkSyncBudgetMs = 1;
                    cfg.MaxReproduceAttemptsPerTick = 8;
                    cfg.MaxSpreadAttemptsPerChunkPerTick = 1;
                    cfg.MaxSpreadChunksVisitedPerTick = 6;
                    cfg.MaxFloraRescanColumnsPerTick = 3;
                    cfg.MaxChunkColumnsScannedPerTick = 8;
                    cfg.MaxRegistrationsPerTick = 32;
                    cfg.MaxRegistryAppliesPerTick = 48;
                    cfg.EnablePlayerVicinityRescan = true;
                    cfg.PlayerVicinityRescanIntervalMs = 9000;
                    cfg.MaxPriorityChunkScansPerTick = 3;
                    cfg.MaxPriorityRegistrationsPerTick = 160;
                    break;

                case PerfTier.Strong:
                    cfg.ReproduceTickIntervalMs = 2800;
                    cfg.ChunkScanTickIntervalMs = 1800;
                    cfg.StressTickIntervalMs = 7000;
                    cfg.TickBudgetMs = 5;
                    cfg.SpreadBudgetMs = 4;
                    cfg.RegistrationBudgetMs = 12;
                    cfg.PriorityRegistrationBudgetMs = 10;
                    cfg.BurstRegistrationBudgetMs = 10;
                    cfg.FoliageBudgetMs = 3;
                    cfg.FoliageChunkSyncBudgetMs = 2;
                    cfg.MaxReproduceAttemptsPerTick = 20;
                    cfg.MaxSpreadAttemptsPerChunkPerTick = 1;
                    cfg.MaxSpreadChunksVisitedPerTick = 16;
                    cfg.MaxFloraRescanColumnsPerTick = 10;
                    cfg.MaxChunkColumnsScannedPerTick = 18;
                    cfg.MaxRegistrationsPerTick = 72;
                    cfg.MaxRegistryAppliesPerTick = 110;
                    cfg.EnablePlayerVicinityRescan = true;
                    cfg.PlayerVicinityRescanIntervalMs = 4000;
                    cfg.MaxPriorityChunkScansPerTick = 8;
                    cfg.MaxPriorityRegistrationsPerTick = 420;
                    break;

                default:
                    cfg.ReproduceTickIntervalMs = 3500;
                    cfg.ChunkScanTickIntervalMs = 2300;
                    cfg.StressTickIntervalMs = 8500;
                    cfg.TickBudgetMs = 2;
                    cfg.SpreadBudgetMs = 1;
                    cfg.RegistrationBudgetMs = 9;
                    cfg.PriorityRegistrationBudgetMs = 8;
                    cfg.BurstRegistrationBudgetMs = 8;
                    cfg.FoliageBudgetMs = 2;
                    cfg.FoliageChunkSyncBudgetMs = 2;
                    cfg.MaxReproduceAttemptsPerTick = 14;
                    cfg.MaxSpreadAttemptsPerChunkPerTick = 1;
                    cfg.MaxSpreadChunksVisitedPerTick = 12;
                    cfg.MaxFloraRescanColumnsPerTick = 7;
                    cfg.MaxChunkColumnsScannedPerTick = 14;
                    cfg.MaxRegistrationsPerTick = 54;
                    cfg.MaxRegistryAppliesPerTick = 85;
                    cfg.EnablePlayerVicinityRescan = true;
                    cfg.PlayerVicinityRescanIntervalMs = 5000;
                    cfg.MaxPriorityChunkScansPerTick = 6;
                    cfg.MaxPriorityRegistrationsPerTick = 340;
                    break;
            }
        }

        /// <summary>
        /// Ultra-low CPU profile for very weak machines — slower/rarer than <see cref="PerfTier.Weak"/>.
        /// Also enables near-player-only activation.
        /// </summary>
        public static void ApplySuperMinimal(EcosystemConfig cfg)
        {
            if (cfg == null) return;

            cfg.ReproduceTickIntervalMs = 10000;
            cfg.ChunkScanTickIntervalMs = 7500;
            cfg.StressTickIntervalMs = 20000;
            cfg.TickBudgetMs = 1;
            cfg.SpreadBudgetMs = 1;
            cfg.RegistrationBudgetMs = 3;
            cfg.PriorityRegistrationBudgetMs = 2;
            cfg.BurstRegistrationBudgetMs = 2;
            cfg.FoliageBudgetMs = 1;
            cfg.FoliageChunkSyncBudgetMs = 1;
            cfg.MaxReproduceAttemptsPerTick = 3;
            cfg.MaxSpreadAttemptsPerChunkPerTick = 1;
            cfg.MaxSpreadChunksVisitedPerTick = 3;
            cfg.MaxFloraRescanColumnsPerTick = 1;
            cfg.MaxChunkColumnsScannedPerTick = 3;
            cfg.MaxRegistrationsPerTick = 12;
            cfg.MaxRegistryAppliesPerTick = 20;
            cfg.EnablePlayerVicinityRescan = true;
            cfg.PlayerVicinityRescanIntervalMs = 15000;
            cfg.MaxPriorityChunkScansPerTick = 1;
            cfg.MaxPriorityRegistrationsPerTick = 48;
            cfg.MaxPriorityRegistryAppliesPerTick = 24;
            cfg.OnlyActivateNearPlayers = true;
        }

        public static CalibrationResult RunAndApply(EcosystemConfig cfg)
        {
            CalibrationResult result = Run();
            ApplyTier(cfg, result.Tier);
            RecordResult(cfg, result);
            return result;
        }

        /// <summary>Persist bench metadata on the world config (does not change Performance knobs).</summary>
        public static void RecordResult(EcosystemConfig cfg, CalibrationResult result)
        {
            if (cfg == null) return;
            cfg.LastAutoTuneTier = result.Tier.ToString();
            cfg.LastAutoTuneOpsPerMs = result.OpsPerMs;
            cfg.LastAutoTuneElapsedMs = (int)Math.Min(int.MaxValue, result.ElapsedMs);
            cfg.LastAutoTuneUtc = DateTime.UtcNow.ToString("o");
        }

        /// <summary>Key Performance fields the setup wizard exposes for post-bench editing.</summary>
        public static readonly string[] WizardEditableFields =
        {
            nameof(EcosystemConfig.ReproduceTickIntervalMs),
            nameof(EcosystemConfig.ChunkScanTickIntervalMs),
            nameof(EcosystemConfig.StressTickIntervalMs),
            nameof(EcosystemConfig.PlayerVicinityRescanIntervalMs),
            nameof(EcosystemConfig.TickBudgetMs),
            nameof(EcosystemConfig.SpreadBudgetMs),
            nameof(EcosystemConfig.RegistrationBudgetMs),
            nameof(EcosystemConfig.MaxReproduceAttemptsPerTick),
            nameof(EcosystemConfig.MaxFloraRescanColumnsPerTick),
            nameof(EcosystemConfig.MaxSpreadChunksVisitedPerTick),
            nameof(EcosystemConfig.MaxChunkColumnsScannedPerTick),
        };
    }
}
