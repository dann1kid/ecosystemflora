using WildFarming.Ecosystem;
using WildFarming.Ecosystem.SpeciesEcology;
using Xunit;

namespace WildFarming.Tests
{
    public class FernRhizomeSpreadTests
    {
        [Fact]
        public void ApplyTo_SetsFernRhizomeMatMode()
        {
            EcosystemConfig.Loaded = new EcosystemConfig { EnableFernRhizomeSpread = true };

            var req = new PlantRequirements { Species = "eaglefern", Habitat = EcologyHabitat.Terrestrial };
            FernRhizomeSpread.ApplyTo(req);

            Assert.True(req.UsesFernRhizomeSpread);
            Assert.Equal(1, req.SpreadRadius);
        }

        [Fact]
        public void ApplyTo_NullSpecies_DoesNotThrow()
        {
            EcosystemConfig.Loaded = new EcosystemConfig { EnableFernRhizomeSpread = true };
            SpeciesEcologyRegistry.ResetForTests();
            SpeciesEcologyRegistry.LoadFromPaths("", null, appendMissingUserRows: false);

            var req = new PlantRequirements { Species = null, Habitat = EcologyHabitat.Terrestrial };
            FernRhizomeSpread.ApplyTo(req);

            Assert.False(req.UsesFernRhizomeSpread);
        }

        [Fact]
        public void IsOrthogonalStep_OnlyCardinalNeighbors()
        {
            Assert.True(FernRhizomeSpread.IsOrthogonalStep(1, 0));
            Assert.True(FernRhizomeSpread.IsOrthogonalStep(0, -1));
            Assert.False(FernRhizomeSpread.IsOrthogonalStep(1, 1));
            Assert.False(FernRhizomeSpread.IsOrthogonalStep(2, 0));
        }
    }

    public class WildFernSpreadTests
    {
        [Fact]
        public void UsesMaturation_OnlyWhenEnabled()
        {
            var on = new EcosystemConfig { EnableFernSpreadMaturation = true };
            var off = new EcosystemConfig { EnableFernSpreadMaturation = false };

            Assert.True(WildFernSpread.UsesMaturation(on, "hartstongue"));
            Assert.False(WildFernSpread.UsesMaturation(off, "hartstongue"));
            Assert.False(WildFernSpread.UsesMaturation(on, "catmint"));
        }

        [Fact]
        public void TryApplySpreadAttemptCooldown_SetsParentPause()
        {
            var cfg = new EcosystemConfig
            {
                EnableFernSpreadAttemptCooldown = true,
                UseSeasonalEcology = false,
            };
            var entry = new ReproducerEntry(
                null,
                null,
                null,
                new PlantRequirements { Species = "eaglefern" },
                0);

            Assert.True(WildFernSpread.TryApplySpreadAttemptCooldown(
                entry,
                nowHours: 10,
                api: null,
                pos: null,
                entry.Requirements,
                cfg,
                failedChanceRoll: false));

            Assert.Equal(44, entry.NextSpawnAllowedAtHours);
        }

        [Theory]
        [InlineData(0.1f, false)]
        [InlineData(0.5f, true)]
        public void IsSporulationSeasonActive_UsesSeasonThreshold(float seasonMult, bool expected)
        {
            Assert.Equal(expected, WildFernSpread.IsSporulationSeasonActive(seasonMult));
        }

        [Fact]
        public void Hartstongue_HasLowerSpreadRateThanBefore()
        {
            Assert.True(WildFernEcology.TryGet("hartstongue", out WildFernEcology.EcologyEntry entry));
            Assert.Equal(0.82f, entry.SpreadRate);
            Assert.Equal(2, entry.SameSpeciesSpacing);
        }
    }

    public class FernJuvenileBlocksTests
    {
        [Theory]
        [InlineData("eaglefern", "fern-eaglefern")]
        [InlineData("tallfern", "tallfern")]
        public void MatureVanillaCode_UsesGamePaths(string species, string expectedPath)
        {
            var code = FernJuvenileBlocks.MatureVanillaCode(species);
            Assert.Equal("game", code.Domain);
            Assert.Equal(expectedPath, code.Path);
        }

        [Fact]
        public void CodeForSpecies_UsesEcosystemfloraDomain()
        {
            var code = FernJuvenileBlocks.CodeForSpecies("hartstongue");
            Assert.Equal("ecosystemflora", code.Domain);
            Assert.Equal("juvenile-fern-hartstongue-free", code.Path);
        }
    }
}
