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
        public void FreshEnqueue_DedupesUntilMarkComplete_ThenAllowsAgain()
        {
            var queue = new RegistrationScanQueue();
            var job = new PendingChunkScan(new Vintagestory.API.MathTools.Vec2i(2, 3));

            queue.Enqueue(job, highPriority: true);
            queue.Enqueue(job, highPriority: true);
            Assert.Equal(1, queue.Count);

            Assert.True(queue.TryDequeue(out _, preferPriority: true));
            Assert.Equal(0, queue.Count);

            // Without MarkComplete, fresh re-enqueue stays blocked (in-flight semantics).
            queue.Enqueue(job, highPriority: true);
            Assert.Equal(0, queue.Count);

            queue.MarkComplete(job.ChunkCoord);
            queue.Enqueue(job, highPriority: true);
            Assert.Equal(1, queue.Count);
        }

        [Fact]
        public void MaxFloraRescanColumns_DefaultsToLightCyclicLoad()
        {
            Assert.Equal(7, new EcosystemConfig().MaxFloraRescanColumnsPerTick);
        }

        [Fact]
        public void ShouldVicinityRescanEnqueue_SkipsFinishedChunks()
        {
            Assert.False(EcosystemSystem.ShouldVicinityRescanEnqueue(scanFinished: true));
            Assert.True(EcosystemSystem.ShouldVicinityRescanEnqueue(scanFinished: false));
        }

        [Fact]
        public void RegistrationThroughput_PerWorkerDefaults()
        {
            var cfg = new EcosystemConfig();
            Assert.Equal(14, cfg.MaxChunkColumnsScannedPerTick);
            Assert.Equal(54, cfg.MaxRegistrationsPerTick);
            Assert.Equal(8, cfg.BurstRegistrationBudgetMs);
            Assert.Equal(8, cfg.PriorityRegistrationBudgetMs);
            Assert.Equal(85, cfg.MaxRegistryAppliesPerTick);
            Assert.Equal(85, cfg.MaxPriorityRegistryAppliesPerTick);
            Assert.True(cfg.EnableBackgroundRegistrationScan);
            Assert.Equal(340, cfg.MaxRegistrationSnapshotCellsPerTick);
            Assert.Equal(0, cfg.RegistrationWorkerCount);
            Assert.Equal(2300, cfg.ChunkScanTickIntervalMs);
            Assert.Equal(9, cfg.RegistrationBudgetMs);
            Assert.Equal(2, cfg.FoliageChunkSyncBudgetMs);
            Assert.Equal(5, cfg.TickBudgetMs);
            Assert.Equal(4, cfg.SpreadBudgetMs);
            Assert.Equal(14, cfg.MaxReproduceAttemptsPerTick);
        }

        [Fact]
        public void RegistrationThroughput_EffectiveScalesWithWorkers()
        {
            var cfg = new EcosystemConfig { RegistrationWorkerCount = 4 };
            Assert.Equal(340, cfg.EffectiveMaxRegistryAppliesPerTick());
            Assert.Equal(340, cfg.EffectiveMaxRegistrationSnapshotCellsPerTick());
        }
    }
}
