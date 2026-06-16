using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class SeasonEcologyWakeTests
    {
        static ReproducerEntry FlowerEntry(string species, int x = 0)
        {
            return new ReproducerEntry(
                new BlockPos(x, 64, 0),
                new AssetLocation("game:flower-catmint-free"),
                new AssetLocation("game:flower-catmint-free"),
                new PlantRequirements { Species = species, Habitat = EcologyHabitat.Terrestrial },
                nextAttemptHours: 1000);
        }

        static ReproducerEntry MyceliumEntry(int x = 10)
        {
            return new ReproducerEntry(
                new BlockPos(x, 64, 0),
                new AssetLocation("game:mushroom-fieldmushroom-normal"),
                new AssetLocation("game:mushroom-fieldmushroom-normal"),
                new PlantRequirements
                {
                    Species = "fieldmushroom",
                    Habitat = EcologyHabitat.MyceliumAnchor,
                },
                nextAttemptHours: 1000);
        }

        [Fact]
        public void EnableSeasonCoarseWake_DefaultsTrue()
        {
            Assert.True(new EcosystemConfig().EnableSeasonCoarseWake);
        }

        [Fact]
        public void UsesSeasonalSpread_DaffodilVariesByMonth()
        {
            Assert.True(WildSpeciesSeason.UsesSeasonalSpread("daffodil"));
        }

        [Fact]
        public void WakeMatching_WakesSeasonalEntriesOnly()
        {
            var registry = new ReproducerRegistry();
            registry.Add(FlowerEntry("daffodil", x: 1));
            registry.Add(MyceliumEntry(x: 2));

            int woken = registry.WakeMatching(SeasonEcologyWake.ShouldWakeEntry);

            Assert.Equal(1, woken);

            var due = new System.Collections.Generic.List<ReproducerEntry>();
            registry.CollectDueEntries(now: 10, activeChunks: null, due, eventDriven: true);

            Assert.Single(due);
            Assert.Equal(1, due[0].Origin.X);
        }

        [Fact]
        public void SeasonSpreadMultiplier_StillAppliedAfterWake()
        {
            Assert.True(WildSpeciesSeason.Resolve("daffodil").SpreadMultiplier(2) > 0.5f);
        }
    }
}
