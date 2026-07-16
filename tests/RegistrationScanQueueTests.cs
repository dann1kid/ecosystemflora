using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class RegistrationScanQueueTests
    {
        [Fact]
        public void Priority_DequeuesBeforeBackground()
        {
            var queue = new RegistrationScanQueue();
            var background = new PendingChunkScan(new Vintagestory.API.MathTools.Vec2i(0, 0));
            var priority = new PendingChunkScan(new Vintagestory.API.MathTools.Vec2i(1, 0));

            queue.Enqueue(background, highPriority: false);
            queue.Enqueue(priority, highPriority: true);

            Assert.True(queue.TryDequeue(out PendingChunkScan job, preferPriority: true));
            Assert.Equal(1, job.ChunkCoord.X);
        }

        [Fact]
        public void PlayerPriorityRegistration_DefaultsEnabled()
        {
            var cfg = new EcosystemConfig();
            Assert.True(cfg.EnablePlayerPriorityRegistration);
            Assert.True(cfg.EnableBurstRegistrationNearPlayers);
            Assert.Equal(64, cfg.PlayerRegistrationPriorityRadiusBlocks);
            Assert.Equal(24, cfg.MaxPriorityChunkScansPerTick);
            Assert.Equal(2048, cfg.MaxPriorityRegistrationsPerTick);
        }

        [Fact]
        public void RegistrationThroughput_PerWorkerDefaults()
        {
            var cfg = new EcosystemConfig();
            Assert.Equal(64, cfg.MaxChunkColumnsScannedPerTick);
            Assert.Equal(256, cfg.MaxRegistrationsPerTick);
            Assert.Equal(120, cfg.BurstRegistrationBudgetMs);
            Assert.Equal(512, cfg.MaxRegistryAppliesPerTick);
            Assert.Equal(512, cfg.MaxPriorityRegistryAppliesPerTick);
            Assert.True(cfg.EnableBackgroundRegistrationScan);
            Assert.Equal(4096, cfg.MaxRegistrationSnapshotCellsPerTick);
            Assert.Equal(0, cfg.RegistrationWorkerCount);
            Assert.Equal(1000, cfg.ChunkScanTickIntervalMs);
        }

        [Fact]
        public void RegistrationThroughput_EffectiveScalesWithWorkers()
        {
            var cfg = new EcosystemConfig { RegistrationWorkerCount = 4 };
            Assert.Equal(2048, cfg.EffectiveMaxRegistryAppliesPerTick());
            Assert.Equal(16384, cfg.EffectiveMaxRegistrationSnapshotCellsPerTick());
        }
    }
}
