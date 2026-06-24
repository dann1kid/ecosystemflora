using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class FlowerPhenologyTests
    {
        static EcosystemConfig EnabledCfg => new EcosystemConfig { EnableFlowerPhenology = true };

        [Theory]
        [InlineData("catmint", true)]
        [InlineData("cornflower", true)]
        [InlineData("coopersreed", false)]
        [InlineData("oak", false)]
        public void UsesPhenology_OnlyMeadowFlowers(string species, bool expected)
        {
            var req = new PlantRequirements { Species = species, Habitat = EcologyHabitat.Terrestrial };
            Assert.Equal(expected, FlowerPhenology.UsesPhenology(EnabledCfg, req));
        }

        [Fact]
        public void UsesPhenology_OffWhenConfigDisabled()
        {
            var cfg = new EcosystemConfig { EnableFlowerPhenology = false };
            var req = new PlantRequirements { Species = "catmint", Habitat = EcologyHabitat.Terrestrial };
            Assert.False(FlowerPhenology.UsesPhenology(cfg, req));
        }

        [Theory]
        [InlineData(0.05f, 12f, FlowerPhenologyPhase.Dormant)]
        [InlineData(0.6f, 18f, FlowerPhenologyPhase.Bloom)]
        [InlineData(0.25f, 18f, FlowerPhenologyPhase.Vegetative)]
        [InlineData(0.5f, 40f, FlowerPhenologyPhase.Dieback)]
        public void InferInitialPhase_RespectsSeasonAndTemperature(
            float season,
            float temp,
            FlowerPhenologyPhase expected)
        {
            var cfg = EnabledCfg;
            Assert.Equal(expected, FlowerPhenology.InferInitialPhase(season, temp, cfg));
        }

        [Fact]
        public void CanSpread_OnlyInBloom()
        {
            var entry = new ReproducerEntry(
                null,
                null,
                null,
                new PlantRequirements { Species = "catmint", Habitat = EcologyHabitat.Terrestrial },
                0);

            entry.PhenologyPhase = FlowerPhenologyPhase.Vegetative;
            Assert.False(FlowerPhenology.CanSpread(entry));

            entry.PhenologyPhase = FlowerPhenologyPhase.Bloom;
            Assert.True(FlowerPhenology.CanSpread(entry));
        }

        [Fact]
        public void Advance_VegetativeAccumulatesEnergyTowardBloom()
        {
            var cfg = EnabledCfg;
            var entry = new ReproducerEntry(
                null,
                null,
                null,
                new PlantRequirements { Species = "wilddaisy", Habitat = EcologyHabitat.Terrestrial },
                0)
            {
                PhenologyPhase = FlowerPhenologyPhase.Vegetative,
                PhenologyEnergy = 0f,
                LastPhenologyUpdateHours = 100,
            };

            FlowerPhenology.AdvanceVegetativeForTests(entry, season: 2f, temp: 18f, cfg, deltaDays: 10);
            Assert.True(entry.PhenologyEnergy > 0.5f);
        }

        [Fact]
        public void Advance_BloomDepletesAndExitsToDieback()
        {
            var cfg = EnabledCfg;
            var entry = new ReproducerEntry(
                null,
                null,
                null,
                new PlantRequirements { Species = "wilddaisy", Habitat = EcologyHabitat.Terrestrial },
                0)
            {
                PhenologyPhase = FlowerPhenologyPhase.Bloom,
                PhenologyEnergy = cfg.FlowerBloomEnergyThreshold,
                LastPhenologyUpdateHours = 100,
            };

            FlowerPhenology.AdvanceBloomForTests(entry, season: 0.1f, temp: 18f, cfg, deltaDays: 12);
            Assert.Equal(FlowerPhenologyPhase.Dieback, entry.PhenologyPhase);
        }
    }
}
