using Vintagestory.API.Common;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class FlowerDrygrassDropsTests
    {
        [Theory]
        [InlineData("game:flower-cornflower-free", true)]
        [InlineData("game:tallplant-brownsedge-land-normal-free", true)]
        [InlineData("game:tallplant-coopersreed-land-normal-free", false)]
        [InlineData("ecosystemflora:juvenile-sedge-brownsedge-free", true)]
        [InlineData("ecosystemflora:juvenile-flower-catmint-free", true)]
        [InlineData("ecosystemflora:flowerphase-catmint-dormant-free", true)]
        public void ShouldPatchBreakDrops_includesShoreSedge(string code, bool expected)
        {
            Assert.Equal(expected, FlowerDrygrassDrops.ShouldPatchBreakDrops(new AssetLocation(code)));
        }
    }
}
