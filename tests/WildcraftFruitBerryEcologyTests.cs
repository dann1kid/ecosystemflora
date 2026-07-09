using Vintagestory.API.Common;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class WildcraftFruitBerryEcologyTests
    {
        [Theory]
        [InlineData("berrybush-blackcurrant-ripe", "blackcurrant", "berrybush-blackcurrant-empty")]
        [InlineData("berrybush-redcurrant-flowering", "redcurrant", "berrybush-redcurrant-empty")]
        [InlineData("shortberrybush-blueberry-empty", "blueberry", "shortberrybush-blueberry-empty")]
        [InlineData("groundberryplant-strawberry-ripe", "strawberry", "groundberryplant-strawberry-empty")]
        [InlineData("pricklyberrybush-brambleberry-flowering", "blackberry", "pricklyberrybush-brambleberry-empty")]
        [InlineData("pricklyberrybush-raspberry-ripe", "raspberry", "pricklyberrybush-raspberry-empty")]
        [InlineData("toppricklybush-loganberry-ripe", "loganberry", "pricklyberrybush-loganberry-empty")]
        [InlineData("bottomberrybush-huckleberry-flowering", "huckleberry", "berrybush-huckleberry-empty")]
        [InlineData("shrubberrybush-gooseberry-empty", "gooseberry", "shrubberrybush-gooseberry-empty")]
        public void TryResolve_VanillaEquivalentBerries(string path, string species, string spreadPath)
        {
            var block = new Block { Code = new AssetLocation("wildcraftfruit", path) };

            Assert.True(WildcraftFruitBerryEcology.TryGetEcologySpecies(block, out string resolved));
            Assert.Equal(species, resolved);
            Assert.True(WildcraftFruitBerryEcology.TryGetSpreadBlock(block, out AssetLocation spread));
            Assert.Equal("wildcraftfruit", spread.Domain);
            Assert.Equal(spreadPath, spread.Path);
        }

        [Theory]
        [InlineData("berrybush-blackcurrant-clipping")]
        [InlineData("berrybushcutting-blackcurrant-free")]
        [InlineData("game:fruitingbush-wild-blueberry-free")]
        public void TryResolve_UnsupportedOrForeignBlocks_ReturnFalse(string code)
        {
            int colon = code.IndexOf(':');
            string domain = colon > 0 ? code.Substring(0, colon) : "wildcraftfruit";
            string path = colon > 0 ? code.Substring(colon + 1) : code;
            var block = new Block { Code = new AssetLocation(domain, path) };

            Assert.False(WildcraftFruitBerryEcology.TryGetEcologySpecies(block, out _));
        }

        [Fact]
        public void IsThirdPartyEcologyBlock_WildcraftFruitBerry_WithoutJsonAttrs()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                EcosystemConfig.Loaded = new EcosystemConfig { EnableThirdPartyParticipants = true };
                Block block = new Block { Code = new AssetLocation("wildcraftfruit", "berrybush-blackcurrant-ripe") };

                Assert.True(PlantCodeHelper.IsThirdPartyEcologyBlock(block));
                Assert.False(PlantCodeHelper.HasDeclaredEcologyParticipant(block));
                Assert.Equal("blackcurrant", PlantCodeHelper.ResolveEcologySpecies(block));
                Assert.True(EcosystemParticipant.TryFromBlock(block, out _));
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }

        [Fact]
        public void TryResolve_WcOnlyBerry_UsesBerryTypeAsSpecies()
        {
            var block = new Block { Code = new AssetLocation("wildcraftfruit", "berrybush-huckleberry-ripe") };

            Assert.True(WildcraftFruitBerryEcology.TryGetEcologySpecies(block, out string species));
            Assert.Equal("huckleberry", species);
        }

        [Fact]
        public void FromBlock_WildcraftFruitBlueberry_UsesWildBerryProfile()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                EcosystemConfig.Loaded = new EcosystemConfig
                {
                    EnableThirdPartyParticipants = true,
                    EnableBerryColonySpread = true,
                };

                Block block = new Block { Code = new AssetLocation("wildcraftfruit", "shortberrybush-blueberry-ripe") };
                PlantRequirements req = PlantRequirements.FromBlock(block);

                Assert.Equal("blueberry", req.Species);
                Assert.True(req.UsesBerryColonySpread);
                Assert.Equal(-2f, req.MinTemp);
                Assert.Equal(18f, req.MaxTemp);
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }
    }
}
