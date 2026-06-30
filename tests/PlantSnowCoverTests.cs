using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Testing;
using Xunit;

namespace WildFarming.Tests
{
    public class PlantSnowCoverTests
    {
        [Theory]
        [InlineData("ecosystemflora:juvenile-flower-cornflower-free", false)]
        [InlineData("ecosystemflora:juvenile-flower-cornflower-snow", true)]
        [InlineData("game:flower-cornflower-snow", true)]
        public void PathHasSnowCover_DetectsSuffix(string code, bool expected)
        {
            Assert.Equal(expected, PlantSnowCover.PathHasSnowCover(new AssetLocation(code).Path));
        }

        [Fact]
        public void ShouldUseSnowVariant_InheritsFromSnowParent()
        {
            Block air = new Block { BlockId = 0 };
            Block snowFlower = new Block
            {
                BlockId = 1,
                Code = new AssetLocation("game:flower-catmint-snow"),
            };
            var blocks = new[] { air, snowFlower };
            var acc = new EcologyTestBlockAccessor(blocks);
            acc.SetBlock(1, new BlockPos(5, 64, 5));

            var world = new Mock<IWorldAccessor>();
            world.Setup(w => w.BlockAccessor).Returns(acc);
            var api = new Mock<ICoreAPI>();
            api.Setup(a => a.World).Returns(world.Object);

            bool snow = PlantSnowCover.ShouldUseSnowVariant(
                api.Object,
                new BlockPos(5, 64, 5),
                new BlockPos(6, 64, 6));

            Assert.True(snow);
        }

        [Fact]
        public void CodeWithCover_SwapsFreeAndSnowSuffix()
        {
            var free = new AssetLocation("game:flower-cornflower-free");
            var snow = PlantSnowCover.CodeWithCover(free, snow: true);
            Assert.Equal("game:flower-cornflower-snow", snow.ToString());

            var back = PlantSnowCover.CodeWithCover(snow, snow: false);
            Assert.Equal("game:flower-cornflower-free", back.ToString());
        }

        [Fact]
        public void CodeWithCover_LegacyFernPhase_UsesBareCodeForFree()
        {
            var bare = new AssetLocation("ecosystemflora:fernphase-eaglefern-dieback");
            var snow = PlantSnowCover.CodeWithCover(bare, snow: true);
            Assert.Equal("ecosystemflora:fernphase-eaglefern-dieback-snow", snow.ToString());

            var back = PlantSnowCover.CodeWithCover(snow, snow: false);
            Assert.Equal("ecosystemflora:fernphase-eaglefern-dieback", back.ToString());
        }

        [Fact]
        public void BlockHasCoverVariant_IncludesLegacyFernPhasePaths()
        {
            Assert.True(PlantSnowCover.BlockHasCoverVariant("fernphase-eaglefern-dieback"));
            Assert.True(PlantSnowCover.BlockHasCoverVariant("fernphase-eaglefern-dieback-snow"));
            Assert.False(PlantSnowCover.BlockHasCoverVariant("fern-eaglefern-normal"));
        }

        [Fact]
        public void FernPhenologyBlocks_CodeForPhase_UsesLegacyPathsWithoutFreeSuffix()
        {
            AssetLocation dieback = FernPhenologyBlocks.CodeForPhase("eaglefern", FernPhenologyPhase.Dieback);
            Assert.Equal("ecosystemflora:fernphase-eaglefern-dieback", dieback.ToString());

            AssetLocation snow = FernPhenologyBlocks.CodeForPhase("eaglefern", FernPhenologyPhase.Dieback, snow: true);
            Assert.Equal("ecosystemflora:fernphase-eaglefern-dieback-snow", snow.ToString());
        }

        [Theory]
        [InlineData("ecosystemflora:fernphase-eaglefern-dieback-free", "ecosystemflora:fernphase-eaglefern-dieback")]
        [InlineData("ecosystemflora:fernphase-cinnamonfern-dormant-free", "ecosystemflora:fernphase-cinnamonfern-dormant")]
        [InlineData("ecosystemflora:fernphase-eaglefern-dieback-snow", null)]
        public void LegacyPhaseBlockMigration_RemapsMistakenFreeSuffix(string from, string expected)
        {
            AssetLocation target = LegacyPhaseBlockMigration.ResolveRemapTarget(new AssetLocation(from));
            if (expected == null)
            {
                Assert.Null(target);
                return;
            }

            Assert.Equal(expected, target.ToString());
        }

