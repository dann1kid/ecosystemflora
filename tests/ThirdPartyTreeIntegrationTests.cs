using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class ThirdPartyTreeIntegrationTests
    {
        [Fact]
        public void GetTreeWood_GameLogGrown_UnknownWood_ReturnsWoodId()
        {
            var code = new AssetLocation("game", "log-grown-stinkwood-ud");
            Assert.Equal("stinkwood", PlantCodeHelper.GetTreeWood(code));

            var block = new Block { Code = code };
            Assert.True(PlantCodeHelper.IsTreeLogGrownBlock(block));
            Assert.Equal("stinkwood", PlantCodeHelper.ResolveEcologySpecies(block));
        }

        [Fact]
        public void WildcraftTree_Trunk_WithInjectedAttrs_Participates()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                EcosystemConfig.Loaded = new EcosystemConfig { EnableThirdPartyParticipants = true };

                string json = @"{
  ""ecologyParticipant"": true,
  ""ecologySpecies"": ""poplar"",
  ""ecologyHabitat"": ""TerrestrialTree"",
  ""ecologySpreadBlock"": ""wildcrafttree:sapling-poplar-free"",
  ""ecologyMatureBlock"": ""wildcrafttree:log-grown-poplar-ud"",
  ""ecologyMinSunlight"": 10,
  ""minTemp"": -5,
  ""maxTemp"": 35,
  ""minRain"": 0.35,
  ""maxRain"": 1.0,
  ""minForest"": 0,
  ""maxForest"": 1.0,
  ""ecologySpreadRate"": 0.45,
  ""ecologySpreadRadius"": 9
}";

                Block trunk = new Block
                {
                    Code = new AssetLocation("wildcrafttree", "log-grown-poplar-ud"),
                    Attributes = JsonObject.FromJson(json),
                };

                Assert.True(PlantCodeHelper.IsThirdPartyEcologyBlock(trunk));
                Assert.Equal("poplar", PlantCodeHelper.ResolveEcologySpecies(trunk));
                Assert.True(EcosystemParticipant.TryFromBlock(trunk, out _));
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }
    }
}

