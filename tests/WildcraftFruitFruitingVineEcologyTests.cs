using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class WildcraftFruitFruitingVineEcologyTests
    {
        [Theory]
        [InlineData("fruitvine-blackgrape-ripe-north", "blackgrape", "fruitvine-blackgrape-empty-north")]
        [InlineData("fruitvine-ivy-flowering-west", "ivy", "fruitvine-ivy-empty-west")]
        [InlineData("bottomfruitvine-kiwi-empty-east", "kiwi", "fruitvine-kiwi-empty-north")]
        [InlineData("bottomtreevine-redgrape-north", "redgrape", "fruitvine-redgrape-empty-north")]
        public void TryResolve_FruitingVines(string path, string species, string spreadPath)
        {
            var block = new Block { Code = new AssetLocation("wildcraftfruit", path) };

            Assert.True(WildcraftFruitFruitingVineEcology.TryGetVineType(block, out string resolved));
            Assert.Equal(species, resolved);
            Assert.True(WildcraftFruitFruitingVineEcology.TryGetProfile(species, out WildcraftFruitFruitingVineEcology.Profile profile));
            Assert.True(profile.MaxTemp > profile.MinTemp);
            Assert.True(WildcraftFruitFruitingVineEcology.TryGetSpreadBlock(block, out AssetLocation spread));
            Assert.Equal("wildcraftfruit", spread.Domain);
            Assert.Equal(spreadPath, spread.Path);
        }

        [Theory]
        [InlineData("vinegrowth-blackgrape-alive-north")]
        [InlineData("vineclipping-kiwi-green")]
        [InlineData("berrybush-blackcurrant-ripe")]
        public void TryResolve_NonMatureVineBlocks_ReturnFalse(string path)
        {
            var block = new Block { Code = new AssetLocation("wildcraftfruit", path) };
            Assert.False(WildcraftFruitFruitingVineEcology.IsFruitingVineBlock(block));
        }

        [Fact]
        public void FromBlock_WithInjectedAttrs_HasZeroSpreadRate()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                EcosystemConfig.Loaded = new EcosystemConfig { EnableThirdPartyParticipants = true };

                Block block = new Block
                {
                    Code = new AssetLocation("wildcraftfruit", "fruitvine-passionfruit-ripe-north"),
                    Attributes = JsonObject.FromJson(
                        @"{
  ""ecologyParticipant"": true,
  ""ecologySpecies"": ""passionfruit"",
  ""ecologyHabitat"": ""Terrestrial"",
  ""ecologySpreadBlock"": ""wildcraftfruit:fruitvine-passionfruit-empty-north"",
  ""minTemp"": 21,
  ""maxTemp"": 40,
  ""minRain"": 0.65,
  ""maxRain"": 1,
  ""minForest"": 0.3,
  ""maxForest"": 1,
  ""ecologySpreadRate"": 0
}"),
                };

                PlantRequirements req = PlantRequirements.FromBlock(block);

                Assert.Equal("passionfruit", req.Species);
                Assert.Equal(0f, req.SpreadRate);
                Assert.Equal(21f, req.MinTemp);
                Assert.Equal(40f, req.MaxTemp);
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }

        [Fact]
        public void ZeroSpreadRateGate_BlocksSpread()
        {
            var entry = new ReproducerEntry(
                null,
                null,
                null,
                new PlantRequirements { SpreadRate = 0f },
                0);

            Assert.True(SpreadGateChain.PreSpawn.BlocksSpread(null, entry, new EcosystemConfig()));
        }
    }
}
