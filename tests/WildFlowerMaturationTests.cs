using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class WildFlowerMaturationTests
    {
        [Theory]
        [InlineData("cowparsley", true)]
        [InlineData("catmint", true)]
        [InlineData("cornflower", true)]
        [InlineData("daffodil", true)]
        [InlineData("rafflesiabrown", true)]
        [InlineData("redtopgrass", true)]
        public void UsesMaturation_AllEcologyFlowersWhenEnabled(string species, bool expected)
        {
            var cfg = new EcosystemConfig { EnableFlowerSpreadMaturation = true };
            Assert.Equal(expected, WildFlowerMaturation.UsesMaturation(cfg, species));
        }

        [Fact]
        public void UsesMaturation_OffWhenConfigDisabled()
        {
            var cfg = new EcosystemConfig { EnableFlowerSpreadMaturation = false };
            Assert.False(WildFlowerMaturation.UsesMaturation(cfg, "cowparsley"));
            Assert.False(WildFlowerMaturation.UsesMaturation(cfg, "catmint"));
        }

        [Fact]
        public void UsesPostSpreadAttemptCooldown_WhenMaturationConfigOff()
        {
            var cfg = new EcosystemConfig { EnableFlowerSpreadMaturation = false };
            Assert.False(WildFlowerMaturation.UsesMaturation(cfg, "catmint"));
            Assert.True(WildFlowerMaturation.UsesPostSpreadAttemptCooldown(cfg, "catmint"));
            Assert.True(WildFlowerMaturation.UsesPostSpreadAttemptCooldown(cfg, "redtopgrass"));
        }

        [Fact]
        public void UsesPostSpreadAttemptCooldown_OffWhenConfigDisabled()
        {
            var cfg = new EcosystemConfig { EnableFlowerSpreadAttemptCooldown = false };
            Assert.False(WildFlowerMaturation.UsesPostSpreadAttemptCooldown(cfg, "catmint"));
        }

        [Fact]
        public void TryGetProfile_IncludesRedtopgrassColonizer()
        {
            Assert.True(WildFlowerMaturation.TryGetProfile("redtopgrass", out var profile));
            Assert.Equal(36, profile.MaturationHours);
            Assert.Equal(16, profile.PostSpreadAttemptCooldownHours);
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
        public void PostSpreadAttemptCooldown_ScalesWithFlowerSpreadCooldownHoursMultiplier()
        {
            var cfgSlow = new EcosystemConfig
            {
                UseSeasonalEcology = false,
                FlowerSpreadCooldownHoursMultiplier = 0.5f,
            };
            var cfgFast = new EcosystemConfig
            {
                UseSeasonalEcology = false,
                FlowerSpreadCooldownHoursMultiplier = 2f,
            };
            var req = new PlantRequirements { Species = "catmint" };

            double slow = WildFlowerMaturation.PostSpreadAttemptCooldownHours(
                null, new BlockPos(0, 64, 0), req, cfgSlow);
            double fast = WildFlowerMaturation.PostSpreadAttemptCooldownHours(
                null, new BlockPos(0, 64, 0), req, cfgFast);

            Assert.True(slow > fast);
        }

        [Fact]
        public void PostSpreadAttemptCooldown_CatmintUsesSteadyMeadowHours()
        {
            var cfg = new EcosystemConfig { UseSeasonalEcology = false };
            var req = new PlantRequirements { Species = "catmint" };
            double hours = WildFlowerMaturation.PostSpreadAttemptCooldownHours(
                null, new BlockPos(0, 64, 0), req, cfg);

            Assert.Equal(24, hours);
        }

        [Fact]
        public void FailedSpreadAttemptCooldown_IsShorterThanPostSpread()
        {
            var cfg = new EcosystemConfig { UseSeasonalEcology = false };
            var req = new PlantRequirements { Species = "catmint" };
            double failed = WildFlowerMaturation.FailedSpreadAttemptCooldownHours(
                null, new BlockPos(0, 64, 0), req, cfg);
            double post = WildFlowerMaturation.PostSpreadAttemptCooldownHours(
                null, new BlockPos(0, 64, 0), req, cfg);

            Assert.InRange(failed, 1, 4);
            Assert.True(failed < post);
        }

        [Fact]
        public void TryGetProfile_FallsBackForUnlistedKnownFlower()
        {
            Assert.True(WildFlowerMaturation.TryGetProfile("goldenpoppy", out var profile));
            Assert.Equal(72, profile.MaturationHours);
            Assert.Equal(36, profile.PostSpreadAttemptCooldownHours);
        }

        [Fact]
        public void JuvenileCode_FollowsSpeciesPattern()
        {
            var code = FlowerJuvenileBlocks.CodeForSpecies("catmint");
            Assert.Equal("ecosystemflora", code.Domain);
            Assert.Equal("juvenile-flower-catmint-free", code.Path);
        }

        [Fact]
        public void GetEcologySpecies_ResolvesJuvenileFlowerBlocks()
        {
            var code = FlowerJuvenileBlocks.CodeForSpecies("catmint");
            Assert.Equal("catmint", PlantCodeHelper.GetEcologySpecies(code));
            Assert.Equal("catmint", PlantCodeHelper.ResolveEcologySpecies(new Vintagestory.API.Common.Block { Code = code }));
        }
    }
}
