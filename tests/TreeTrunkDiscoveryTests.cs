using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class TreeTrunkDiscoveryTests
    {
        [Fact]
        public void ScanChunkColumns_NullAccessor_ReturnsCompleted()
        {
            TreeTrunkDiscovery.ScanResult result = TreeTrunkDiscovery.ScanChunkColumns(
                null,
                new Vec2i(0, 0),
                (_, __) => true,
                maxHits: 4,
                maxColumns: 8);

            Assert.True(result.Completed);
            Assert.Equal(0, result.TreesFound);
            Assert.Equal(0, result.ColumnsScanned);
        }
    }
}
