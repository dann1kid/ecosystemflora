using Vintagestory.API.Common;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class EcologyAttributesTests
    {
        [Fact]
        public void ReproduceEnabled_AcceptsEaglefernNormalVariant()
        {
            var block = new Block
            {
                BlockId = 1,
                Code = new AssetLocation("game:fern-eaglefern-normal-free"),
                Replaceable = 3000,
                BlockMaterial = EnumBlockMaterial.Plant,
            };

            Assert.True(EcologyAttributes.ReproduceEnabled(block));
            Assert.True(EcosystemParticipant.TryFromBlock(block, out _));
        }

        [Fact]
        public void ReproduceEnabled_AcceptsFernPhaseWhenStrictParticipantFails()
        {
            var block = new Block
            {
                BlockId = 2,
                Code = new AssetLocation("ecosystemflora:fernphase-eaglefern-dormant-free"),
                Replaceable = 3000,
                BlockMaterial = EnumBlockMaterial.Plant,
            };

            Assert.True(EcologyAttributes.ReproduceEnabled(block));
            Assert.True(EcosystemParticipant.TryFromBlock(block, out _));
        }
    }
}
