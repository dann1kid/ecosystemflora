using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class FernPhenologyTests
    {
        static EcosystemConfig EnabledCfg => new EcosystemConfig { EnableFernPhenology = true };

        [Theory]
        [InlineData("eaglefern", true)]
        [InlineData("catmint", false)]
        public void UsesPhenology_OnlyFerns(string species, bool expected)
        {
            var req = new PlantRequirements { Species = species, Habitat = EcologyHabitat.Terrestrial };
            Assert.Equal(expected, FernPhenology.UsesPhenology(EnabledCfg, req));
        }

        [Theory]
        [InlineData(0.05f, FernPhenologyPhase.Dormant)]
        [InlineData(0.6f, FernPhenologyPhase.Sporulating)]
        public void InferPhaseForTests_RespectsSeason(float season, FernPhenologyPhase expected)
        {
            Assert.Equal(expected, FernPhenology.InferPhaseForTests(season));
        }

        [Theory]
        [InlineData(FernPhenologyPhase.Dormant, false)]
        [InlineData(FernPhenologyPhase.Sporulating, true)]
        [InlineData(FernPhenologyPhase.Dieback, false)]
        public void AllowsSpread_OnlySporulating(FernPhenologyPhase phase, bool expected)
        {
            Assert.Equal(expected, FernPhenology.AllowsSpread(phase));
        }
    }
}
