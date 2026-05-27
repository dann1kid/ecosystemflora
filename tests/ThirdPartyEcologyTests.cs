using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class ThirdPartyEcologyTests
    {
        [Theory]
        [InlineData("Terrestrial", EcologyHabitat.Terrestrial)]
        [InlineData("terrestrialtree", EcologyHabitat.TerrestrialTree)]
        [InlineData("ReedNearWater", EcologyHabitat.ReedNearWater)]
        [InlineData("watersurface", EcologyHabitat.WaterSurface)]
        [InlineData("UnderwaterColumn", EcologyHabitat.UnderwaterColumn)]
        public void ParseEcologyHabitat_ParsesNames(string raw, EcologyHabitat expected)
        {
            Assert.Equal(expected, PlantCodeHelper.ParseEcologyHabitat(raw));
        }

        [Fact]
        public void ParseEcologyHabitat_Unknown_DefaultsTerrestrial()
        {
            Assert.Equal(EcologyHabitat.Terrestrial, PlantCodeHelper.ParseEcologyHabitat("not-a-habitat"));
        }

        [Fact]
        public void ResolveEcologyAsset_WithDomain_SplitsDomainAndPath()
        {
            var loc = PlantCodeHelper.ResolveEcologyAsset("mydomain:plants/wildgrass-free", "game");
            Assert.Equal("mydomain", loc.Domain);
            Assert.Equal("plants/wildgrass-free", loc.Path);
        }

        [Fact]
        public void ResolveEcologyAsset_NoDomain_UsesFallback()
        {
            var loc = PlantCodeHelper.ResolveEcologyAsset("plants/wildgrass-free", "ecograss");
            Assert.Equal("ecograss", loc.Domain);
            Assert.Equal("plants/wildgrass-free", loc.Path);
        }
    }
}
