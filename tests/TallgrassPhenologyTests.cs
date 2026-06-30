using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Testing;
using Xunit;

namespace WildFarming.Tests
{
    public class TallgrassPhenologyTests
    {
        static EcosystemConfig EnabledCfg => new EcosystemConfig { EnableTallgrassPhenology = true };

        [Fact]
        public void UsesPhenology_TallgrassAndShoreSedge()
        {
            var tallgrass = new PlantRequirements { Species = "tallgrass", Habitat = EcologyHabitat.Terrestrial };
            Assert.True(TallgrassPhenology.UsesPhenology(EnabledCfg, tallgrass));

            var sedge = new PlantRequirements { Species = EcologyShoreSedgeSpecies.Brownsedge, Habitat = EcologyHabitat.Terrestrial };
            Assert.True(TallgrassPhenology.UsesPhenology(EnabledCfg, sedge));

            var flower = new PlantRequirements { Species = "catmint", Habitat = EcologyHabitat.Terrestrial };
            Assert.False(TallgrassPhenology.UsesPhenology(EnabledCfg, flower));
        }

        [Theory]
        [InlineData(0.05f, TallgrassPhenologyPhase.Dormant)]
        [InlineData(0.5f, TallgrassPhenologyPhase.Active)]
        public void InferPhaseForTests_RespectsSeason(float season, TallgrassPhenologyPhase expected)
        {
            Assert.Equal(expected, TallgrassPhenology.InferPhaseForTests(season));
        }

        [Theory]
        [InlineData(TallgrassPhenologyPhase.Dormant, false)]
        [InlineData(TallgrassPhenologyPhase.Active, true)]
        [InlineData(TallgrassPhenologyPhase.Dieback, false)]
        public void AllowsSpread_OnlyActive(TallgrassPhenologyPhase phase, bool expected)
        {
            Assert.Equal(expected, TallgrassPhenology.AllowsSpread(phase));
        }

        [Fact]
        public void SedgePhaseBlocks_HaveSnowVariants()
        {
            Assert.Equal(
                "ecosystemflora:sedgephase-dormant-snow",
                SedgePhenologyBlocks.CodeForPhase(TallgrassPhenologyPhase.Dormant, snow: true).ToString());
            Assert.Equal(
                "ecosystemflora:sedgephase-dieback-snow",
                SedgePhenologyBlocks.CodeForPhase(TallgrassPhenologyPhase.Dieback, snow: true).ToString());
        }

        [Fact]
        public void SyncBlockToPhase_SedgeDormant_ReplacesGreenMatureBlock()
        {
            Block air = new Block { BlockId = 0 };
            Block mature = new Block
            {
                BlockId = 1,
                Code = new AssetLocation("game:tallplant-brownsedge-land-normal-free"),
            };
            Block dormant = new Block
            {
                BlockId = 2,
                Code = new AssetLocation("ecosystemflora:sedgephase-dormant-snow"),
            };
            var acc = new EcologyTestBlockAccessor(new[] { air, mature, dormant })
            {
                Temperature = -6f,
            };
            var pos = new BlockPos(8, 64, 8);
            acc.SetBlock(1, pos);

            var world = new Mock<IWorldAccessor>();
            world.Setup(w => w.BlockAccessor).Returns(acc);
            world.Setup(w => w.GetBlock(It.IsAny<AssetLocation>()))
                .Returns((AssetLocation loc) =>
                {
                    if (loc.Path.Contains("sedgephase-dormant-snow")) return dormant;
                    if (loc.Path.Contains("tallplant-brownsedge")) return mature;
                    return air;
                });
            var api = new Mock<ICoreAPI>();
            api.Setup(a => a.World).Returns(world.Object);

            var req = new PlantRequirements { Species = EcologyShoreSedgeSpecies.Brownsedge, Habitat = EcologyHabitat.Terrestrial };
            Assert.True(TallgrassPhenology.SyncBlockToPhase(api.Object, pos, req, TallgrassPhenologyPhase.Dormant));
            Assert.Equal(2, acc.GetBlock(pos).BlockId);
        }

        [Fact]
        public void BlockMatchesPhase_SedgeDormant_FalseOnGreenMature()
        {
            Block mature = new Block
            {
                BlockId = 1,
                Code = new AssetLocation("game:tallplant-brownsedge-land-normal-free"),
            };
            var req = new PlantRequirements { Species = EcologyShoreSedgeSpecies.Brownsedge, Habitat = EcologyHabitat.Terrestrial };
            Assert.False(TallgrassPhenology.BlockMatchesPhase(null, null, req, mature, TallgrassPhenologyPhase.Dormant));
        }
    }
}
