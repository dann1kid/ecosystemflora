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
        [InlineData("game:tallplant-brownsedge-land-normal-free", true)]
        [InlineData("ecosystemflora:juvenile-sedge-brownsedge-free", true)]
        [InlineData("game:fern-cinnamon-free", false)]
        public void IsMeadowPlant_matches_flowers_and_tallgrass(string code, bool expected)
        {
            var block = new Block { Code = new AssetLocation(code) };
            Assert.Equal(expected, PlantHandHarvest.IsMeadowPlant(block));
        }

        [Theory]
        [InlineData("game:flower-cornflower-free", true)]
        [InlineData("game:flower-horsetail-free", true)]
        [InlineData("game:flower-ghostpipe-white-free", false)]
        [InlineData("game:tallgrass-fern-veryshort-free", false)]
        [InlineData("game:frostedtallgrass-fern-free", false)]
        public void DropsWholePlantBlock_flowersOnly(string code, bool expected)
        {
            var block = new Block { Code = new AssetLocation(code) };
            Assert.Equal(expected, PlantHandHarvest.DropsWholePlantBlock(block));
        }

        [Fact]
        public void IsMowTool_detectsKnifeAndScythe()
        {
            Assert.False(PlantHandHarvest.IsMowTool((Item)null));
            Assert.True(PlantHandHarvest.IsMowTool(new Item { Tool = EnumTool.Knife }));
            Assert.True(PlantHandHarvest.IsMowTool(new Item { Tool = EnumTool.Scythe }));
            Assert.False(PlantHandHarvest.IsMowTool(new Item { Tool = EnumTool.Pickaxe }));
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("whole", true)]
        [InlineData("delegate", false)]
        [InlineData("none", false)]
        public void MeadowHarvestModes_defaultWholeDrop(string mode, bool allowsDefault)
        {
            Assert.Equal(allowsDefault, MeadowHarvestModes.AllowsDefaultWholeDrop(mode ?? "whole"));
        }
    }
}
