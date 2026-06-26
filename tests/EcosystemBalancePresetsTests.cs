using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class EcosystemBalancePresetsTests
    {
        [Fact]
        public void VanillaMinimal_DisablesPhenologyAndJuvenileSpread()
        {
            var cfg = new EcosystemConfig { BalancePreset = EcosystemBalancePresets.VanillaMinimal };
            EcosystemBalancePresets.Apply(cfg, EcosystemBalancePresets.VanillaMinimal);

            Assert.False(cfg.EnableFlowerSpreadMaturation);
            Assert.False(cfg.EnableFernSpreadMaturation);
            Assert.False(cfg.EnableFlowerPhenology);
            Assert.False(cfg.EnableFernPhenology);
            Assert.False(cfg.EnableTallgrassPhenology);
        }

        [Fact]
        public void IsKnownPreset_IncludesVanillaMinimal()
        {
            Assert.True(EcosystemBalancePresets.IsKnownPreset(EcosystemBalancePresets.VanillaMinimal));
        }
    }
}
