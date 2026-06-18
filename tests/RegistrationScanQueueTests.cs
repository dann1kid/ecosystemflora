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
            Assert.Equal(16, cfg.PlayerRegistrationPriorityRadiusBlocks);
            Assert.Equal(48, cfg.MaxPriorityChunkScansPerTick);
            Assert.Equal(8192, cfg.MaxPriorityRegistrationsPerTick);
        }

        [Fact]
        public void RegistrationThroughput_DefaultsRaised()
        {
            var cfg = new EcosystemConfig();
            Assert.Equal(16, cfg.MaxChunkColumnsScannedPerTick);
            Assert.Equal(2048, cfg.MaxRegistrationsPerTick);
            Assert.Equal(80, cfg.BurstRegistrationBudgetMs);
            Assert.Equal(512, cfg.MaxRegistryAppliesPerTick);
            Assert.Equal(2048, cfg.MaxPriorityRegistryAppliesPerTick);
            Assert.True(cfg.EnableBackgroundRegistrationScan);
            Assert.Equal(8192, cfg.MaxRegistrationSnapshotCellsPerTick);
        }
    }
}
