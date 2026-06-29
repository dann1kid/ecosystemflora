using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
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
        [InlineData("ecosystemflora:flowerphase-cornflower-dormant-free", true)]
        [InlineData("ecosystemflora:flowerphase-ghostpipewhite-vegetative-free", true)]
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

        [Theory]
        [InlineData("game:flower-cornflower-free", true)]
        [InlineData("ecosystemflora:flowerphase-cornflower-vegetative-free", true)]
        [InlineData("ecosystemflora:juvenile-flower-cornflower-free", true)]
        [InlineData("game:tallgrass-fern-veryshort-free", false)]
        public void ShouldDropFlowerBlockInWorld_includesPhenologyAndJuvenile(string code, bool expected)
        {
            var block = new Block { Code = new AssetLocation(code) };
            Assert.Equal(expected, PlantHandHarvest.ShouldDropFlowerBlockInWorld(block));
        }

        [Fact]
        public void TryResolveFlowerBlockDrop_mapsPhenologyBlockToVanillaFlower()
        {
            var api = new Mock<ICoreAPI>();
            var world = new Mock<IWorldAccessor>();
            var mature = new Block { Code = new AssetLocation("game", "flower-cornflower-free"), BlockId = 12 };
            api.Setup(a => a.World).Returns(world.Object);
            world.Setup(w => w.GetBlock(It.Is<AssetLocation>(l => l.Path == "flower-cornflower-free")))
                .Returns(mature);

            var phase = new Block { Code = new AssetLocation("ecosystemflora", "flowerphase-cornflower-dormant-free") };

            Assert.True(PlantHandHarvest.TryResolveFlowerBlockDrop(
                api.Object,
                phase,
                new BlockPos(0, 64, 0),
                entry: null,
                out ItemStack drop));

            Assert.Equal(mature.Code, drop.Collectible.Code);
        }

        [Fact]
        public void TryResolveFlowerBlockDrop_prefersRegistryMatureVariant()
        {
            var api = new Mock<ICoreAPI>();
            var world = new Mock<IWorldAccessor>();
            var blueLupine = new Block { Code = new AssetLocation("game", "flower-lupine-blue-free"), BlockId = 11 };
            var purpleLupine = new Block { Code = new AssetLocation("game", "flower-lupine-purple-free"), BlockId = 12 };
            api.Setup(a => a.World).Returns(world.Object);
            world.Setup(w => w.GetBlock(It.Is<AssetLocation>(l => l.Path == "flower-lupine-blue-free")))
                .Returns(blueLupine);
            world.Setup(w => w.GetBlock(It.Is<AssetLocation>(l => l.Path == "flower-lupine-purple-free")))
                .Returns(purpleLupine);

            var entry = new ReproducerEntry(
                null,
                null,
                purpleLupine.Code,
                new PlantRequirements { Species = "lupine", Habitat = EcologyHabitat.Terrestrial },
                0);
            var phase = new Block { Code = new AssetLocation("ecosystemflora", "flowerphase-lupine-vegetative-free") };

            Assert.True(PlantHandHarvest.TryResolveFlowerBlockDrop(
                api.Object,
                phase,
                new BlockPos(0, 64, 0),
                entry,
                out ItemStack drop));

            Assert.Equal(purpleLupine.Code, drop.Collectible.Code);
        }

        [Fact]
        public void TryResolveFlowerBlockDrop_mapsGhostpipePhaseToVanillaFlower()
        {
            var api = new Mock<ICoreAPI>();
            var world = new Mock<IWorldAccessor>();
            var mature = new Block { Code = new AssetLocation("game", "flower-ghostpipewhite-free"), BlockId = 12 };
            api.Setup(a => a.World).Returns(world.Object);
            world.Setup(w => w.GetBlock(It.Is<AssetLocation>(l => l.Path == "flower-ghostpipewhite-free")))
                .Returns(mature);

            var phase = new Block { Code = new AssetLocation("ecosystemflora", "flowerphase-ghostpipewhite-dormant-free") };

            Assert.True(PlantHandHarvest.TryResolveFlowerBlockDrop(
                api.Object,
                phase,
                new BlockPos(0, 64, 0),
                entry: null,
                out ItemStack drop));

            Assert.Equal(mature.Code, drop.Collectible.Code);
        }
    }
}
