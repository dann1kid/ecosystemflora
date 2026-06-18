using System.Collections.Generic;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class PendingRegistrationQueueTests
    {
        [Fact]
        public void IsReadyToMarkComplete_WhenScanDoneAndQueueEmpty()
        {
            var queue = new PendingRegistrationQueue();
            var chunk = new Vec2i(3, 4);

            Assert.True(queue.IsReadyToMarkComplete(chunk));

            queue.SetScanCompleted(chunk, true);
            Assert.True(queue.IsReadyToMarkComplete(chunk));
        }

        [Fact]
        public void IsReadyToMarkComplete_FalseWhilePendingRemain()
        {
            var queue = new PendingRegistrationQueue();
            var chunk = new Vec2i(1, 2);
            var hits = new List<ChunkFlowerHit>
            {
                new ChunkFlowerHit(new BlockPos(8, 64, 8), new Vintagestory.API.Common.AssetLocation("game", "flower-lupine-blue")),
            };

            queue.EnqueueHits(chunk, hits, PendingRegistrationKind.Flower);
            queue.SetScanCompleted(chunk, true);

            Assert.False(queue.IsReadyToMarkComplete(chunk));
            Assert.Equal(1, queue.TotalPending);
        }

        [Fact]
        public void TryEnqueueTree_SkipsDuplicateBase()
        {
            var queue = new PendingRegistrationQueue();
            var registry = new ReproducerRegistry();
            var chunk = new Vec2i(0, 0);
            var basePos = new BlockPos(4, 64, 4);
            var code = new Vintagestory.API.Common.AssetLocation("game", "log-grown-oak-ud");

            Assert.True(queue.TryEnqueueTree(chunk, basePos, code, registry));
            Assert.False(queue.TryEnqueueTree(chunk, basePos, code, registry));
            Assert.Equal(1, queue.TotalPending);
        }
    }
}
