using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class WildFlowerMaturationTests
    {
        [Theory]
        [InlineData("cowparsley", true)]
        [InlineData("woad", true)]
        [InlineData("catmint", false)]
        public void UsesMaturation_OnlyRolloutSpecies(string species, bool expected)
        {
            var cfg = new EcosystemConfig { EnableFlowerSpreadMaturation = true };
            Assert.Equal(expected, WildFlowerMaturation.UsesMaturation(cfg, species));
        }

        [Fact]
        public void UsesMaturation_OffWhenConfigDisabled()
        {
            var cfg = new EcosystemConfig { EnableFlowerSpreadMaturation = false };
            Assert.False(WildFlowerMaturation.UsesMaturation(cfg, "cowparsley"));
        }

        [Fact]
        public void MaturationHours_ScalesWithGrowthHoursMultiplier()
        {
            var cfgSlow = new EcosystemConfig
            {
                EnableFlowerSpreadMaturation = true,
                GrowthHoursMultiplier = 0.5f,
                UseSeasonalEcology = false,
            };
            var cfgFast = new EcosystemConfig
            {
                EnableFlowerSpreadMaturation = true,
                GrowthHoursMultiplier = 2f,
                UseSeasonalEcology = false,
            };

            double slow = WildFlowerMaturation.MaturationHours(null, new BlockPos(0, 64, 0), "cowparsley", cfgSlow);
            double fast = WildFlowerMaturation.MaturationHours(null, new BlockPos(0, 64, 0), "cowparsley", cfgFast);

            Assert.True(slow > fast);
        }

        [Fact]
        public void JuvenileCode_FollowsSpeciesPattern()
        {
            var code = FlowerJuvenileBlocks.CodeForSpecies("woad");
            Assert.Equal("ecosystemflora", code.Domain);
            Assert.Equal("juvenile-flower-woad-free", code.Path);
        }
    }
}
