using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class TallgrassPhenologyTests
    {
        static EcosystemConfig EnabledCfg => new EcosystemConfig { EnableTallgrassPhenology = true };

        [Fact]
        public void UsesPhenology_OnlyTallgrass()
        {
            var req = new PlantRequirements { Species = "tallgrass", Habitat = EcologyHabitat.Terrestrial };
            Assert.True(TallgrassPhenology.UsesPhenology(EnabledCfg, req));

            var flower = new PlantRequirements { Species = "catmint", Habitat = EcologyHabitat.Terrestrial };
            Assert.False(TallgrassPhenology.UsesPhenology(EnabledCfg, flower));
        }

        [Theory]
        [InlineData(0.05f, TallgrassPhenologyPhase.Dormant)]
        [InlineData(0.5f, TallgrassPhenologyPhase.Active)]
        public void InferPhaseForTests_RespectsSeason(float season, TallgrassPhenologyPhase expected)
        {
            Assert.Equal(expected, TallgrassPhenology.InferPhaseForTests(season));
        }

        [Theory]
        [InlineData(TallgrassPhenologyPhase.Dormant, false)]
        [InlineData(TallgrassPhenologyPhase.Active, true)]
        [InlineData(TallgrassPhenologyPhase.Dieback, false)]
        public void AllowsSpread_OnlyActive(TallgrassPhenologyPhase phase, bool expected)
        {
            Assert.Equal(expected, TallgrassPhenology.AllowsSpread(phase));
        }
    }
}
