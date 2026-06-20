using Vintagestory.API.Common;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class TallgrassSpreadMaturationTests
    {
        static Block Block(string path) =>
            new Block { Code = new AssetLocation("game", path) };

        [Theory]
        [InlineData("tallgrass-veryshort-free", false)]
        [InlineData("tallgrass-fern-veryshort-free", false)]
        [InlineData("tallgrass-short-free", true)]
        [InlineData("tallgrass-medium-free", true)]
        [InlineData("tallgrass-verytall-free", true)]
        [InlineData("frostedtallgrass-tall-free", true)]
        [InlineData("frostedtallgrass-fern-free", false)]
        public void CanReproduceFrom_RequiresShortOrTaller(string path, bool expected)
        {
            Assert.Equal(expected, TallgrassSpreadMaturation.CanReproduceFrom(Block(path)));
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
        public void ShouldQueuePromotion_OnlyForEstablishingTallgrass()
        {
            var cfgOn = new EcosystemConfig { EnableTallgrassSpreadMaturation = true };
            EcosystemConfig.Loaded = cfgOn;

            var req = new PlantRequirements { Species = "tallgrass", Habitat = EcologyHabitat.Terrestrial };
            Assert.True(TallgrassSpreadMaturation.ShouldQueuePromotion(Block("tallgrass-veryshort-free"), req));
            Assert.False(TallgrassSpreadMaturation.ShouldQueuePromotion(Block("tallgrass-short-free"), req));

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
