using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class SpreadChunkSchedulerTests
    {
        static ReproducerEntry MakeEntry(int x, int z, string species, double nextAttemptHours)
        {
            return new ReproducerEntry(
                new BlockPos(x, 64, z),
                new AssetLocation("game", "flower-" + species + "-free"),
                new AssetLocation("game", "flower-" + species + "-free"),
                new PlantRequirements { Species = species, Habitat = EcologyHabitat.Terrestrial },
                nextAttemptHours);
        }

        [Fact]
        public void Process_RotatesAmongAllDueEntries_NotListHead()
        {
            var registry = new ReproducerRegistry();
            var cfg = new EcosystemConfig
            {
                EnableEventDrivenSpread = false,
                MaxSpreadAttemptsPerChunkPerTick = 2,
                MaxSpreadChunksVisitedPerTick = 4,
            };

            registry.Add(MakeEntry(0, 0, "grassA", nextAttemptHours: 0));
            registry.Add(MakeEntry(1, 0, "grassB", nextAttemptHours: 0));
            registry.Add(MakeEntry(2, 0, "grassC", nextAttemptHours: 1000));
            registry.Add(MakeEntry(3, 0, "newflower", nextAttemptHours: 0));

            var scheduler = new SpreadChunkScheduler();
            var attempted = new List<string>();

            for (int pass = 0; pass < 2; pass++)
            {
                scheduler.Process(
                    registry,
                    cfg,
                    now: 10,
                    maxTotalAttempts: 2,
                    scopeChunks: null,
                    _ => 24,
                    entry =>
                    {
                        attempted.Add(entry.Requirements.Species);
                        entry.NextAttemptHours = 1000;
                        return true;
                    },
                    budgetTicks: 0,
                    budgetWatch: null,
                    out _,
                    out _,
                    out _);
            }

            Assert.Contains("newflower", attempted);
            Assert.Contains("grassA", attempted);
            Assert.Contains("grassB", attempted);
        }

        [Fact]
        public void WakeAround_DoesNotPullCalendarWhenRetryHoursZero()
        {
            var registry = new ReproducerRegistry();
            var entry = MakeEntry(1, 1, "cowparsley", nextAttemptHours: 100);
            registry.Add(entry);

            registry.WakeAround(new BlockPos(1, 64, 1), radiusBlocks: 4, now: 10, new EcosystemConfig
            {
                EnableEventDrivenSpread = true,
                EventWakeRetryHours = 0,
            });

            Assert.Equal(100, entry.NextAttemptHours);
        }
    }
}
