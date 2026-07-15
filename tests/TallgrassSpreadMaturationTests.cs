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
        [InlineData(0, 1)]
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
        public void NeedsEstablishment_ContinuesPastHalfTargetTowardFull()
        {
            EcosystemConfig.Loaded = new EcosystemConfig { EnableTallgrassSpreadMaturation = true };
            // Seed chosen so open default context yields a tall target (> medium).
            var pos = new BlockPos(3, 64, 7);
            var req = new PlantRequirements { Species = "tallgrass" };
            int target = TallgrassSpreadHeight.PickTargetStageIndex(null, pos, req);
            int minSpread = TallgrassSpreadHeight.MinSpreadStageIndex(target);

            Assert.True(target > minSpread, "test seed should pick target above half-spread stage");

            string halfStage = TallgrassSpreadHeight.HeightStages[minSpread];
            Assert.True(TallgrassEstablishment.IsReadyToRegister(
                Block("tallgrass-" + halfStage + "-free"), target, null, pos));
            Assert.True(TallgrassEstablishment.NeedsEstablishment(
                null, pos, Block("tallgrass-" + halfStage + "-free"), out int stillTarget));
            Assert.Equal(target, stillTarget);

            string fullStage = TallgrassSpreadHeight.HeightStages[target];
            Assert.False(TallgrassEstablishment.NeedsEstablishment(
                null, pos, Block("tallgrass-" + fullStage + "-free"), out _));

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
        public void StuckTimeout_RequiresPriorDueFailure_NotMereLateness()
        {
            const double timeout = 60 * 24 * 14;
            Assert.False(PendingTallgrassPromotion.IsStuckPastTimeout(0, 1_000_000, timeout));
            Assert.False(PendingTallgrassPromotion.IsStuckPastTimeout(100, 100 + timeout, timeout));
            Assert.True(PendingTallgrassPromotion.IsStuckPastTimeout(100, 100 + timeout + 1, timeout));
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
