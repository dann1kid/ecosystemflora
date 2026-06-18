using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class RegistrationChunkSnapshotTests
    {
        [Fact]
        public void CellIndex_RoundTrips()
        {
            int mapSizeY = 128;
            int idx = RegistrationChunkSnapshot.CellIndex(5, 7, 42, mapSizeY);
            Assert.Equal((5 * 32 + 7) * mapSizeY + 42, idx);
        }

        [Fact]
        public void GetBlockId_OutOfRange_ReturnsZero()
        {
            var snap = new RegistrationChunkSnapshot(
                new Vec2i(0, 0),
                mapSizeY: 4,
                rainHeightMap: new ushort[32 * 32],
                blockIds: new int[32 * 32 * 4]);

            Assert.Equal(0, snap.GetBlockId(-1, 0, 0));
            Assert.Equal(0, snap.GetBlockId(0, 0, 99));
        }
    }
}
