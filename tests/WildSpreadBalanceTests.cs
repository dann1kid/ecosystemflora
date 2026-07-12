using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class WildSpreadBalanceTests
    {
        static EcosystemConfig ConfigWithScale(float scale) =>
            new EcosystemConfig { SpeciesSpreadRateScale = scale };

        [Fact]
        public void ScaleSpeciesSpreadRate_UsesConfigScale()
        {
            var cfg = ConfigWithScale(1f / 3f);
            Assert.Equal(2.8f / 3f, WildSpreadBalance.ScaleSpeciesSpreadRate("horsetail", 2.8f, cfg), 4);
        }

        [Fact]
        public void ScaleSpeciesSpreadRate_UnityScaleMatchesEcologyTable()
        {
            var cfg = ConfigWithScale(1f);
            Assert.Equal(2.8f, WildSpreadBalance.ScaleSpeciesSpreadRate("horsetail", 2.8f, cfg), 4);
        }

        [Fact]
        public void ScaleSpeciesSpreadRate_BrownsedgeUsesGlobalScale()
        {
            var cfg = ConfigWithScale(1f / 3f);
            Assert.Equal(0.35f / 3f, WildSpreadBalance.ScaleSpeciesSpreadRate(EcologyShoreSedgeSpecies.Brownsedge, 0.35f, cfg), 4);
        }

        [Fact]
        public void SpeciesSpread_IntervalScalesGlobalBalance()
        {
            var cfg = new EcosystemConfig
            {
                UseSpeciesSpreadRates = true,
                UseCalendarScaledSpread = false,
                ReproduceIntervalHours = 24,
                SpeciesSpreadRateScale = 1f / 3f,
            };
            var req = new PlantRequirements { Species = "horsetail", SpreadRate = 2.8f };

            double hours = SpeciesSpread.EffectiveIntervalHours(null, null, cfg, req);

            Assert.Equal(24 / (2.8f / 3f), hours, 2);
        }

        [Fact]
        public void SpeciesSpread_ChanceScalesGlobalBalance()
        {
            var cfg = new EcosystemConfig
            {
                UseSpeciesSpreadRates = true,
                ReproduceChance = 0.5f,
                SpeciesSpreadRateScale = 1f / 3f,
            };
            var req = new PlantRequirements { Species = "catmint", SpreadRate = 1.65f };

            float chance = SpeciesSpread.EffectiveChance(null, null, cfg, req);

            Assert.Equal(0.5f * (1.65f / 3f), chance, 3);
        }

        [Fact]
        public void TimelapsePreset_BoostsTallgrassSpreadRate()
        {
            var cfg = new EcosystemConfig
            {
                BalancePreset = EcosystemBalancePresets.Timelapse,
                SpeciesSpreadRateScale = 10f,
            };

            float flower = WildSpreadBalance.ScaleSpeciesSpreadRate("cornflower", 2f, cfg);
            float grass = WildSpreadBalance.ScaleSpeciesSpreadRate("tallgrass", 1.35f, cfg);

            Assert.Equal(20f, flower, 3);
            Assert.Equal(1.35f * 10f * EcosystemBalancePresets.TimelapseTallgrassSpreadMultiplier, grass, 3);
            Assert.True(grass > flower);
        }
    }
}
