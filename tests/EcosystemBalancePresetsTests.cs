using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Config;
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

        [Fact]
        public void IsKnownPreset_IncludesTimelapse()
        {
            Assert.True(EcosystemBalancePresets.IsKnownPreset(EcosystemBalancePresets.Timelapse));
        }

        [Fact]
        public void Timelapse_MaxSpreadAndPerfBudgets()
        {
            var cfg = new EcosystemConfig { BalancePreset = EcosystemBalancePresets.Timelapse };
            EcosystemBalancePresets.Apply(cfg, EcosystemBalancePresets.Timelapse);

            Assert.Equal(100000, cfg.ReproduceAttemptsPerYear);
            Assert.Equal(1f, cfg.ReproduceChance);
            Assert.Equal(0.1f, cfg.MinFitness);
            Assert.Equal(10f, cfg.SpeciesSpreadRateScale);
            Assert.False(cfg.EnableTwoPhaseSpreadPlacement);
            Assert.False(cfg.EnableBackgroundSpreadSolve);
            Assert.True(cfg.EnableChunkFairSpread);
            Assert.Equal(1f, cfg.DisplacementHoldMargin);
            Assert.False(cfg.EnableEventDrivenSpread);
            Assert.Equal(2048, cfg.MaxReproduceAttemptsPerTick);
            Assert.Equal(16, cfg.MaxSpreadAttemptsPerChunkPerTick);
            Assert.Equal(25, cfg.ReproduceTickIntervalMs);
            Assert.Equal(17, cfg.ChunkScanTickIntervalMs);
        }

        [Fact]
        public void Timelapse_PassesConfigValidator()
        {
            var cfg = new EcosystemConfig();
            EcosystemBalancePresets.Apply(cfg, EcosystemBalancePresets.Timelapse);

            Assert.True(
                EcosystemConfigValidator.TryValidate(cfg, out string[] errors),
                string.Join(", ", errors ?? System.Array.Empty<string>()));
        }
    }
}
