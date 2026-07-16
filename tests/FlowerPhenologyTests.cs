using System;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.SpeciesEcology;
using Xunit;

namespace WildFarming.Tests
{
    public class FlowerPhenologyTests
    {
        static EcosystemConfig EnabledCfg => new EcosystemConfig { EnableFlowerPhenology = true };

        [Theory]
        [InlineData("catmint", true)]
        [InlineData("cornflower", true)]
        [InlineData("coopersreed", false)]
        [InlineData("oak", false)]
        public void UsesPhenology_OnlyMeadowFlowers(string species, bool expected)
        {
            var req = new PlantRequirements { Species = species, Habitat = EcologyHabitat.Terrestrial };
            Assert.Equal(expected, FlowerPhenology.UsesPhenology(EnabledCfg, req));
        }

        [Fact]
        public void UsesPhenology_OffWhenConfigDisabled()
        {
            var cfg = new EcosystemConfig { EnableFlowerPhenology = false };
            var req = new PlantRequirements { Species = "catmint", Habitat = EcologyHabitat.Terrestrial };
            Assert.False(FlowerPhenology.UsesPhenology(cfg, req));
        }

        [Theory]
        [InlineData(0.05f, 12f, FlowerPhenologyPhase.Dormant)]
        [InlineData(0.6f, 18f, FlowerPhenologyPhase.Bloom)]
        [InlineData(0.25f, 18f, FlowerPhenologyPhase.Vegetative)]
        [InlineData(0.5f, 40f, FlowerPhenologyPhase.Vegetative)] // heat defers via stress; no snap dieback
        public void InferInitialPhase_RespectsSeasonAndTemperature(
            float season,
            float temp,
            FlowerPhenologyPhase expected)
        {
            var cfg = EnabledCfg;
            Assert.Equal(expected, FlowerPhenology.InferInitialPhase(season, temp, cfg));
        }

        [Fact]
        public void CanSpread_OnlyInBloom()
        {
            var entry = new ReproducerEntry(
                null,
                null,
                null,
                new PlantRequirements { Species = "catmint", Habitat = EcologyHabitat.Terrestrial },
                0);

            entry.PhenologyPhase = FlowerPhenologyPhase.Vegetative;
            Assert.False(FlowerPhenology.CanSpread(entry));

            entry.PhenologyPhase = FlowerPhenologyPhase.Bloom;
            Assert.True(FlowerPhenology.CanSpread(entry));
        }

        [Fact]
        public void Advance_VegetativeAccumulatesEnergyTowardBloom()
        {
            var cfg = EnabledCfg;
            var entry = new ReproducerEntry(
                null,
                null,
                null,
                new PlantRequirements { Species = "wilddaisy", Habitat = EcologyHabitat.Terrestrial },
                0)
            {
                PhenologyPhase = FlowerPhenologyPhase.Vegetative,
                PhenologyEnergy = 0f,
                LastPhenologyUpdateHours = 100,
            };

            FlowerPhenology.AdvanceVegetativeForTests(entry, season: 2f, temp: 18f, cfg, deltaDays: 10);
            Assert.True(entry.PhenologyEnergy > 0.5f);
        }

        [Fact]
        public void ResolveRegisterPhase_MatureFlowerFollowsSeason_NotBloomAppearance()
        {
            var cfg = EnabledCfg;
            var mature = new Block { Code = new AssetLocation("game:flower-catmint-free"), BlockId = 1 };
            var entry = new ReproducerEntry(
                new BlockPos(1, 64, 1),
                new AssetLocation("game:flower-catmint-free"),
                new AssetLocation("game:flower-catmint-free"),
                new PlantRequirements { Species = "catmint", Habitat = EcologyHabitat.Terrestrial },
                0);

            FlowerPhenologyPhase winter = FlowerPhenology.ResolveRegisterPhase(
                api: null,
                entry,
                mature,
                cfg,
                spreadEstablished: false);

            // api null => season/temp default to 0 => dormant
            Assert.Equal(FlowerPhenologyPhase.Dormant, winter);
        }

        [Fact]
        public void ResolveRegisterPhase_PhaseBlockPreservesWorldPhase()
        {
            var cfg = EnabledCfg;
            var dormant = new Block
            {
                Code = new AssetLocation("ecosystemflora:flowerphase-catmint-dormant-free"),
                BlockId = 1,
            };
            var entry = new ReproducerEntry(
                new BlockPos(1, 64, 1),
                null,
                new AssetLocation("game:flower-catmint-free"),
                new PlantRequirements { Species = "catmint", Habitat = EcologyHabitat.Terrestrial },
                0);

            Assert.Equal(
                FlowerPhenologyPhase.Dormant,
                FlowerPhenology.ResolveRegisterPhase(null, entry, dormant, cfg, spreadEstablished: false));
        }

        [Fact]
        public void SampleStress_FrostAndWinterShareColdRate()
        {
            var cfg = EnabledCfg;
            float frost = FlowerPhenology.SampleStressGainPerDay(
                FlowerPhenologyPhase.Vegetative, season: 1f, temp: 0f, cfg, energy: 1f);
            float winter = FlowerPhenology.SampleStressGainPerDay(
                FlowerPhenologyPhase.Vegetative, season: 0.05f, temp: 18f, cfg, energy: 1f);
            Assert.Equal(cfg.FlowerPhenologyColdStressGainPerDay, frost);
            Assert.Equal(frost, winter);
        }

