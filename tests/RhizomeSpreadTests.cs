using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class RhizomeSpreadTests
    {
        [Theory]
        [InlineData(1, 0, true)]
        [InlineData(-1, 0, true)]
        [InlineData(0, 1, true)]
        [InlineData(0, -1, true)]
        [InlineData(1, 1, false)]
        [InlineData(2, 0, false)]
        [InlineData(0, 0, false)]
        public void IsOrthogonalStep_MatchesManhattanDistanceOne(int dx, int dz, bool expected)
        {
            Assert.Equal(expected, RhizomeSpread.IsOrthogonalStep(dx, dz));
        }

        [Fact]
        public void ApplyTo_ReedHabitat_SetsRhizomeModeAndRadius()
        {
            EcosystemConfig.Loaded = new EcosystemConfig { UseRhizomeSpreadForReeds = true };

            var req = new PlantRequirements
            {
                Habitat = EcologyHabitat.ReedNearWater,
                Species = "coopersreed",
            };

            RhizomeSpread.ApplyTo(req);

            Assert.True(req.UsesRhizomeSpread);
            Assert.Equal(1, req.SpreadRadius);
        }

        [Fact]
        public void ApplyTo_ConfigOff_LeavesIndependentMode()
        {
            EcosystemConfig.Loaded = new EcosystemConfig { UseRhizomeSpreadForReeds = false };

            var req = new PlantRequirements
            {
                Habitat = EcologyHabitat.ReedNearWater,
                Species = "coopersreed",
            };

            RhizomeSpread.ApplyTo(req);

            Assert.False(req.UsesRhizomeSpread);
            Assert.Equal(0, req.SpreadRadius);
        }

        [Fact]
        public void WildAquaticEcology_ReedSpreadRates_AreBelowTwo()
        {
            Assert.True(WildAquaticEcology.TryGet("coopersreed", out WildAquaticEcology.Profile reed));
            Assert.True(WildAquaticEcology.TryGet("tule", out WildAquaticEcology.Profile tule));
            Assert.True(WildAquaticEcology.TryGet("papyrus", out WildAquaticEcology.Profile papyrus));

            Assert.Equal(1.0f, reed.SpreadRate);
            Assert.Equal(0.85f, tule.SpreadRate);
            Assert.Equal(0.75f, papyrus.SpreadRate);
        }
    }
}
