using Vintagestory.API.Common;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class SeasonProfileTests
    {
        [Theory]
        [InlineData("catmint")]
        [InlineData("wilddaisy")]
        [InlineData("tallgrass")]
        [InlineData("bluebell")]
        [InlineData("coopersreed")]
        public void Resolve_KnownSpecies_ReturnsProfile(string species)
        {
            var profile = WildSpeciesSeason.Resolve(species);
            Assert.True(profile.SpringSpread > 0f);
            Assert.True(profile.SummerSpread > 0f);
        }

        [Fact]
        public void Resolve_UnknownSpecies_ReturnsDefault()
        {
            var profile = WildSpeciesSeason.Resolve("nonexistent_plant_xyz");
            Assert.True(profile.SpringSpread > 0f, "Default profile should have positive spring spread");
        }

        [Fact]
        public void TryGet_Null_ReturnsFalse()
        {
            Assert.False(WildSpeciesSeason.TryGet(null, out _));
            Assert.False(WildSpeciesSeason.TryGet("", out _));
        }

        [Theory]
        [InlineData(EnumSeason.Spring)]
        [InlineData(EnumSeason.Summer)]
        [InlineData(EnumSeason.Fall)]
        [InlineData(EnumSeason.Winter)]
        public void SpreadMultiplier_AllSeasons_NonNegative(EnumSeason season)
        {
            var profile = WildSpeciesSeason.Resolve("catmint");
            float mult = profile.SpreadMultiplier(season);
            Assert.True(mult >= 0f, $"Spread multiplier for {season} should be >= 0");
        }

        [Fact]
        public void MeadowAnnual_WinterSpread_NearZero()
        {
            var profile = WildSpeciesSeason.Resolve("wilddaisy");
            Assert.True(profile.WinterSpread < 0.1f, "Annual meadow plants should barely spread in winter");
        }

        [Fact]
        public void MeadowAnnual_SpringSpread_BoostedAboveOne()
        {
            var profile = WildSpeciesSeason.Resolve("wilddaisy");
            Assert.True(profile.SpringSpread > 1f, "Annual meadow plants should have spring boost > 1");
        }

        [Fact]
        public void AquaticWarm_WinterSurvival_Moderate()
        {
            var profile = WildSpeciesSeason.Resolve("coopersreed");
            Assert.True(profile.WinterSurvival > 0.3f, "Aquatic warm species should survive winter moderately");
        }

        [Fact]
        public void ForestPerennial_FallDieoff_Low()
        {
            var profile = WildSpeciesSeason.Resolve("bluebell");
            Assert.True(profile.FallDieoffChance < 0.3f, "Forest perennials should have low fall die-off");
        }
    }
}
