using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class FloralZonesEcologyTests
    {
        [Fact]
        public void ThirdPartyFlower_WithEcologyAttrs_Participates()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                EcosystemConfig.Loaded = new EcosystemConfig { EnableThirdPartyParticipants = true };

                string json = @"{
  ""ecologyParticipant"": true,
  ""ecologySpecies"": ""amaryllisbelladonna"",
  ""ecologyHabitat"": ""Terrestrial"",
  ""ecologySpreadBlock"": ""floralzonescaperegion:flower-amaryllisbelladonna-free"",
  ""minTemp"": 14,
  ""maxTemp"": 18,
  ""minRain"": 0.3,
  ""maxRain"": 0.58,
  ""minForest"": 0,
  ""maxForest"": 0.3,
  ""ecologySpreadRate"": 0.65
}";
                Block block = new Block
                {
                    Code = new AssetLocation("floralzonescaperegion", "flower-amaryllisbelladonna-free"),
                    Attributes = JsonObject.FromJson(json),
                };

                Assert.True(PlantCodeHelper.IsThirdPartyEcologyBlock(block));
                Assert.Equal("amaryllisbelladonna", PlantCodeHelper.ResolveEcologySpecies(block));
                Assert.True(EcosystemParticipant.TryFromBlock(block, out _));

                AssetLocation spread = PlantCodeHelper.SpreadBlockCode(block);
                Assert.Equal("floralzonescaperegion", spread.Domain);
                Assert.Equal("flower-amaryllisbelladonna-free", spread.Path);

                PlantRequirements req = PlantRequirements.FromBlock(block);
                Assert.Equal("amaryllisbelladonna", req.Species);
                Assert.Equal(14f, req.MinTemp);
                Assert.False(FlowerPhenology.IsFlowerSpecies(req.Species));
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }
    }
}
