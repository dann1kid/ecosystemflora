using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class SoilSuccessionGuardTests
    {
        [Theory]
        [InlineData("game", "soil-medium-normal", false)]
        [InlineData("game", "cobblestoneslab-granite-down-free", true)]
        [InlineData("terrainslabs", "soil-low-normal", true)]
        [InlineData("terrainslabs", "forestfloor-3", true)]
        [InlineData("game", "forestfloor-3", false)]
        public void IsSlabProtectedBlock_DetectsSlabsAndTerrainSlabs(string domain, string path, bool expected)
        {
            Assert.Equal(expected, SoilSuccessionGuard.IsSlabProtectedBlock(domain, path));
        }
    }
}
