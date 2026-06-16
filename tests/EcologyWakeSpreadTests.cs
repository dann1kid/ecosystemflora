using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class EcologyWakeSpreadTests
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

        static EcosystemConfig ChunkFairConfig()
        {
            return new EcosystemConfig
            {
                EnableChunkFairSpread = true,
                EnableEventDrivenSpread = true,
                MaxSpreadAttemptsPerChunkPerTick = 1,
                MaxSpreadChunksVisitedPerTick = 8,
                MaxReproduceAttemptsPerTick = 8,
            };
        }

        [Fact]
        public void WakeAround_MakesNeighborDueBeforeCalendar()
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

        [Fact]
        public void ProcessDueChunkFair_VisitsMultipleChunksPerTick()
        {
            var registry = new ReproducerRegistry();
            registry.Add(MakeEntry(1, 0, 0));
            registry.Add(MakeEntry(40, 0, 0));

            var cfg = ChunkFairConfig();
            var seenChunks = new HashSet<long>();

            int processed = registry.ProcessDueChunkFair(
                cfg,
                now: 10,
                maxAttempts: 2,
                activeChunks: null,
                _ => 48,
                entry =>
                {
                    seenChunks.Add(ChunkKey(entry.Origin.X, entry.Origin.Z));
                    return true;
                });

            Assert.Equal(2, processed);
            Assert.Equal(2, seenChunks.Count);
        }

        [Fact]
        public void ProcessDueChunkFair_WakeDrivenSpreadWithoutCalendarDue()
        {
            var registry = new ReproducerRegistry();
            registry.Add(MakeEntry(1, 0, 1000));
            registry.WakeAround(new BlockPos(1, 64, 0), radiusBlocks: 6);

            int processed = registry.ProcessDueChunkFair(
                ChunkFairConfig(),
                now: 10,
                maxAttempts: 1,
                activeChunks: null,
                _ => 48,
                _ => true);

            Assert.Equal(1, processed);
        }

        static long ChunkKey(int x, int z)
        {
            int cs = 32;
            int cx = x / cs;
            int cz = z / cs;
            return ((long)cx << 32) | (uint)cz;
        }
    }
}
