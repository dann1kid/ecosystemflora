using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class EcologySpreadBlockResolverTests
    {
        [Fact]
        public void Resolve_FernBareCode_FallsBackToNormalFreeVariant()
        {
            Block air = new Block { BlockId = 0 };
            Block normalFree = new Block
            {
                BlockId = 10,
                Code = new AssetLocation("game:fern-eaglefern-normal-free"),
            };

            var world = new Mock<IWorldAccessor>();
            world.Setup(w => w.GetBlock(It.IsAny<AssetLocation>()))
                .Returns((AssetLocation loc) =>
                {
                    if (loc.Path == "fern-eaglefern-normal-free") return normalFree;
                    if (loc.Path == "fern-eaglefern") return air;
                    return air;
                });
            var api = new Mock<ICoreAPI>();
            api.Setup(a => a.World).Returns(world.Object);

            Block resolved = EcologySpreadBlockResolver.Resolve(
                api.Object,
                new AssetLocation("game:fern-eaglefern"),
                new BlockPos(0, 64, 0),
                normalFree);

            Assert.NotNull(resolved);
            Assert.Equal(normalFree.Code, resolved.Code);
        }

        [Fact]
        public void Resolve_FlowerBareCode_FallsBackToFreeVariant()
        {
            Block air = new Block { BlockId = 0 };
            Block catmintFree = new Block
            {
                BlockId = 11,
                Code = new AssetLocation("game:flower-catmint-free"),
            };

            var world = new Mock<IWorldAccessor>();
            world.Setup(w => w.GetBlock(It.IsAny<AssetLocation>()))
                .Returns((AssetLocation loc) =>
                {
                    if (loc.Path == "flower-catmint-free") return catmintFree;
                    if (loc.Path == "flower-catmint") return air;
                    return air;
                });
            var api = new Mock<ICoreAPI>();
            api.Setup(a => a.World).Returns(world.Object);

            Block resolved = EcologySpreadBlockResolver.Resolve(
                api.Object,
                new AssetLocation("game:flower-catmint"),
                new BlockPos(0, 64, 0),
                catmintFree);

            Assert.NotNull(resolved);
            Assert.Equal(catmintFree.Code, resolved.Code);
        }

        [Fact]
        public void Resolve_BerryBareCode_FallsBackToWildBushFreeVariant()
        {
            Block air = new Block { BlockId = 0 };
            Block bushFree = new Block
            {
                BlockId = 12,
                Code = new AssetLocation("game:fruitingbush-wild-blackcurrant-free"),
            };

            var world = new Mock<IWorldAccessor>();
            world.Setup(w => w.GetBlock(It.IsAny<AssetLocation>()))
                .Returns((AssetLocation loc) =>
                {
                    if (loc.Path == "fruitingbush-wild-blackcurrant-free") return bushFree;
                    if (loc.Path == "fruitingbush-wild-blackcurrant") return air;
                    return air;
                });
            var api = new Mock<ICoreAPI>();
            api.Setup(a => a.World).Returns(world.Object);

            Block resolved = EcologySpreadBlockResolver.Resolve(
                api.Object,
                new AssetLocation("game:fruitingbush-wild-blackcurrant"),
                new BlockPos(0, 64, 0),
                bushFree);

            Assert.NotNull(resolved);
            Assert.Equal(bushFree.Code, resolved.Code);
        }
    }

    public class PlantCodeHelperSpreadBlockCodeTests
    {
        [Fact]
        public void SpreadBlockCode_FlowerPhase_ReturnsLiveBlockCode()
        {
            var block = new Block
            {
                Code = new AssetLocation("ecosystemflora:flowerphase-catmint-dormant-free"),
            };

            AssetLocation spread = PlantCodeHelper.SpreadBlockCode(block);

            Assert.Equal(block.Code, spread);
        }

        [Fact]
        public void SpreadBlockCode_FernPhase_ReturnsLiveBlockCode()
        {
            var block = new Block
            {
                Code = new AssetLocation("ecosystemflora:fernphase-eaglefern-sporulating-free"),
            };

            AssetLocation spread = PlantCodeHelper.SpreadBlockCode(block);

            Assert.Equal(block.Code, spread);
        }
    }
}
