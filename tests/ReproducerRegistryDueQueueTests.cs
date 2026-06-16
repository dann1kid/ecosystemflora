using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class ReproducerRegistryDueQueueTests
    {
        static ReproducerEntry MakeEntry(int x, double nextAttemptHours)
        {
            return new ReproducerEntry(
                new BlockPos(x, 64, 10),
                new AssetLocation("game:flower-catmint-free"),
                new AssetLocation("game:flower-catmint-free"),
                new PlantRequirements { Species = "catmint" },
                nextAttemptHours);
        }

        [Fact]
        public void CollectDueEntries_IncludesAllDueInScope()
        {
            var registry = new ReproducerRegistry();
            registry.Add(MakeEntry(1, 0));
            registry.Add(MakeEntry(2, 100));
            registry.Add(MakeEntry(3, 0));

            var due = new List<ReproducerEntry>();
            registry.CollectDueEntries(now: 10, activeChunks: null, due);

            Assert.Equal(2, due.Count);
        }

        [Fact]
        public void ProcessDue_RotatesFairCursorSoLaterEntriesGetAttempts()
        {
            var registry = new ReproducerRegistry();
            for (int i = 0; i < 8; i++)
            {
                registry.Add(MakeEntry(i, 0));
            }

            var seen = new HashSet<int>();
            for (int tick = 0; tick < 8; tick++)
            {
                int processed = registry.ProcessDue(
                    10,
                    maxAttempts: 1,
                    _ => 24,
                    entry =>
                    {
                        seen.Add(entry.Origin.X);
                        return true;
                    });

                Assert.Equal(1, processed);
            }

            Assert.Equal(8, seen.Count);
        }

        [Fact]
        public void ProcessDue_ReportsDueQueueSizeBeforeBudgetCap()
        {
            var registry = new ReproducerRegistry();
            registry.Add(MakeEntry(1, 0));
            registry.Add(MakeEntry(2, 0));
            registry.Add(MakeEntry(3, 0));

            int processed = registry.ProcessDue(
                10,
                maxAttempts: 1,
                _ => 24,
                _ => true);

            Assert.Equal(1, processed);
            Assert.Equal(3, registry.LastDueQueueSize);
        }

        [Fact]
        public void CollectDueEntries_HeapSkipsNotYetDue()
        {
            var registry = new ReproducerRegistry();
            registry.Add(MakeEntry(1, 0));
            for (int i = 2; i < 2000; i++)
            {
                registry.Add(MakeEntry(i, 1000));
            }

            var due = new List<ReproducerEntry>();
            registry.CollectDueEntries(now: 10, activeChunks: null, due);

            Assert.Single(due);
            Assert.Equal(1, due[0].Origin.X);
        }

        [Fact]
        public void ProcessDue_RequeuesEntryAfterAttempt()
        {
            var registry = new ReproducerRegistry();
            registry.Add(MakeEntry(1, 0));

            int processed = registry.ProcessDue(
                10,
                maxAttempts: 1,
                _ => 48,
                _ => true);

            Assert.Equal(1, processed);

            var due = new List<ReproducerEntry>();
            registry.CollectDueEntries(now: 11, activeChunks: null, due);
            Assert.Empty(due);

            registry.CollectDueEntries(now: 58, activeChunks: null, due);
            Assert.Single(due);
        }
    }
}
