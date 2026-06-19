using Vintagestory.API.Common;
using WildFarming.Ecosystem;
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
    }
}
