using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Testing;
using Xunit;

namespace WildFarming.Tests
{
    public class PlantSnowCoverTests
    {
        static void SetSnowAccum(EcologyTestBlockAccessor acc, BlockPos pos, float value)
        {
            int cs = GlobalConstants.ChunkSize;
            EcologyTestMapChunk chunk = acc.GetOrCreateMapChunk(pos.X / cs, pos.Z / cs);
            chunk.SnowAccum ??= new float[cs * cs];
            int idx = (pos.Z % cs) * cs + (pos.X % cs);
            chunk.SnowAccum[idx] = value;
        }

        static void PlaceSnowLayerAbove(EcologyTestBlockAccessor acc, BlockPos plantPos, Block snowLayer)
        {
            acc.SetBlock(snowLayer.BlockId, plantPos.UpCopy());
        }
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
            Block snowLayer = new Block
            {
                BlockId = 2,
                Code = new AssetLocation("game:snowlayer-1"),
                BlockMaterial = EnumBlockMaterial.Snow,
            };
            var blocks = new[] { air, snowFlower, snowLayer };
            var acc = new EcologyTestBlockAccessor(blocks)
            {
                Temperature = -4f,
            };
            var parentPos = new BlockPos(5, 64, 5);
            var childPos = new BlockPos(6, 64, 6);
            acc.SetBlock(1, parentPos);
            PlaceSnowLayerAbove(acc, childPos, snowLayer);

            var world = new Mock<IWorldAccessor>();
            world.Setup(w => w.BlockAccessor).Returns(acc);
            var api = new Mock<ICoreAPI>();
            api.Setup(a => a.World).Returns(world.Object);

            bool snow = PlantSnowCover.ShouldUseSnowVariant(
                api.Object,
                parentPos,
                childPos);

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
        public void CodeWithCover_LegacyBareFernPhase_MapsToFreeAndSnow()
        {
            var bare = new AssetLocation("ecosystemflora:fernphase-eaglefern-dieback");
            var snow = PlantSnowCover.CodeWithCover(bare, snow: true);
            Assert.Equal("ecosystemflora:fernphase-eaglefern-dieback-snow", snow.ToString());

            var back = PlantSnowCover.CodeWithCover(snow, snow: false);
            Assert.Equal("ecosystemflora:fernphase-eaglefern-dieback-free", back.ToString());
        }

        [Fact]
        public void BlockHasCoverVariant_IncludesLegacyBareFernPhasePaths()
        {
            Assert.True(PlantSnowCover.BlockHasCoverVariant("fernphase-eaglefern-dieback"));
            Assert.True(PlantSnowCover.BlockHasCoverVariant("fernphase-eaglefern-dieback-free"));
            Assert.False(PlantSnowCover.BlockHasCoverVariant("fern-eaglefern-normal"));
        }

