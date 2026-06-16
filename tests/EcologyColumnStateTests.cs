using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class EcologyColumnStateTests
    {
        [Fact]
        public void CellKey_DistinguishesAdjacentCells()
        {
            long a = EcologyColumnState.CellKey(1, 64, 10);
            long b = EcologyColumnState.CellKey(1, 65, 10);
            long c = EcologyColumnState.CellKey(2, 64, 10);

            Assert.NotEqual(a, b);
            Assert.NotEqual(a, c);
            Assert.NotEqual(b, c);
        }

        [Fact]
        public void InvalidateAround_RemovesMatchingCell()
        {
            var state = new EcologyColumnState();
            var pos = new Vintagestory.API.MathTools.BlockPos(10, 64, 20);
            state.InvalidateAround(pos, horizontalRadius: 1);

            Assert.Equal(0, state.CacheSize);
        }

        [Fact]
        public void EnableEcologyColumnCache_DefaultsTrue()
        {
            Assert.True(new EcosystemConfig().EnableEcologyColumnCache);
        }
    }
}
