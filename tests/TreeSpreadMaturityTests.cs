using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class TreeSpreadMaturityTests
    {
        [Theory]
        [InlineData(12, 4, "birch", true)]
        [InlineData(14, 5, "oak", true)]
        [InlineData(8, 3, "birch", true)]
        [InlineData(1, 1, "birch", false)]
        [InlineData(3, 1, "birch", false)]
        public void MeetsStructuralBypass_worldgenSizedTrees(
            int trunkHeight,
            int crownRadius,
            string wood,
            bool expected)
        {
            var metrics = new TreeStructureMetrics(trunkHeight, crownRadius, null);
            var profile = WildTreeGrowthProfiles.Resolve(wood);
            var cfg = new EcosystemConfig { TreeYoungSpreadBypassTrunkHeight = 14 };

            bool actual = TreeSpreadMaturity.MeetsStructuralBypass(metrics, profile, cfg);

            Assert.Equal(expected, actual);
        }
    }
}
