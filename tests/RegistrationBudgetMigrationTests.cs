using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class RegistrationBudgetMigrationTests
    {
        [Fact]
        public void ApplyIfNeeded_DividesLegacyAbsoluteValues()
        {
            var cfg = new EcosystemConfig
            {
                RegistrationBudgetPerWorkerMigrated = false,
                RegistrationWorkerCount = 4,
                MaxRegistryAppliesPerTick = 512,
                MaxRegistryAppliesPerChunkPerTick = 256,
                MaxRegistrationSnapshotCellsPerTick = 8192,
                MaxRegistrationsPerTick = 2048,
                MaxPriorityChunkScansPerTick = 48,
                MaxPriorityRegistrationsPerTick = 8192,
                MaxBurstRegistrationsPerChunk = 4096,
                MaxPriorityRegistryAppliesPerTick = 2048,
                MaxChunkColumnsScannedPerTick = 16,
            };

            Assert.True(RegistrationBudgetMigration.ApplyIfNeeded(cfg, api: null, configFileExisted: true));

            Assert.True(cfg.RegistrationBudgetPerWorkerMigrated);
            Assert.Equal(128, cfg.MaxRegistryAppliesPerTick);
            Assert.Equal(64, cfg.MaxRegistryAppliesPerChunkPerTick);
            Assert.Equal(2048, cfg.MaxRegistrationSnapshotCellsPerTick);
            Assert.Equal(512, cfg.MaxRegistrationsPerTick);
            Assert.Equal(12, cfg.MaxPriorityChunkScansPerTick);
            Assert.Equal(2048, cfg.MaxPriorityRegistrationsPerTick);
            Assert.Equal(1024, cfg.MaxBurstRegistrationsPerChunk);
            Assert.Equal(512, cfg.MaxPriorityRegistryAppliesPerTick);
            Assert.Equal(4, cfg.MaxChunkColumnsScannedPerTick);
            Assert.Equal(512, cfg.EffectiveMaxRegistryAppliesPerTick());
        }

        [Fact]
        public void ApplyIfNeeded_SkipsWhenAlreadyPerWorker()
        {
            var cfg = new EcosystemConfig
            {
                RegistrationBudgetPerWorkerMigrated = false,
                RegistrationWorkerCount = 4,
            };

            Assert.True(RegistrationBudgetMigration.ApplyIfNeeded(cfg, api: null, configFileExisted: true));
            Assert.True(cfg.RegistrationBudgetPerWorkerMigrated);
            Assert.Equal(512, cfg.MaxRegistryAppliesPerTick);
            Assert.Equal(2048, cfg.EffectiveMaxRegistryAppliesPerTick());
        }

        [Fact]
        public void ApplyIfNeeded_NoOpWhenAlreadyMigrated()
        {
            var cfg = new EcosystemConfig
            {
                RegistrationBudgetPerWorkerMigrated = true,
                MaxRegistryAppliesPerTick = 512,
            };

            Assert.False(RegistrationBudgetMigration.ApplyIfNeeded(cfg, api: null, configFileExisted: true));
            Assert.Equal(512, cfg.MaxRegistryAppliesPerTick);
        }

        [Fact]
        public void ToPerWorker_PreservesEffectiveTotal()
        {
            Assert.Equal(128, RegistrationWorkerScale.ToPerWorker(512, 4));
            Assert.Equal(512, RegistrationWorkerScale.Scale(128, 4));
        }
    }
}
