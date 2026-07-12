using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// One-time conversion of registration throughput keys from legacy absolute totals
    /// to per-worker values (effective = stored × worker count).
    /// </summary>
    internal static class RegistrationBudgetMigration
    {
        delegate int Getter(EcosystemConfig cfg);
        delegate void Setter(EcosystemConfig cfg, int value);

        readonly struct Field
        {
            public readonly Getter Get;
            public readonly Setter Set;
            public readonly int PerWorkerDefault;

            public Field(Getter get, Setter set, int perWorkerDefault)
            {
                Get = get;
                Set = set;
                PerWorkerDefault = perWorkerDefault;
            }
        }

        static readonly Field[] ThroughputFields =
        {
            new Field(c => c.MaxChunkColumnsScannedPerTick, (c, v) => c.MaxChunkColumnsScannedPerTick = v, 64),
            new Field(c => c.MaxRegistrationsPerTick, (c, v) => c.MaxRegistrationsPerTick = v, 256),
            new Field(c => c.MaxPriorityChunkScansPerTick, (c, v) => c.MaxPriorityChunkScansPerTick = v, 24),
            new Field(c => c.MaxPriorityRegistrationsPerTick, (c, v) => c.MaxPriorityRegistrationsPerTick = v, 2048),
            new Field(c => c.MaxBurstRegistrationsPerChunk, (c, v) => c.MaxBurstRegistrationsPerChunk = v, 2048),
            new Field(c => c.MaxRegistryAppliesPerTick, (c, v) => c.MaxRegistryAppliesPerTick = v, 512),
            new Field(c => c.MaxRegistryAppliesPerChunkPerTick, (c, v) => c.MaxRegistryAppliesPerChunkPerTick = v, 96),
            new Field(c => c.MaxPriorityRegistryAppliesPerTick, (c, v) => c.MaxPriorityRegistryAppliesPerTick = v, 512),
            new Field(c => c.MaxRegistrationSnapshotCellsPerTick, (c, v) => c.MaxRegistrationSnapshotCellsPerTick = v, 4096),
        };

        /// <returns>True when values were divided (config should be rewritten).</returns>
        public static bool ApplyIfNeeded(EcosystemConfig cfg, ICoreAPI api, bool configFileExisted)
        {
            if (cfg == null || !configFileExisted) return false;

            if (cfg.RegistrationBudgetPerWorkerMigrated)
            {
                return false;
            }

            if (LooksAlreadyPerWorker(cfg))
            {
                cfg.RegistrationBudgetPerWorkerMigrated = true;
                return true;
            }

            int workers = RegistrationWorkerScale.Resolve(cfg.RegistrationWorkerCount);
            for (int i = 0; i < ThroughputFields.Length; i++)
            {
                Field field = ThroughputFields[i];
                field.Set(cfg, RegistrationWorkerScale.ToPerWorker(field.Get(cfg), workers));
            }

            cfg.RegistrationBudgetPerWorkerMigrated = true;

            api?.Logger?.Notification(
                "[ecosystemflora] Migrated registration throughput settings to per-worker values ({0} workers). Effective totals unchanged.",
                workers);

            return true;
        }

        static bool LooksAlreadyPerWorker(EcosystemConfig cfg)
        {
            for (int i = 0; i < ThroughputFields.Length; i++)
            {
                if (ThroughputFields[i].Get(cfg) > ThroughputFields[i].PerWorkerDefault)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
