using Vintagestory.API.Common;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class WildGrassColonizerEcologyTests
    {
        [Theory]
        [InlineData("game:flower-redtopgrass-free", "redtopgrass")]
        [InlineData("game:flower-redtopgrass-snow", "redtopgrass")]
        public void Redtopgrass_ParsesAsEcologySpecies(string code, string expectedSpecies)
        {
            Assert.Equal(expectedSpecies, PlantCodeHelper.GetEcologySpecies(new AssetLocation(code)));
        }

        [Fact]
        public void Redtopgrass_UsesGrassColonizerEcology_NotFlowerClimate()
        {
            const string species = EcologyGrassColonizerSpecies.Redtopgrass;

            Assert.True(WildGrassColonizerEcology.TryGet(species, out var colonizer));
            Assert.False(WildFlowerClimate.TryGet(species, out _));
            Assert.False(EcologyFlowerSpecies.IsKnownFlower(species));
            Assert.True(WildTallgrassEcology.TryGet("tallgrass", out var matrix));
            Assert.True(colonizer.SpreadRate > matrix.SpreadRate);
        }

        [Fact]
        public void Redtopgrass_StrongerHoldThan_TallgrassMatrix()
        {
            Assert.True(WildSpeciesModifiers.TryGet(EcologyGrassColonizerSpecies.Redtopgrass, out var colonizer));
            Assert.True(WildSpeciesModifiers.TryGet("tallgrass", out var matrix));
            Assert.True(colonizer.HoldStrength > matrix.HoldStrength);
        }

        [Fact]
        public void Redtopgrass_HasGrassColonizerSoilRole()
        {
            Assert.True(WildSpeciesSoilSuccession.TryGetRole(
                EcologyGrassColonizerSpecies.Redtopgrass,
                out PlantSoilRole role));
            Assert.Equal(PlantSoilRole.GrassColonizer, role);
        }
    }
}
