using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class TreeGrowthTargetsTests
    {
        [Fact]
        public void Oak_MaxTargets_MatchProfile()
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");
            int height = TreeGrowthTargets.MaxTargetTrunkHeight(profile, 1f);
            int radius = TreeGrowthTargets.MaxTargetCrownRadius(profile, 1f);

            Assert.Equal(profile.MaxTrunkHeight, height);
            Assert.Equal(profile.MaxCrownRadius, radius);
        }

        [Fact]
        public void Oak_WorldgenSizedTree_IsPartiallyMature()
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");
            int pct = TreeGrowthTargets.MaturityPercent(14, 5, profile);

            Assert.InRange(pct, 35, 65);
        }

        [Fact]
        public void Oak_FullSizeTree_IsFullyMature()
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");
            int pct = TreeGrowthTargets.MaturityPercent(
                profile.MaxTrunkHeight,
                profile.MaxCrownRadius,
                profile);

            Assert.Equal(100, pct);
        }

        [Fact]
        public void Oak_SmallSapling_IsLowMaturity()
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");
            int pct = TreeGrowthTargets.MaturityPercent(3, 1, profile);

            Assert.InRange(pct, 5, 25);
        }

        [Theory]
        [InlineData(10, 3, 0.35f)]
        [InlineData(34, 8, 1f)]
        public void MaturityFraction_ScalesWithStructure(int trunk, int crown, float expectedMin)
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");
            float fraction = TreeGrowthTargets.MaturityFraction(trunk, crown, profile);

            Assert.InRange(fraction, expectedMin - 0.05f, 1f);
        }
    }
}
