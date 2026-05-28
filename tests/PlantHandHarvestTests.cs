using Vintagestory.API.Common;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class PlantHandHarvestTests
    {
        [Theory]
        [InlineData("game:flower-cornflower-free", true)]
        [InlineData("game:flower-horsetail-free", true)]
        [InlineData("game:flower-ghostpipe-white-free", false)]
        [InlineData("game:tallgrass-fern-veryshort-free", true)]
        [InlineData("game:tallgrass-eaten-fern-free", false)]
        [InlineData("game:frostedtallgrass-fern-free", true)]
        [InlineData("game:fern-cinnamon-free", false)]
        public void IsMeadowPlant_matches_flowers_and_tallgrass(string code, bool expected)
        {
            var block = new Block { Code = new AssetLocation(code) };
            Assert.Equal(expected, PlantHandHarvest.IsMeadowPlant(block));
        }
    }
}
