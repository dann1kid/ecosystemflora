using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class EcologyColumnOccupancyTests
    {
        [Fact]
        public void TracksColumnOccupancyPerChunk()
        {
            var occupancy = new EcologyColumnOccupancy();
            var pos = new BlockPos(35, 64, 35);

            occupancy.OnPlantAdded(pos);
            Assert.True(occupancy.IsOccupied(35, 35));

            occupancy.OnPlantRemoved(pos, remainingInChunk: null);
            Assert.False(occupancy.IsOccupied(35, 35));
        }

        [Fact]
        public void KeepsOccupiedWhenAnotherPlantRemainsInColumn()
        {
            var occupancy = new EcologyColumnOccupancy();
            var a = new BlockPos(35, 64, 35);
            var b = new BlockPos(35, 65, 35);

            occupancy.OnPlantAdded(a);
            occupancy.OnPlantAdded(b);

            occupancy.OnPlantRemoved(a, new[] { b });
            Assert.True(occupancy.IsOccupied(35, 35));
        }

        [Fact]
        public void EmptyFirstSpreadCollect_DefaultsTrue()
        {
            var cfg = new EcosystemConfig();
            Assert.True(cfg.EnableEmptyFirstSpreadCollect);
            Assert.True(cfg.EnableSpreadColumnOccupancyHint);
        }
    }
}
