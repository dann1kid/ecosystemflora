using Vintagestory.API.Common;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class BrownsedgeMowBreakTests
    {
        static Block Block(string code) =>
            new Block { Code = new AssetLocation(code) };

        [Fact]
        public void IsBrownsedgeMowHarvestTransition_NormalToHarvested_IsTrue()
        {
            Block oldBlock = Block("game:tallplant-brownsedge-land-normal-free");
            Block newBlock = Block("game:tallplant-brownsedge-land-harvested-free");
            Assert.True(PlantCodeHelper.IsBrownsedgeMowHarvestTransition(oldBlock, newBlock));
        }

        [Fact]
        public void IsBrownsedgeMowHarvestTransition_HarvestedToAir_IsFalse()
        {
            Block oldBlock = Block("game:tallplant-brownsedge-land-harvested-free");
            Block newBlock = new Block { BlockId = 0 };
            Assert.False(PlantCodeHelper.IsBrownsedgeMowHarvestTransition(oldBlock, newBlock));
        }

        [Fact]
        public void CountsAsEcologyPlantRemovalForWake_HarvestedSedge_IsFalse()
        {
            Block harvested = Block("game:tallplant-brownsedge-land-harvested-free");
            Assert.False(PlantCodeHelper.CountsAsEcologyPlantRemovalForWake(harvested));
        }

        [Fact]
        public void CountsAsEcologyPlantRemovalForWake_NormalSedge_IsTrue()
        {
            Block normal = Block("game:tallplant-brownsedge-land-normal-free");
            Assert.True(PlantCodeHelper.CountsAsEcologyPlantRemovalForWake(normal));
        }

        [Fact]
        public void CountsAsEcologyPlantRemovalForWake_EatenTallgrass_IsFalse()
        {
            Block eaten = Block("game:tallgrass-eaten-short-free");
            Assert.False(PlantCodeHelper.CountsAsEcologyPlantRemovalForWake(eaten));
        }
    }
}
