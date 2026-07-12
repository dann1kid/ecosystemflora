using WildFarming.Ecosystem;
using Vintagestory.API.Common;
using Xunit;

namespace WildFarming.Tests
{
    public class VanillaFernSpeciesParserTests
    {
        [Theory]
        [InlineData("fern-eaglefern", "eaglefern")]
        [InlineData("fern-eaglefern-free", "eaglefern")]
        [InlineData("fern-eaglefern-snow", "eaglefern")]
        [InlineData("fern-eaglefern-normal", "eaglefern")]
        [InlineData("fern-eaglefern-normal-free", "eaglefern")]
        [InlineData("fern-eaglefern-normal-snow", "eaglefern")]
        [InlineData("fern-cinnamonfern-short-free", "cinnamonfern")]
        [InlineData("fern-deerfern-normal-free", "deerfern")]
        public void TryParseSpeciesFromPath_RecognizesVanillaShapeVariants(string path, string expected)
        {
            Assert.Equal(expected, VanillaFernSpeciesParser.TryParseSpeciesFromPath(path));
            Assert.Equal(expected, PlantCodeHelper.GetEcologySpecies(new AssetLocation("game", path)));
        }

        [Fact]
        public void IsEcologySpreadParent_AcceptsEaglefernNormalVariant()
        {
            var block = new Block
            {
                BlockId = 5,
                Code = new AssetLocation("game:fern-eaglefern-normal-free"),
                Replaceable = 3000,
                BlockMaterial = EnumBlockMaterial.Plant,
            };

            Assert.True(PlantCodeHelper.IsEcologySpreadParent(block));
            Assert.True(EcosystemParticipant.TryFromBlock(block, out _));
        }
    }
}
