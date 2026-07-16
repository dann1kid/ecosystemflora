using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Config;
using Xunit;

namespace WildFarming.Tests
{
    public class EcosystemBalancePresetsTests
    {
        [Fact]
        public void DefaultBalancePreset_IsNatural()
        {
            Assert.Equal(EcosystemBalancePresets.Natural, new EcosystemConfig().BalancePreset);
        }

        [Fact]
        public void Default_DisablesAnimalFootTraffic()
        {
            Assert.False(new EcosystemConfig().EnableAnimalFootTraffic);
        }

        [Fact]
        public void Natural_EnablesCoreEcologyFeatures()
        {
            var cfg = new EcosystemConfig
            {
                EnableFlowerSpreadMaturation = false,
                EnableFlowerPhenology = false,
                EnableTrampling = false,
                EnableAnimalFootTraffic = false,
                EnableTreeAging = false,
                EnableMyceliumEcology = false,
                ApplyCrossHabitatSpacing = false,
                BalancePreset = EcosystemBalancePresets.Natural,
            };

            EcosystemBalancePresets.Apply(cfg, EcosystemBalancePresets.Natural);

            Assert.True(cfg.EnableFlowerSpreadMaturation);
            Assert.True(cfg.EnableTallgrassSpreadMaturation);
            Assert.True(cfg.EnableFernSpreadMaturation);
            Assert.True(cfg.EnableBerrySpreadMaturation);
            Assert.True(cfg.EnableFlowerPhenology);
            Assert.True(cfg.EnableFernPhenology);
            Assert.True(cfg.EnableTallgrassPhenology);
            Assert.True(cfg.UseSeasonalEcology);
            Assert.True(cfg.EnableSeasonalFoliage);
            Assert.False(cfg.EnableTrampling);
            Assert.False(cfg.TramplingSoilDegradation);
            Assert.False(cfg.EnableAnimalFootTraffic);
            Assert.Equal(2000, cfg.ReproduceTickIntervalMs);
            Assert.Equal(1000, cfg.ChunkScanTickIntervalMs);
            Assert.Equal(48, cfg.FoliageColumnScanHeightAboveSurface);
            Assert.Equal(48, cfg.MaxFoliageCellsTickedPerTick);
            Assert.True(cfg.EnableTreeAging);
            Assert.True(cfg.EnableTreeSenescence);
            Assert.True(cfg.EnableWildVineEcology);
            Assert.True(cfg.EnableMyceliumEcology);
            Assert.True(cfg.EnableSymbiosis);
            Assert.True(cfg.UseSoilSuccession);
            Assert.True(cfg.ApplyCrossHabitatSpacing);
            Assert.True(cfg.EnableChunkFairSpread);
            Assert.True(cfg.EnableEventDrivenSpread);
            Assert.Equal(72, cfg.ReproduceAttemptsPerYear);
            Assert.Equal(1f / 3f, cfg.SpeciesSpreadRateScale, 3);
        }

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