        [Fact]
        public void ClimateWantsSnowCover_WhenTemperatureBelowFreezing()
        {
            Block air = new Block { BlockId = 0 };
            Block flower = new Block
            {
                BlockId = 1,
                Code = new AssetLocation("ecosystemflora:flowerphase-cornflower-dormant-free"),
            };
            var acc = new EcologyTestBlockAccessor(blocks: new[] { air, flower })
            {
                Temperature = -5f,
            };
            acc.SetBlock(1, new BlockPos(4, 64, 4));

            var world = new Mock<IWorldAccessor>();
            world.Setup(w => w.BlockAccessor).Returns(acc);
            var api = new Mock<ICoreAPI>();
            api.Setup(a => a.World).Returns(world.Object);

            Assert.True(PlantSnowCover.ClimateWantsSnowCover(api.Object, new BlockPos(4, 64, 4)));
            Assert.True(PlantSnowCover.ResolveWantsSnowCover(api.Object, new BlockPos(4, 64, 4)));
        }

        [Fact]
        public void TrySyncCover_SwitchesPhaseBlockToSnowInWinter()
        {
            Block air = new Block { BlockId = 0 };
            Block freePhase = new Block
            {
                BlockId = 1,
                Code = new AssetLocation("ecosystemflora:flowerphase-cornflower-dormant-free"),
            };
            Block snowPhase = new Block
            {
                BlockId = 2,
                Code = new AssetLocation("ecosystemflora:flowerphase-cornflower-dormant-snow"),
            };
            var acc = new EcologyTestBlockAccessor(new[] { air, freePhase, snowPhase })
            {
                Temperature = -8f,
            };
            var pos = new BlockPos(3, 64, 3);
            acc.SetBlock(1, pos);

            var world = new Mock<IWorldAccessor>();
            world.Setup(w => w.BlockAccessor).Returns(acc);
            world.Setup(w => w.GetBlock(It.IsAny<AssetLocation>()))
                .Returns((AssetLocation loc) =>
                {
                    if (loc.Path.Contains("-snow")) return snowPhase;
                    if (loc.Path.Contains("-free")) return freePhase;
                    return air;
                });
            var api = new Mock<ICoreAPI>();
            api.Setup(a => a.World).Returns(world.Object);

            Assert.True(PlantSnowCoverSync.TrySyncCover(api.Object, pos));

            Block placed = acc.GetBlock(pos);
            Assert.True(PlantSnowCover.PathHasSnowCover(placed.Code.Path));
        }

        [Fact]
        public void EnvironmentWantsSnowCover_DetectsSnowLayerAbove()
        {
            Block air = new Block { BlockId = 0 };
            Block snowLayer = new Block
            {
                BlockId = 2,
                Code = new AssetLocation("game:snowlayer-1"),
                BlockMaterial = EnumBlockMaterial.Snow,
            };
            Block flower = new Block
            {
                BlockId = 1,
                Code = new AssetLocation("ecosystemflora:flowerphase-cornflower-dormant-free"),
            };
            var blocks = new[] { air, flower, snowLayer };
            var acc = new EcologyTestBlockAccessor(blocks);
            acc.SetBlock(1, new BlockPos(4, 64, 4));
            acc.SetBlock(2, new BlockPos(4, 65, 4));

            var world = new Mock<IWorldAccessor>();
            world.Setup(w => w.BlockAccessor).Returns(acc);
            var api = new Mock<ICoreAPI>();
            api.Setup(a => a.World).Returns(world.Object);

            Assert.True(PlantSnowCover.EnvironmentWantsSnowCover(api.Object, new BlockPos(4, 64, 4)));
        }

        [Fact]
        public void FlowerJuvenileBlocks_CodeForSpecies_ResolvesSnowVariant()
        {
            AssetLocation snow = FlowerJuvenileBlocks.CodeForSpecies("cornflower", snow: true);
            Assert.Equal("ecosystemflora", snow.Domain);
            Assert.Equal("juvenile-flower-cornflower-snow", snow.Path);

            string species = FlowerJuvenileBlocks.SpeciesFromJuvenileCode(snow);
            Assert.Equal("cornflower", species);
        }
    }
}
