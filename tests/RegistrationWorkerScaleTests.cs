using System;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class RegistrationWorkerScaleTests
    {
        [Theory]
        [InlineData(1, 1)]
        [InlineData(4, 4)]
        [InlineData(16, 8)]
        public void Resolve_ExplicitCount(int configured, int expected)
        {
            Assert.Equal(expected, RegistrationWorkerScale.Resolve(configured));
        }

        [Fact]
        public void Resolve_AutoUsesHalfLogicalProcessors()
        {
            int cores = Math.Max(1, Environment.ProcessorCount / 2);
            if (cores > RegistrationWorkerScale.MaxWorkers) cores = RegistrationWorkerScale.MaxWorkers;
            Assert.Equal(cores, RegistrationWorkerScale.Resolve(0));
        }

        [Fact]
        public void EffectiveBudgets_ApplyScale_ButSnapshotDoesNot()
        {
            var cfg = new EcosystemConfig
            {
                RegistrationWorkerCount = 4,
                MaxRegistryAppliesPerTick = 128,
                MaxRegistryAppliesPerChunkPerTick = 64,
                MaxRegistrationSnapshotCellsPerTick = 2048,
            };

            Assert.Equal(4, cfg.EffectiveRegistrationWorkerCount());
            Assert.Equal(512, cfg.EffectiveMaxRegistryAppliesPerTick());
            Assert.Equal(256, cfg.EffectiveMaxRegistryAppliesPerChunkPerTick());
            // Snapshot capture is main-thread GetBlock — must not multiply by worker count.
            Assert.Equal(2048, cfg.EffectiveMaxRegistrationSnapshotCellsPerTick());
        }
    }
}
