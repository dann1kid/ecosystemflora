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
        [InlineData("tule")]
        public void Resolve_KnownSpecies_ReturnsProfile(string species)
        {
            var profile = WildSpeciesSeason.Resolve(species);
            Assert.True(profile.SpreadMultiplier(EnumSeason.Spring) > 0f);
            Assert.True(profile.SpreadMultiplier(EnumSeason.Summer) > 0f);
        }

        [Fact]
        public void Resolve_UnknownSpecies_ReturnsDefault()
        {
            var profile = WildSpeciesSeason.Resolve("nonexistent_plant_xyz");
            Assert.True(profile.SpreadMultiplier(EnumSeason.Spring) > 0f,
                "Default profile should have positive spring spread");
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
        public void MeadowSummer_WinterSpread_NearZero()
        {
            var profile = WildSpeciesSeason.Resolve("wilddaisy");
            Assert.True(profile.SpreadMultiplier(EnumSeason.Winter) < 0.1f,
                "Summer meadow annuals should barely spread in winter");
        }

        [Fact]
        public void MeadowSummer_PeakMonth_AboveOne()
        {
            var profile = WildSpeciesSeason.Resolve("wilddaisy");
            float june = profile.SpreadMultiplierInterpolated(5f / 12f);
            Assert.True(june > 1f, "Daisy should have peak spread > 1 in June");
        }

        [Fact]
        public void EarlySpring_MarchApril_Peak()
        {
            var profile = WildSpeciesSeason.Resolve("daffodil");
            float april = profile.SpreadMultiplierInterpolated(3f / 12f);
            float july = profile.SpreadMultiplierInterpolated(6f / 12f);
            Assert.True(april > july, "Daffodil should peak in spring, not summer");
        }

        [Fact]
        public void Aquatic_WinterStress_Moderate()
        {
            var profile = WildSpeciesSeason.Resolve("coopersreed");
            float janStress = profile.StressChance(0);
            Assert.True(janStress > 0f && janStress < 0.7f,
                "Aquatic species should have moderate winter stress");
        }

        [Fact]
        public void ForestSpring_FallStress_Low()
        {
            var profile = WildSpeciesSeason.Resolve("bluebell");
            float octStress = profile.StressChance(9);
            Assert.True(octStress < 0.3f, "Forest perennials should have low fall stress");
        }

        [Fact]
        public void MonthlyInterpolation_SmoothTransition()
        {
            var profile = WildSpeciesSeason.Resolve("catmint");
            float prev = profile.SpreadMultiplierInterpolated(0f);
            bool smooth = true;
            for (int i = 1; i <= 24; i++)
            {
                float curr = profile.SpreadMultiplierInterpolated(i / 24f);
                if (System.Math.Abs(curr - prev) > 1.0f) smooth = false;
                prev = curr;
            }
            Assert.True(smooth, "Monthly interpolation should produce smooth transitions");
        }

        [Fact]
        public void LateSummer_AugustPeak()
        {
            var profile = WildSpeciesSeason.Resolve("heather");
            float aug = profile.SpreadMultiplierInterpolated(7f / 12f);
            float mar = profile.SpreadMultiplierInterpolated(2f / 12f);
            Assert.True(aug > mar, "Heather should peak in late summer, not early spring");
        }

        [Fact]
        public void NoStress_InSummer()
        {
            var profile = WildSpeciesSeason.Resolve("wilddaisy");
            Assert.Equal(0f, profile.StressChance(5));
            Assert.Equal(0f, profile.StressChance(6));
            Assert.Equal(0f, profile.StressChance(7));
        }
    }
}
