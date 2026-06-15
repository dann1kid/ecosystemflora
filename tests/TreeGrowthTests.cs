using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class TreeGrowthTargetsTests
    {
        [Fact]
        public void Oak_AtMaxAge_ReachesProfileHeight()
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");
            int height = TreeGrowthTargets.TargetTrunkHeight(profile.MaxAgeYears, profile, 1f);
            int radius = TreeGrowthTargets.TargetCrownRadius(profile.MaxAgeYears, profile, 1f);

            Assert.Equal(profile.MaxTrunkHeight, height);
            Assert.Equal(profile.MaxCrownRadius, radius);
        }

        [Fact]
        public void Oak_YoungTree_IsShort()
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");
            int height = TreeGrowthTargets.TargetTrunkHeight(5, profile, 1f);
            Assert.True(height < profile.MaxTrunkHeight / 2);
        }

        [Fact]
        public void EstimateAge_WorldgenSizedTree_IsYoungMature()
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");
            int age = TreeGrowthTargets.EstimateAgeYears(14, 5, profile);

            Assert.InRange(age, 20, 45);
        }

        [Fact]
        public void EstimateAge_FullSizeTree_IsNearMaxAge()
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");
            int age = TreeGrowthTargets.EstimateAgeYears(
                profile.MaxTrunkHeight,
                profile.MaxCrownRadius,
                profile);

            Assert.InRange(age, profile.MaxAgeYears - 5, profile.MaxAgeYears);
        }

        [Fact]
        public void EstimateAge_SmallSapling_IsVeryYoung()
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");
            int age = TreeGrowthTargets.EstimateAgeYears(3, 1, profile);

            Assert.InRange(age, 1, 12);
        }

        [Theory]
        [InlineData(0f, 0f)]
        [InlineData(60f, 0.5f)]
        [InlineData(120f, 1f)]
        public void GrowthFraction_IsMonotonic(int age, float expectedMin)
        {
            float t = TreeGrowthTargets.GrowthFraction(age, 120);
            Assert.True(t >= expectedMin - 0.01f);
        }
    }
}