        [Theory]
        [InlineData("ecosystemflora:fernphase-eaglefern-dormant", "ecosystemflora:fernphase-eaglefern-dormant-free")]
        [InlineData("ecosystemflora:fernphase-cinnamonfern-dieback", "ecosystemflora:fernphase-cinnamonfern-dieback-free")]
        [InlineData("ecosystemflora:fernphase-eaglefern-dormant-free", null)]
        [InlineData("ecosystemflora:fernphase-eaglefern-dieback-snow", null)]
        public void LegacyPhaseBlockMigration_RemapsBareFernPhaseToFree(string from, string expected)
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
        public void TrySyncCover_SwitchesFernDormantBlockToSnowInWinter()
        {
            Block air = new Block { BlockId = 0 };
            Block freePhase = new Block
            {
                BlockId = 1,
                Code = new AssetLocation("ecosystemflora:fernphase-eaglefern-dormant-free"),
            };
            Block snowPhase = new Block
            {
                BlockId = 2,
                Code = new AssetLocation("ecosystemflora:fernphase-eaglefern-dormant-snow"),
            };
            Block snowLayer = new Block
            {
                BlockId = 3,
                Code = new AssetLocation("game:snowlayer-1"),
                BlockMaterial = EnumBlockMaterial.Snow,
            };
            var acc = new EcologyTestBlockAccessor(new[] { air, freePhase, snowPhase, snowLayer })
            {
                Temperature = -8f,
            };
            var pos = new BlockPos(3, 64, 3);
            acc.SetBlock(1, pos);
            PlaceSnowLayerAbove(acc, pos, snowLayer);

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
        public void TrySyncCover_SwitchesFernSporulatingBlockToSnowInWinter()
        {
            Block air = new Block { BlockId = 0 };
            Block freePhase = new Block
            {
                BlockId = 1,
                Code = new AssetLocation("ecosystemflora:fernphase-eaglefern-sporulating-free"),
            };
            Block snowPhase = new Block
            {
                BlockId = 2,
                Code = new AssetLocation("ecosystemflora:fernphase-eaglefern-sporulating-snow"),
            };
            var acc = new EcologyTestBlockAccessor(new[] { air, freePhase, snowPhase })
            {
                Temperature = -8f,
            };
            var pos = new BlockPos(7, 64, 7);
            acc.SetBlock(1, pos);
            SetSnowAccum(acc, pos, 0.25f);

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
            Assert.True(PlantSnowCover.PathHasSnowCover(acc.GetBlock(pos).Code.Path));
        }

        [Fact]
        public void ClimateWantsSnowCover_WarmWeather_IgnoresResidualSnowAccum()
        {
            Block air = new Block { BlockId = 0 };
            Block flower = new Block
            {
                BlockId = 1,
                Code = new AssetLocation("ecosystemflora:flowerphase-cornflower-dormant-free"),
            };
            var acc = new EcologyTestBlockAccessor(blocks: new[] { air, flower })
            {
                Temperature = 18f,
            };
            var pos = new BlockPos(4, 64, 4);
            acc.SetBlock(1, pos);

            int cs = GlobalConstants.ChunkSize;
            EcologyTestMapChunk chunk = acc.GetOrCreateMapChunk(pos.X / cs, pos.Z / cs);
            chunk.SnowAccum = new float[cs * cs];
            int idx = (pos.Z % cs) * cs + (pos.X % cs);
            chunk.SnowAccum[idx] = 0.5f;

            var world = new Mock<IWorldAccessor>();
            world.Setup(w => w.BlockAccessor).Returns(acc);
            var api = new Mock<ICoreAPI>();
            api.Setup(a => a.World).Returns(world.Object);

            Assert.False(PlantSnowCover.ClimateWantsSnowCover(api.Object, pos));
            Assert.False(PlantSnowCover.ResolveWantsSnowCover(api.Object, pos));
        }

        [Fact]
        public void TrySyncCover_SwitchesFernDiebackBlockToSnowInWinter()
        {
            Block air = new Block { BlockId = 0 };
            Block freePhase = new Block
            {
                BlockId = 1,
                Code = new AssetLocation("ecosystemflora:fernphase-eaglefern-dieback-free"),
            };
            Block snowPhase = new Block
            {
                BlockId = 2,
                Code = new AssetLocation("ecosystemflora:fernphase-eaglefern-dieback-snow"),
            };
            var acc = new EcologyTestBlockAccessor(new[] { air, freePhase, snowPhase })
            {
                Temperature = -8f,
            };
            var pos = new BlockPos(5, 64, 5);
            acc.SetBlock(1, pos);
            SetSnowAccum(acc, pos, 0.25f);

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
            Assert.True(PlantSnowCover.PathHasSnowCover(acc.GetBlock(pos).Code.Path));
        }

        [Fact]
        public void ResolveWantsSnowCover_ColdWithoutGroundSnow_ReturnsFalse()
        {
            Block air = new Block { BlockId = 0 };
            Block dormant = new Block
            {
                BlockId = 1,
                Code = new AssetLocation("ecosystemflora:fernphase-eaglefern-dormant-free"),
            };
            var acc = new EcologyTestBlockAccessor(new[] { air, dormant })
            {
                Temperature = -6f,
            };
            var pos = new BlockPos(8, 64, 8);
            acc.SetBlock(1, pos);

            var world = new Mock<IWorldAccessor>();
            world.Setup(w => w.BlockAccessor).Returns(acc);
            var api = new Mock<ICoreAPI>();
            api.Setup(a => a.World).Returns(world.Object);

            Assert.True(PlantSnowCover.ClimateWantsSnowCover(api.Object, pos));
            Assert.False(PlantSnowCover.ResolveWantsSnowCover(api.Object, pos));
        }

        [Fact]
        public void TrySyncCover_ThawsDormantSnowWhenColdButNoGroundSnow()
        {
            Block air = new Block { BlockId = 0 };
            Block dormantSnow = new Block
            {
                BlockId = 1,
                Code = new AssetLocation("ecosystemflora:fernphase-eaglefern-dormant-snow"),
            };
            Block dormantFree = new Block
            {
                BlockId = 2,
                Code = new AssetLocation("ecosystemflora:fernphase-eaglefern-dormant-free"),
            };
            var acc = new EcologyTestBlockAccessor(new[] { air, dormantSnow, dormantFree })
            {
                Temperature = -3f,
            };
            var pos = new BlockPos(9, 64, 9);
            acc.SetBlock(1, pos);

            var world = new Mock<IWorldAccessor>();
            world.Setup(w => w.BlockAccessor).Returns(acc);
            world.Setup(w => w.GetBlock(It.IsAny<AssetLocation>()))
                .Returns((AssetLocation loc) =>
                {
                    if (loc.Path.Contains("-snow")) return dormantSnow;
                    if (loc.Path.Contains("-free")) return dormantFree;
                    return air;
                });
            var api = new Mock<ICoreAPI>();
            api.Setup(a => a.World).Returns(world.Object);

            Assert.True(PlantSnowCoverSync.TrySyncCover(api.Object, pos));
            Assert.False(PlantSnowCover.PathHasSnowCover(acc.GetBlock(pos).Code.Path));
        }

        [Fact]
        public void ResolveWantsSnowCover_WarmWeather_IgnoresSnowLayerAbove()
        {
            Block air = new Block { BlockId = 0 };
            Block snowLayer = new Block
            {
                BlockId = 2,
                Code = new AssetLocation("game:snowlayer-1"),
                BlockMaterial = EnumBlockMaterial.Snow,
            };
            Block dieback = new Block
            {
                BlockId = 1,
                Code = new AssetLocation("ecosystemflora:tallgrassphase-dieback-snow"),
            };
            var acc = new EcologyTestBlockAccessor(new[] { air, dieback, snowLayer })
            {
                Temperature = 16f,
            };
            var pos = new BlockPos(4, 64, 4);
            acc.SetBlock(1, pos);
            acc.SetBlock(2, new BlockPos(4, 65, 4));

            var world = new Mock<IWorldAccessor>();
            world.Setup(w => w.BlockAccessor).Returns(acc);
            var api = new Mock<ICoreAPI>();
            api.Setup(a => a.World).Returns(world.Object);

            Assert.True(PlantSnowCover.EnvironmentWantsSnowCover(api.Object, pos));
            Assert.False(PlantSnowCover.ResolveWantsSnowCover(api.Object, pos));
        }

        [Fact]
        public void TrySyncCover_ThawsDiebackSnowInSummer()
        {
            Block air = new Block { BlockId = 0 };
            Block diebackSnow = new Block
            {
                BlockId = 1,
                Code = new AssetLocation("ecosystemflora:tallgrassphase-dieback-snow"),
            };
            Block diebackFree = new Block
            {
                BlockId = 2,
                Code = new AssetLocation("ecosystemflora:tallgrassphase-dieback-free"),
            };
            var acc = new EcologyTestBlockAccessor(new[] { air, diebackSnow, diebackFree })
            {
                Temperature = 18f,
            };
            var pos = new BlockPos(6, 64, 6);
            acc.SetBlock(1, pos);

            var world = new Mock<IWorldAccessor>();
            world.Setup(w => w.BlockAccessor).Returns(acc);
            world.Setup(w => w.GetBlock(It.IsAny<AssetLocation>()))
                .Returns((AssetLocation loc) =>
                {
                    if (loc.Path.Contains("-snow")) return diebackSnow;
                    if (loc.Path.Contains("-free")) return diebackFree;
                    return air;
                });
            var api = new Mock<ICoreAPI>();
            api.Setup(a => a.World).Returns(world.Object);

            Assert.True(PlantSnowCoverSync.TrySyncCover(api.Object, pos));
            Assert.False(PlantSnowCover.PathHasSnowCover(acc.GetBlock(pos).Code.Path));
        }

        [Fact]
        public void ShouldUseSnowVariant_DoesNotInheritParentSnowInWarmWeather()
        {
            Block air = new Block { BlockId = 0 };
            Block snowFlower = new Block
            {
                BlockId = 1,
                Code = new AssetLocation("game:flower-catmint-snow"),
            };
            var acc = new EcologyTestBlockAccessor(blocks: new[] { air, snowFlower })
            {
                Temperature = 20f,
            };
            acc.SetBlock(1, new BlockPos(5, 64, 5));

            var world = new Mock<IWorldAccessor>();
            world.Setup(w => w.BlockAccessor).Returns(acc);
            var api = new Mock<ICoreAPI>();
            api.Setup(a => a.World).Returns(world.Object);

            Assert.False(PlantSnowCover.ShouldUseSnowVariant(
                api.Object,
                new BlockPos(5, 64, 5),
                new BlockPos(6, 64, 6)));
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
            Assert.False(PlantSnowCover.ResolveWantsSnowCover(api.Object, new BlockPos(4, 64, 4)));
        }

        [Fact]
        public void ResolveWantsSnowCover_ColdWithSnowLayer_ReturnsTrue()
        {
            Block air = new Block { BlockId = 0 };
            Block snowLayer = new Block
            {
                BlockId = 2,
                Code = new AssetLocation("game:snowlayer-1"),
                BlockMaterial = EnumBlockMaterial.Snow,
            };
            Block dormant = new Block
            {
                BlockId = 1,
                Code = new AssetLocation("ecosystemflora:flowerphase-cornflower-dormant-free"),
            };
            var acc = new EcologyTestBlockAccessor(new[] { air, dormant, snowLayer })
            {
                Temperature = -5f,
            };
            var pos = new BlockPos(4, 64, 4);
            acc.SetBlock(1, pos);
            PlaceSnowLayerAbove(acc, pos, snowLayer);

            var world = new Mock<IWorldAccessor>();
            world.Setup(w => w.BlockAccessor).Returns(acc);
            var api = new Mock<ICoreAPI>();
            api.Setup(a => a.World).Returns(world.Object);

            Assert.True(PlantSnowCover.ResolveWantsSnowCover(api.Object, pos));
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
            Block snowLayer = new Block
            {
                BlockId = 3,
                Code = new AssetLocation("game:snowlayer-1"),
                BlockMaterial = EnumBlockMaterial.Snow,
            };
            var acc = new EcologyTestBlockAccessor(new[] { air, freePhase, snowPhase, snowLayer })
            {
                Temperature = -8f,
            };
            var pos = new BlockPos(3, 64, 3);
            acc.SetBlock(1, pos);
            PlaceSnowLayerAbove(acc, pos, snowLayer);

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
        public void ResolveWantsSnowCover_UnderWaterColumn_ReturnsFalse()
        {
            Block air = new Block { BlockId = 0 };
            Block gravel = new Block
            {
                BlockId = 1,
                Code = new AssetLocation("game:gravel-granite"),
            };
            gravel.SideSolid[BlockFacing.UP.Index] = true;
            Block dormant = new Block
            {
                BlockId = 2,
                Code = new AssetLocation("ecosystemflora:flowerphase-cornflower-dormant-free"),
            };
            Block water = new Block
            {
                BlockId = 3,
                Code = new AssetLocation("game:water-still-7"),
                LiquidLevel = 7,
            };
            var acc = new EcologyTestBlockAccessor(new[] { air, gravel, dormant, water })
            {
                Temperature = -8f,
            };
            var bed = new BlockPos(8, 63, 8);
            var plantPos = new BlockPos(8, 64, 8);
            var waterPos = new BlockPos(8, 65, 8);
            acc.SetBlock(1, bed);
            acc.SetBlock(2, plantPos);
            acc.SetBlock(3, waterPos);
            SetSnowAccum(acc, plantPos, 0.4f);

            var world = new Mock<IWorldAccessor>();
            world.Setup(w => w.BlockAccessor).Returns(acc);
            var api = new Mock<ICoreAPI>();
            api.Setup(a => a.World).Returns(world.Object);

            Assert.True(BlockFluidHelper.ExcludesSnowCover(acc, plantPos));
            Assert.False(PlantSnowCover.ResolveWantsSnowCover(api.Object, plantPos));
        }

        [Fact]
        public void TrySyncCover_StripsSnowFromPlantUnderWater()
        {
            Block air = new Block { BlockId = 0 };
            Block gravel = new Block
            {
                BlockId = 1,
                Code = new AssetLocation("game:gravel-granite"),
            };
            gravel.SideSolid[BlockFacing.UP.Index] = true;
            Block snowPhase = new Block
            {
                BlockId = 2,
                Code = new AssetLocation("ecosystemflora:flowerphase-cornflower-dormant-snow"),
            };
            Block freePhase = new Block
            {
                BlockId = 3,
                Code = new AssetLocation("ecosystemflora:flowerphase-cornflower-dormant-free"),
            };
            Block water = new Block
            {
                BlockId = 4,
                Code = new AssetLocation("game:water-still-7"),
                LiquidLevel = 7,
            };
            var acc = new EcologyTestBlockAccessor(new[] { air, gravel, snowPhase, freePhase, water })
            {
                Temperature = -8f,
            };
            var bed = new BlockPos(9, 63, 9);
            var plantPos = new BlockPos(9, 64, 9);
            acc.SetBlock(1, bed);
            acc.SetBlock(2, plantPos);
            acc.SetBlock(4, plantPos.UpCopy());
            SetSnowAccum(acc, plantPos, 0.5f);

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

            Assert.True(PlantSnowCoverSync.TrySyncCover(api.Object, plantPos));
            Assert.False(PlantSnowCover.PathHasSnowCover(acc.GetBlock(plantPos).Code.Path));
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
