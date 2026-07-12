using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class RegistrationParticipantResolverTests
    {
        [Fact]
        public void TryFromLiveBlock_FernPhase_WhenTryFromBlockWouldUseStrictParent()
        {
            Block phase = new Block
            {
                BlockId = 2,
                Code = new AssetLocation("ecosystemflora:fernphase-eaglefern-dormant-free"),
                Replaceable = 3000,
                BlockMaterial = EnumBlockMaterial.Plant,
            };

            var world = new Mock<IWorldAccessor>();
            world.Setup(w => w.GetBlock(It.IsAny<AssetLocation>())).Returns(phase);
            var api = new Mock<ICoreAPI>();
            api.Setup(a => a.World).Returns(world.Object);

            PlantRequirements requirements = null;
            AssetLocation spread = null;
            AssetLocation mature = null;

            bool ok = RegistrationParticipantResolver.TryFromLiveBlock(
                api.Object,
                new BlockPos(4, 64, 4),
                phase,
                ref requirements,
                ref spread,
                ref mature);

            Assert.True(ok);
            Assert.Equal("eaglefern", requirements?.Species);
            Assert.Equal(phase.Code, spread);
            Assert.Equal(phase.Code, mature);
        }
    }

    public class ReproducerEntrySpeciesMatchTests
    {
        [Fact]
        public void IsRegisteredPlantBlock_MatchesFernPhaseAfterVanillaMatureCode()
        {
            var entry = new ReproducerEntry(
                new BlockPos(0, 64, 0),
                new AssetLocation("game:fern-eaglefern-normal-free"),
                new AssetLocation("game:fern-eaglefern-normal-free"),
                new PlantRequirements { Species = "eaglefern", Habitat = EcologyHabitat.Terrestrial },
                0);

            var phase = new Block
            {
                Code = new AssetLocation("ecosystemflora:fernphase-eaglefern-sporulating-free"),
            };

            Assert.True(entry.IsRegisteredPlantBlock(phase));
        }
    }
}
