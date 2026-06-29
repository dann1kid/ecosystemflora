using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class CellCompetitionSpreadTests
    {
        [Fact]
        public void Birch_OpenField_ReproduceFitness_PassesDefaultMinFitness()
        {
            EcosystemConfig.Loaded = new EcosystemConfig
            {
                EnableTreeSeralSuccession = true,
                ApplyWorldgenRainForest = false,
            };

            Assert.True(WildTreeEcology.TryGet("birch", out WildTreeEcology.Profile profile));

            var req = new PlantRequirements
            {
                Species = "birch",
                Habitat = EcologyHabitat.TerrestrialTree,
                SpreadRate = profile.SpreadRate,
                MinForest = profile.MinForest,
                MaxForest = profile.MaxForest,
                MinRain = 0f,
                MaxRain = 1f,
            };

            var ctx = new StubContext { LocalForestCover = 0f };

            float climate = SuitabilityEvaluator.ReproduceFitness(req, ctx);

            Assert.True(climate >= EcosystemConfig.Loaded.MinFitness, $"climate={climate}");
            Assert.True(climate * profile.SpreadRate < EcosystemConfig.Loaded.MinFitness);
        }
    }
}
