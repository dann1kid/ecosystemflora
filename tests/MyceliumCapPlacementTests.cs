using Vintagestory.API.Common;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class MyceliumCapPlacementTests
    {
        [Fact]
        public void PassesVanillaPlacementGate_Air_IsTrue()
        {
            var air = new Block { BlockId = 0 };

            Assert.True(MyceliumCapPlacement.PassesVanillaPlacementGate(air, mushroomsGrownTotalDays: 100));
        }

        [Fact]
        public void PassesVanillaPlacementGate_OccupiedAfterFirstCycle_IsFalse()
        {
            var grass = new Block
            {
                BlockId = 42,
                Code = new AssetLocation("game", "tallgrass-veryshort-free"),
                Replaceable = 9000,
            };

            Assert.False(MyceliumCapPlacement.PassesVanillaPlacementGate(grass, mushroomsGrownTotalDays: 100));
        }

        [Fact]
        public void PassesVanillaPlacementGate_HighReplaceableOnFirstCycle_IsTrue()
        {
            var grass = new Block
            {
                BlockId = 42,
                Replaceable = 6000,
            };

            Assert.True(MyceliumCapPlacement.PassesVanillaPlacementGate(grass, mushroomsGrownTotalDays: 0));
        }

        [Fact]
        public void PassesModDisplacementGate_MeadowFlower_WhenEnabled_IsTrue()
        {
            EcosystemConfig.Loaded = new EcosystemConfig
            {
                EnableMyceliumEcology = true,
                EnableMyceliumCapDisplacement = true,
            };

            var flower = new Block { Code = new AssetLocation("game", "flower-wilddaisy-free"), BlockId = 7 };

            Assert.True(MyceliumCapPlacement.PassesModDisplacementGate(flower));
        }

        [Fact]
        public void PassesModDisplacementGate_Tallgrass_WhenEnabled_IsTrue()
        {
            EcosystemConfig.Loaded = new EcosystemConfig
            {
                EnableMyceliumEcology = true,
                EnableMyceliumCapDisplacement = true,
            };

            var grass = new Block { Code = new AssetLocation("game", "tallgrass-veryshort-free"), BlockId = 7 };

            Assert.True(MyceliumCapPlacement.PassesModDisplacementGate(grass));
        }

        [Fact]
        public void PassesModDisplacementGate_WhenDisabled_IsFalse()
        {
            EcosystemConfig.Loaded = new EcosystemConfig
            {
                EnableMyceliumEcology = true,
                EnableMyceliumCapDisplacement = false,
            };

            var grass = new Block { Code = new AssetLocation("game", "tallgrass-veryshort-free"), BlockId = 7 };

            Assert.False(MyceliumCapPlacement.PassesModDisplacementGate(grass));
        }

        [Fact]
        public void PassesModDisplacementGate_TreeLog_IsFalse()
        {
            EcosystemConfig.Loaded = new EcosystemConfig
            {
                EnableMyceliumEcology = true,
                EnableMyceliumCapDisplacement = true,
            };

            var log = new Block { Code = new AssetLocation("game", "log-grown-oak-ud"), BlockId = 7 };

            Assert.False(MyceliumCapPlacement.PassesModDisplacementGate(log));
        }

        [Fact]
        public void PassesExtendedPlacementGate_GrassAfterFirstCycle_WhenPatchEnabled_IsTrue()
        {
            EcosystemConfig.Loaded = new EcosystemConfig
            {
                EnableMyceliumEcology = true,
                EnableMyceliumCapDisplacement = true,
            };

            var grass = new Block
            {
                BlockId = 42,
                Code = new AssetLocation("game", "tallgrass-tall-free"),
                Replaceable = 9000,
            };

            Assert.True(MyceliumCapPlacement.PassesExtendedPlacementGate(grass, mushroomsGrownTotalDays: 50));
        }
    }
}
