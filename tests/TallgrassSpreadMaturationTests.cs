using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class TallgrassSpreadMaturationTests
    {
        static Vintagestory.API.Common.Block Block(string path) =>
            new Vintagestory.API.Common.Block { Code = new Vintagestory.API.Common.AssetLocation("game", path) };

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(2, 1)]
        [InlineData(3, 2)]
        [InlineData(4, 2)]
        [InlineData(5, 3)]
        public void MinSpreadStageIndex_IsHalfOfTargetRoundedUp(int target, int expectedMin)
        {
            Assert.Equal(expectedMin, TallgrassSpreadHeight.MinSpreadStageIndex(target));
        }

        [Fact]
        public void CanReproduceFrom_BlocksBelowHalfTarget()
        {
            EcosystemConfig.Loaded = new EcosystemConfig { EnableTallgrassSpreadMaturation = true };
            var pos = new BlockPos(100, 64, 200);
            var req = new PlantRequirements { Species = "tallgrass", Habitat = EcologyHabitat.Terrestrial };
            int target = TallgrassSpreadHeight.PickTargetStageIndex(null, pos, req);
            int minSpread = TallgrassSpreadHeight.MinSpreadStageIndex(target);

            Assert.False(TallgrassSpreadMaturation.CanReproduceFrom(Block("tallgrass-veryshort-free"), null, pos));

            if (minSpread >= 1)
            {
                Assert.False(TallgrassSpreadMaturation.CanReproduceFrom(Block("tallgrass-short-free"), null, pos));
            }

            string minStage = TallgrassSpreadHeight.HeightStages[minSpread];
            Assert.True(TallgrassSpreadMaturation.CanReproduceFrom(Block("tallgrass-" + minStage + "-free"), null, pos));

            EcosystemConfig.Loaded = new EcosystemConfig();
        }

        [Fact]
        public void CanReproduceFrom_RejectsEaten()
        {
            Assert.False(TallgrassSpreadMaturation.CanReproduceFrom(Block("tallgrass-eaten-free")));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void UsesMaturation_FollowsConfig(bool enabled, bool expected)
        {
            var cfg = new EcosystemConfig { EnableTallgrassSpreadMaturation = enabled };
            Assert.Equal(expected, TallgrassSpreadMaturation.UsesMaturation(cfg));
        }

        [Fact]
        public void ShouldQueuePromotion_EstablishingAndBelowTarget()
        {
            var cfgOn = new EcosystemConfig { EnableTallgrassSpreadMaturation = true };
            EcosystemConfig.Loaded = cfgOn;

            var req = new PlantRequirements { Species = "tallgrass", Habitat = EcologyHabitat.Terrestrial };
            Assert.True(TallgrassSpreadMaturation.ShouldQueuePromotion(Block("tallgrass-veryshort-free"), req));
            Assert.True(TallgrassSpreadMaturation.ShouldQueuePromotion(Block("tallgrass-short-free"), req));
            Assert.False(TallgrassSpreadMaturation.ShouldQueuePromotion(Block("tallgrass-verytall-free"), req));

            EcosystemConfig.Loaded = new EcosystemConfig();
        }

        [Fact]
        public void IsReadyToRegister_OpensAtHalfTarget_NotFullTarget()
        {
            EcosystemConfig.Loaded = new EcosystemConfig { EnableTallgrassSpreadMaturation = true };
            var pos = new BlockPos(12, 64, 34);
            int target = TallgrassSpreadHeight.PickTargetStageIndex(null, pos, new PlantRequirements { Species = "tallgrass" });
            int minSpread = TallgrassSpreadHeight.MinSpreadStageIndex(target);

            string minStage = TallgrassSpreadHeight.HeightStages[minSpread];
            Assert.True(TallgrassEstablishment.IsReadyToRegister(
                Block("tallgrass-" + minStage + "-free"), target, null, pos));

            if (minSpread > 0)
            {
                string below = TallgrassSpreadHeight.HeightStages[minSpread - 1];
                Assert.False(TallgrassEstablishment.IsReadyToRegister(
                    Block("tallgrass-" + below + "-free"), target, null, pos));
            }

            if (target > minSpread)
            {
                Assert.True(TallgrassEstablishment.NeedsEstablishment(null, pos, Block("tallgrass-" + minStage + "-free"), out _));
            }

            EcosystemConfig.Loaded = new EcosystemConfig();
        }

        [Fact]
        public void GetHeightStageIndex_MatchesStageOrder()
        {
            Assert.Equal(0, TallgrassSpreadHeight.GetHeightStageIndex("veryshort"));
            Assert.Equal(1, TallgrassSpreadHeight.GetHeightStageIndex("short"));
            Assert.Equal(-1, TallgrassSpreadHeight.GetHeightStageIndex("eaten"));
        }

        [Fact]
        public void StageAdvanceHours_ScalesWithGrowthHoursMultiplier()
        {
            var slow = new EcosystemConfig
            {
                EnableTallgrassSpreadMaturation = true,
                GrowthHoursMultiplier = 0.5f,
            };
            var fast = new EcosystemConfig
            {
                EnableTallgrassSpreadMaturation = true,
                GrowthHoursMultiplier = 2f,
            };

            double slowHours = WildTallgrassMaturation.StageAdvanceHours(null, null, slow);
            double fastHours = WildTallgrassMaturation.StageAdvanceHours(null, null, fast);

            Assert.True(fastHours < slowHours);
            Assert.True(slowHours >= 6);
            Assert.True(fastHours >= 6);
        }
    }
}
