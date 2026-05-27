using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class SpeciesSpreadTests
    {
        [Fact]
        public void LegacyInterval_ZeroHours_UsesDayDefaultAndSpeciesRate()
        {
            var cfg = new EcosystemConfig
            {
                UseCalendarScaledSpread = false,
                ReproduceIntervalHours = 0,
                UseSpeciesSpreadRates = true,
            };
            var req = new PlantRequirements { SpreadRate = 2f };

            double hours = SpeciesSpread.EffectiveIntervalHours(null, null, cfg, req);

            Assert.Equal(12, hours, 3);
        }
    }
}
