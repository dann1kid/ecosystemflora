using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class TreeGrowthTargetsTests
    {
        [Fact]
        public void Oak_WorldgenSizedTree_IsNearReferenceIndex()
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");
            int pct = TreeGrowthTargets.SizeIndexPercent(14, 5, profile);

            Assert.InRange(pct, 95, 105);
        }

        [Fact]
        public void Oak_TallTree_ExceedsReferenceIndex()
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");
            int pct = TreeGrowthTargets.SizeIndexPercent(28, 8, profile);

            Assert.True(pct > 130);
        }

        [Fact]
        public void Oak_Sapling_IsLowSizeIndex()
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");
            int pct = TreeGrowthTargets.SizeIndexPercent(3, 1, profile);

            Assert.InRange(pct, 15, 35);
        }

        [Fact]
        public void SenescenceAge_IsCalendarHorizon_NotStructure()
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");

            Assert.True(profile.SenescenceAgeYears >= 80);
            Assert.True(profile.ReferenceTrunkHeight < 20);
        }

        [Theory]
        [InlineData(10, 3, 0.6f)]
        [InlineData(14, 5, 0.95f)]
        public void SizeIndexFraction_ScalesWithStructure(int trunk, int crown, float expectedMin)
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");
            float fraction = TreeGrowthTargets.SizeIndexFraction(trunk, crown, profile);

            Assert.InRange(fraction, expectedMin, 2.5f);
        }
    }
}
