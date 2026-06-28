using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class TallgrassEstablishmentTests
    {
        [Fact]
        public void PickTargetStageIndex_IsStableForSamePos()
        {
            var pos = new BlockPos(12, 64, 34);
            var req = new PlantRequirements { Species = "tallgrass" };

            int a = TallgrassSpreadHeight.PickTargetStageIndex(null, pos, req);
            int b = TallgrassSpreadHeight.PickTargetStageIndex(null, pos, req);

            Assert.Equal(a, b);
            Assert.InRange(a, 0, TallgrassSpreadHeight.HeightStages.Length - 1);
        }

        [Fact]
        public void IsReadyToRegister_WhenAtOrAboveHalfTarget()
        {
            EcosystemConfig.Loaded = new EcosystemConfig { EnableTallgrassSpreadMaturation = true };
            var pos = new BlockPos(12, 64, 34);
            int target = TallgrassSpreadHeight.PickTargetStageIndex(null, pos, new PlantRequirements { Species = "tallgrass" });
            int minSpread = TallgrassSpreadHeight.MinSpreadStageIndex(target);

            Assert.True(TallgrassEstablishment.IsReadyToRegister(
                Block("tallgrass-" + TallgrassSpreadHeight.HeightStages[minSpread] + "-free"), target, null, pos));
            Assert.True(TallgrassEstablishment.IsReadyToRegister(
                Block("tallgrass-verytall-free"), target, null, pos));

            if (minSpread > 0)
            {
                Assert.False(TallgrassEstablishment.IsReadyToRegister(
                    Block("tallgrass-" + TallgrassSpreadHeight.HeightStages[minSpread - 1] + "-free"), target, null, pos));
            }

            EcosystemConfig.Loaded = new EcosystemConfig();
        }

        [Fact]
        public void ShouldQueueAfterPlacement_NonTallgrass_SkipsWithoutThrowing()
        {
            EcosystemConfig.Loaded = new EcosystemConfig { EnableTallgrassSpreadMaturation = true };

            var block = new Vintagestory.API.Common.Block
            {
                Code = new Vintagestory.API.Common.AssetLocation("game", "rock-granite"),
            };

            Assert.False(TallgrassEstablishment.ShouldQueueAfterPlacement(
                null, new BlockPos(0, 64, 0), block));
        }

        [Fact]
        public void ShouldQueueEstablishment_NullRequirements_ReturnsFalse()
        {
            EcosystemConfig.Loaded = new EcosystemConfig { EnableTallgrassSpreadMaturation = true };

            Assert.False(TallgrassEstablishment.ShouldQueueEstablishment(
                null,
                new BlockPos(0, 64, 0),
                Block("tallgrass-veryshort-free"),
                requirements: null));
        }

        static Vintagestory.API.Common.Block Block(string path) =>
            new Vintagestory.API.Common.Block { Code = new Vintagestory.API.Common.AssetLocation("game", path) };
    }
}
