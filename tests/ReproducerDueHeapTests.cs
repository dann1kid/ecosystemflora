using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class ReproducerDueHeapTests
    {
        static ReproducerEntry Entry(int index, double nextHours)
        {
            var entry = new ReproducerEntry(
                new Vintagestory.API.MathTools.BlockPos(index, 64, 0),
                new Vintagestory.API.Common.AssetLocation("game:flower-catmint-free"),
                new Vintagestory.API.Common.AssetLocation("game:flower-catmint-free"),
                new PlantRequirements { Species = "catmint" },
                nextHours);
            entry.EntriesIndex = index;
            return entry;
        }

        [Fact]
        public void Pop_ReturnsEarliestNextAttemptFirst()
        {
            var heap = new ReproducerDueHeap();
            heap.Push(Entry(3, 30));
            heap.Push(Entry(1, 5));
            heap.Push(Entry(2, 10));

            Assert.Equal(5, heap.Pop().NextAttemptHours);
            Assert.Equal(10, heap.Pop().NextAttemptHours);
            Assert.Equal(30, heap.Pop().NextAttemptHours);
        }

        [Fact]
        public void TryPeek_RespectsNowThreshold()
        {
            var heap = new ReproducerDueHeap();
            heap.Push(Entry(1, 20));

            Assert.False(heap.TryPeek(now: 10, out _));
            Assert.True(heap.TryPeek(now: 25, out ReproducerEntry due));
            Assert.Equal(20, due.NextAttemptHours);
        }
    }
}
