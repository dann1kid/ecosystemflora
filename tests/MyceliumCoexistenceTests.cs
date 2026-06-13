using Vintagestory.API.Common;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class MyceliumCoexistenceTests
    {
        [Fact]
        public void IsForestMyceliumNiche_MeadowOpen_IsFalse()
        {
            Assert.False(MyceliumCoexistence.IsForestMyceliumNiche(MyceliumNiche.MeadowOpen));
        }

        [Theory]
        [InlineData(MyceliumNiche.ForestAnyTree)]
        [InlineData(MyceliumNiche.ForestDeciduous)]
        [InlineData(MyceliumNiche.ForestConifer)]
        public void IsForestMyceliumNiche_ForestTypes_AreTrue(MyceliumNiche niche)
        {
            Assert.True(MyceliumCoexistence.IsForestMyceliumNiche(niche));
        }

        [Fact]
        public void IsMeadowTerrestrialPlant_Wilddaisy_IsTrue()
        {
            var req = new PlantRequirements
            {
                Species = "wilddaisy",
                Habitat = EcologyHabitat.Terrestrial,
            };

            Assert.True(MyceliumCoexistence.IsMeadowTerrestrialPlant(req));
        }

        [Fact]
        public void IsMeadowPlantBlock_FlowerCode_IsTrue()
        {
            var block = new Block { Code = new AssetLocation("game", "flower-wilddaisy-free") };

            Assert.True(MyceliumCoexistence.IsMeadowPlantBlock(block));
        }

        [Fact]
        public void IsMeadowPlantBlock_TreeLog_IsFalse()
        {
            var block = new Block { Code = new AssetLocation("game", "log-grown-oak-ud") };

            Assert.False(MyceliumCoexistence.IsMeadowPlantBlock(block));
        }
    }
}
