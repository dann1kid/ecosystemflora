using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class PlantGroundRulesTests
    {
        [Theory]
        [InlineData("lakeice", true)]
        [InlineData("glacierice", true)]
        [InlineData("snow", true)]
        [InlineData("snowblock-normal", true)]
        [InlineData("soil-medium-normal", false)]
        [InlineData("forestfloor", false)]
        public void IsUnplantableGroundPath_DetectsIceAndSnow(string path, bool expected)
        {
            Assert.Equal(expected, WildSoilGroundRules.IsUnplantableGroundPath(path));
        }

        [Fact]
        public void CanReproduce_Tree_BarrenGround_Fails()
        {
            var req = new PlantRequirements
            {
                Species = "pine",
                Habitat = EcologyHabitat.TerrestrialTree,
                MinRain = 0f,
                MaxRain = 1f,
                MinForest = 0f,
                MaxForest = 1f,
            };
            WildPlantSoil.ApplyTo(req);

            var ctx = new StubContext
            {
                GroundSoilKinds = SoilKind.Barren,
                GroundFertility = 2,
                SpaceReplaceable = 9999,
            };

            Assert.False(SuitabilityEvaluator.CanReproduce(req, ctx, harshClimate: false));
        }

        [Theory]
        [InlineData("birch")]
        [InlineData("oak")]
        [InlineData("maple")]
        public void CanReproduce_Tree_OpenFieldSoil_Passes(string species)
        {
            var req = new PlantRequirements
            {
                Species = species,
                Habitat = EcologyHabitat.TerrestrialTree,
                MinRain = 0f,
                MaxRain = 1f,
                MinForest = 0f,
                MaxForest = 1f,
            };
            WildPlantSoil.ApplyTo(req);

            var ctx = new StubContext
            {
                GroundSoilKinds = SoilKind.MediumFert,
                GroundFertility = 200,
                SpaceReplaceable = 9999,
                GroundSideSolid = true,
            };

            Assert.True(SuitabilityEvaluator.CanReproduce(req, ctx, harshClimate: false));
        }
    }
}
