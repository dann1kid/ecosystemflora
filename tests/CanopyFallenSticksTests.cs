using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Testing;
using Xunit;

namespace WildFarming.Tests
{
    public class CanopyFallenSticksTests
    {
        [Theory]
        [InlineData("game:loosestick-free", true)]
        [InlineData("game:loosestick-snow", true)]
        [InlineData("game:tallgrass-fern-medium-free", false)]
        public void IsLooseStickBlock_DetectsVanillaSticks(string code, bool expected)
        {
            var block = new Block { Code = new AssetLocation(code) };
            Assert.Equal(expected, CanopyFallenSticks.IsLooseStickBlock(block));
        }

        [Theory]
        [InlineData("game:air", true)]
        [InlineData("game:tallgrass-fern-medium-free", true)]
        [InlineData("game:frostedtallgrass-tall-free", true)]
        [InlineData("game:flower-cornflower-free", true)]
        [InlineData("game:fern-eaglefern", true)]
        [InlineData("game:flower-horsetail-free", true)]
        [InlineData("game:loosestick-free", false)]
        [InlineData("game:log-grown-oak-ud", false)]
        [InlineData("game:sapling-oak-free", false)]
        [InlineData("game:fruitingbush-wild-blueberry-free", false)]
        [InlineData("game:tallplant-coopersreed-land-normal-free", false)]
        public void CanStickReplaceFlora_AcceptsMeadowFloraOnly(string code, bool expected)
        {
            var block = new Block { Code = new AssetLocation(code), BlockId = 1 };
            if (code.EndsWith("air"))
            {
                block.BlockId = 0;
            }

            Assert.Equal(expected, CanopyFallenSticks.CanStickReplaceFlora(block));
        }

        [Fact]
        public void TryFindGroundStickCell_ReturnsFirstSurface_NotLowestGap()
        {
            Block air = new Block { BlockId = 0 };
            Block floor = Solid("game:cobblestone", 1);
            var acc = new EcologyTestBlockAccessor(new[] { air, floor }) { MapSizeY = 80 };
            var foliage = new BlockPos(4, 60, 4);
            acc.SetBlock(0, new BlockPos(4, 59, 4));
            acc.SetBlock(1, new BlockPos(4, 58, 4));
            acc.SetBlock(0, new BlockPos(4, 57, 4));
            acc.SetBlock(1, new BlockPos(4, 56, 4));

            Assert.True(CanopyFallenSticks.TryFindGroundStickCell(acc, foliage, out BlockPos stickPos));
            Assert.Equal(59, stickPos.Y);
        }

        [Fact]
        public void TryFindGroundStickCell_StopsAtSolidFloor_DoesNotReachBasement()
        {
            Block air = new Block { BlockId = 0 };
            Block floor = Solid("game:cobblestone", 1);
            var acc = new EcologyTestBlockAccessor(new[] { air, floor }) { MapSizeY = 80 };
            var foliage = new BlockPos(2, 50, 2);
            acc.SetBlock(1, new BlockPos(2, 48, 2));
            acc.SetBlock(0, new BlockPos(2, 47, 2));

            Assert.True(CanopyFallenSticks.TryFindGroundStickCell(acc, foliage, out BlockPos stickPos));
            Assert.Equal(49, stickPos.Y);
        }

        static Block Solid(string code, int id)
        {
            var block = new Block
            {
                BlockId = id,
                Code = new AssetLocation(code),
            };
            block.SideSolid[BlockFacing.UP.Index] = true;
            block.SideSolid[BlockFacing.DOWN.Index] = true;
            block.SideSolid[BlockFacing.NORTH.Index] = true;
            block.SideSolid[BlockFacing.SOUTH.Index] = true;
            block.SideSolid[BlockFacing.EAST.Index] = true;
            block.SideSolid[BlockFacing.WEST.Index] = true;
            return block;
        }
    }
}
