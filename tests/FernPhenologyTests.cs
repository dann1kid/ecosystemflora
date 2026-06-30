using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Testing;
using Xunit;

namespace WildFarming.Tests
{
    public class FernPhenologyTests
    {
        static EcosystemConfig EnabledCfg => new EcosystemConfig { EnableFernPhenology = true };

        [Theory]
        [InlineData("eaglefern", true)]
        [InlineData("catmint", false)]
        public void UsesPhenology_OnlyFerns(string species, bool expected)
        {
            var req = new PlantRequirements { Species = species, Habitat = EcologyHabitat.Terrestrial };
            Assert.Equal(expected, FernPhenology.UsesPhenology(EnabledCfg, req));
        }

        [Theory]
        [InlineData(0.05f, FernPhenologyPhase.Dormant)]
        [InlineData(0.6f, FernPhenologyPhase.Sporulating)]
        public void InferPhaseForTests_RespectsSeason(float season, FernPhenologyPhase expected)
        {
            Assert.Equal(expected, FernPhenology.InferPhaseForTests(season));
        }

        [Theory]
        [InlineData(FernPhenologyPhase.Dormant, false)]
        [InlineData(FernPhenologyPhase.Sporulating, true)]
        [InlineData(FernPhenologyPhase.Dieback, false)]
        public void AllowsSpread_OnlySporulating(FernPhenologyPhase phase, bool expected)
        {
            Assert.Equal(expected, FernPhenology.AllowsSpread(phase));
        }

        [Fact]
        public void BlockMatchesPhase_DiebackFree_FalseWhenWinterWantsSnow()
        {
            Block diebackFree = new Block
            {
                BlockId = 1,
                Code = new AssetLocation("ecosystemflora:fernphase-eaglefern-dieback-free"),
            };
            var acc = new EcologyTestBlockAccessor(new[] { new Block { BlockId = 0 }, diebackFree })
            {
                Temperature = -6f,
            };
            var pos = new BlockPos(2, 64, 2);
            acc.SetBlock(1, pos);

            var world = new Mock<IWorldAccessor>();
            world.Setup(w => w.BlockAccessor).Returns(acc);
            var api = new Mock<ICoreAPI>();
            api.Setup(a => a.World).Returns(world.Object);

            Assert.False(FernPhenology.BlockMatchesPhase(
                api.Object, pos, "eaglefern", diebackFree, FernPhenologyPhase.Dieback));
        }

        [Fact]
        public void SyncBlockToPhase_DiebackThenCoverSync_AppliesSnowInWinter()
        {
            Block air = new Block { BlockId = 0 };
            Block dormantFree = new Block
            {
                BlockId = 1,
                Code = new AssetLocation("ecosystemflora:fernphase-eaglefern-dormant-free"),
            };
            Block diebackFree = new Block
            {
                BlockId = 2,
                Code = new AssetLocation("ecosystemflora:fernphase-eaglefern-dieback-free"),
            };
            Block diebackSnow = new Block
            {
                BlockId = 3,
                Code = new AssetLocation("ecosystemflora:fernphase-eaglefern-dieback-snow"),
            };
            var acc = new EcologyTestBlockAccessor(new[] { air, dormantFree, diebackFree, diebackSnow })
            {
                Temperature = -7f,
            };
            var pos = new BlockPos(9, 64, 9);
            acc.SetBlock(1, pos);

            var world = new Mock<IWorldAccessor>();
            world.Setup(w => w.BlockAccessor).Returns(acc);
            world.Setup(w => w.GetBlock(It.IsAny<AssetLocation>()))
                .Returns((AssetLocation loc) =>
                {
                    if (loc.Path.Contains("dieback-snow")) return diebackSnow;
                    if (loc.Path.Contains("dieback-free")) return diebackFree;
                    if (loc.Path.Contains("dormant-free")) return dormantFree;
                    return air;
                });
            var api = new Mock<ICoreAPI>();
            api.Setup(a => a.World).Returns(world.Object);

            Assert.True(FernPhenology.SyncBlockToPhase(api.Object, pos, "eaglefern", FernPhenologyPhase.Dieback));
            Assert.True(PlantSnowCoverSync.TrySyncCover(api.Object, pos));
            Assert.True(PlantSnowCover.PathHasSnowCover(acc.GetBlock(pos).Code.Path));
        }
    }
}
