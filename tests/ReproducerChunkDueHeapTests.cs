using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class ReproducerChunkDueHeapTests
    {
        static ReproducerEntry MakeEntry(int x, int z, double nextAttemptHours)
        {
            return new ReproducerEntry(
                new BlockPos(x, 64, z),
                new AssetLocation("game:flower-catmint-free"),
                new AssetLocation("game:flower-catmint-free"),
                new PlantRequirements { Species = "catmint" },
                nextAttemptHours);
        }

        [Fact]
        public void CollectDueEntries_ScopedChunkHeap_SkipsFutureDueInOtherChunk()
        {
            var registry = new ReproducerRegistry();
            registry.Add(MakeEntry(0, 0, 0));
            registry.Add(MakeEntry(512, 0, 1000));

            var scope = new List<Vec2i> { ReproducerRegistry.ToChunkCoord(new BlockPos(0, 64, 0)) };
            var due = new List<ReproducerEntry>();
            registry.CollectDueEntries(now: 10, scope, due, eventDriven: false);

            Assert.Single(due);
            Assert.Equal(0, due[0].Origin.X);
        }

        [Fact]
        public void CollectDueEntries_EventDriven_UsesChunkScanNotGlobalHeap()
        {
            var registry = new ReproducerRegistry();
            registry.Add(MakeEntry(1, 10, 1000));
            registry.Add(MakeEntry(50, 10, 1000));

            registry.WakeAround(new BlockPos(1, 64, 10), radiusBlocks: 6);

            var due = new List<ReproducerEntry>();
            registry.CollectDueEntries(now: 10, activeChunks: null, due, eventDriven: true);

            Assert.Single(due);
            Assert.Equal(1, due[0].Origin.X);
        }
    }
}
