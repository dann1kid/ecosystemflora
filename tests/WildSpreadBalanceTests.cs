using Vintagestory.API.Common;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class WildSpreadBalanceTests
    {
        [Fact]
        public void ScaleSpeciesSpreadRate_DividesByThree()
        {
            Assert.Equal(2.8f / 3f, WildSpreadBalance.ScaleSpeciesSpreadRate("horsetail", 2.8f), 4);
        }

        [Fact]
        public void ScaleSpeciesSpreadRate_BrownsedgeExempt()
        {
            Assert.Equal(0.35f, WildSpreadBalance.ScaleSpeciesSpreadRate(EcologyShoreSedgeSpecies.Brownsedge, 0.35f));
        }

        [Fact]
        public void SpeciesSpread_IntervalScalesGlobalBalance()
        {
            var cfg = new EcosystemConfig
            {
                UseSpeciesSpreadRates = true,
                UseCalendarScaledSpread = false,
                ReproduceIntervalHours = 24,
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
            };
            var req = new PlantRequirements { Species = "catmint", SpreadRate = 1.65f };

            float chance = SpeciesSpread.EffectiveChance(null, null, cfg, req);

            Assert.Equal(0.5f * (1.65f / 3f), chance, 3);
        }
    }
}