        [Fact]
        public void UpdateStress_WinterAccumulatesTowardEnterThreshold()
        {
            var cfg = EnabledCfg;
            var entry = new ReproducerEntry(
                null,
                null,
                null,
                new PlantRequirements { Species = "catmint", Habitat = EcologyHabitat.Terrestrial },
                0)
            {
                PhenologyPhase = FlowerPhenologyPhase.Vegetative,
                PhenologyStress = 0f,
            };

            FlowerPhenology.UpdateStressForTests(entry, season: 0.05f, temp: 18f, cfg, deltaDays: 5);
            Assert.True(entry.PhenologyStress >= cfg.FlowerPhenologyStressEnterDieback);
        }

        [Fact]
        public void TryEnterDieback_IncrementsLifeCycles()
        {
            var cfg = EnabledCfg;
            var entry = new ReproducerEntry(
                null,
                null,
                null,
                new PlantRequirements { Species = "catmint", Habitat = EcologyHabitat.Terrestrial },
                0)
            {
                PhenologyPhase = FlowerPhenologyPhase.Bloom,
                PhenologyLifeCycles = 0,
                PhenologyStress = cfg.FlowerPhenologyStressEnterDieback,
            };

            Assert.True(FlowerPhenology.TryEnterDieback(api: null, entry, cfg));
            Assert.Equal(1, entry.PhenologyLifeCycles);
            Assert.Equal(FlowerPhenologyPhase.Dieback, entry.PhenologyPhase);
        }

        [Fact]
        public void TryEnterDieback_AtMaxCycles_DoesNotIncrementAgain()
        {
            SpeciesEcologyRegistry.ResetForTests();
            var cfg = EnabledCfg;
            cfg.MaxFlowerPhenologyLifeCycles = 2;
            var entry = new ReproducerEntry(
                null,
                null,
                null,
                new PlantRequirements { Species = "testflower-no-csv", Habitat = EcologyHabitat.Terrestrial },
                0)
            {
                PhenologyPhase = FlowerPhenologyPhase.Vegetative,
                PhenologyLifeCycles = 2,
                PhenologyStress = cfg.FlowerPhenologyStressEnterDieback,
            };

            Assert.True(FlowerPhenology.TryEnterDieback(api: null, entry, cfg));
            Assert.Equal(2, entry.PhenologyLifeCycles);
            // Without EcosystemSystem.Instance, kill is a no-op for world blocks; cycle cap still blocks increment.
            Assert.Equal(FlowerPhenologyPhase.Vegetative, entry.PhenologyPhase);
        }

        [Fact]
        public void ResolveMaxLifeCycles_FallsBackToConfig_WhenSpeciesUnset()
        {
            SpeciesEcologyRegistry.ResetForTests();
            var cfg = EnabledCfg;
            cfg.MaxFlowerPhenologyLifeCycles = 9;
            Assert.Equal(9, FlowerPhenology.ResolveMaxLifeCycles("testflower-no-csv", cfg));
        }

        [Fact]
        public void ResolveMaxLifeCycles_PrefersSpeciesCsv()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "wf-pheno-life-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string userCsv = Path.Combine(tempDir, "ecology.csv");
            File.WriteAllText(userCsv, "species,flower_phenology_life_cycles\ncatmint,2\n");

            try
            {
                SpeciesEcologyRegistry.LoadFromPaths(FindRepoRoot(), userCsv, appendMissingUserRows: false);
                var cfg = EnabledCfg;
                cfg.MaxFlowerPhenologyLifeCycles = 9;
                Assert.Equal(2, FlowerPhenology.ResolveMaxLifeCycles("catmint", cfg));
            }
            finally
            {
                SpeciesEcologyRegistry.ResetForTests();
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
        }

        [Theory]
        [InlineData("cowparsley", 2)]
        [InlineData("heather", 8)]
        [InlineData("westerngorse", 7)]
        [InlineData("horsetail", 6)]
        public void WildFlowerPhenologyLife_ExportDefaults_MatchLongevityBands(string species, int expected)
        {
            Assert.True(WildFlowerPhenologyLife.TryGet(species, out int cycles));
            Assert.Equal(expected, cycles);
        }

        static string FindRepoRoot()
        {
            string dir = Directory.GetCurrentDirectory();
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "wildfarming.sln"))) return dir;
                dir = Directory.GetParent(dir)?.FullName;
            }

            return Directory.GetCurrentDirectory();
        }

        [Fact]
        public void Advance_BloomDepletesEnergy_WithoutInstantDieback()
        {
            var cfg = EnabledCfg;
            var entry = new ReproducerEntry(
                null,
                null,
                null,
                new PlantRequirements { Species = "wilddaisy", Habitat = EcologyHabitat.Terrestrial },
                0)
            {
                PhenologyPhase = FlowerPhenologyPhase.Bloom,
                PhenologyEnergy = cfg.FlowerBloomEnergyThreshold,
                LastPhenologyUpdateHours = 100,
            };

            FlowerPhenology.AdvanceBloomForTests(entry, season: 0.1f, temp: 18f, cfg, deltaDays: 12);
            Assert.Equal(FlowerPhenologyPhase.Bloom, entry.PhenologyPhase);
            Assert.True(entry.PhenologyEnergy < cfg.FlowerBloomEnergyThreshold);
        }
    }
}
