using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class FlowerSpreadIntegrationTests
    {
        static ReproducerEntry MakeFlowerEntry(string species, double nextAttemptHours, double spawnAllowedAt = 0)
        {
            return new ReproducerEntry(
                new BlockPos(8, 64, 8),
                new AssetLocation("game", "flower-" + species + "-free"),
                new AssetLocation("game", "flower-" + species + "-free"),
                new PlantRequirements { Species = species, Habitat = EcologyHabitat.Terrestrial },
                nextAttemptHours)
            {
                NextSpawnAllowedAtHours = spawnAllowedAt,
            };
        }

        [Theory]
        [InlineData("catmint")]
        [InlineData("redtopgrass")]
        public void ClassifyDueReason_BlocksWakeDuringPostSpreadCooldown(string species)
        {
            var entry = MakeFlowerEntry(species, nextAttemptHours: 0, spawnAllowedAt: 100);
            entry.WakeGeneration = 3;
            entry.LastProcessedWakeGeneration = 0;

            Assert.Equal(
                ReproducerRegistry.SpreadDueReason.None,
                ReproducerRegistry.ClassifyDueReason(entry, now: 10, eventDriven: true));
        }

        [Theory]
        [InlineData("catmint", 24)]
        [InlineData("redtopgrass", 16)]
        public void TryApplySpreadAttemptCooldown_SetsPostSpreadPause(string species, double expectedCooldownHours)
        {
            var cfg = new EcosystemConfig
            {
                EnableFlowerSpreadAttemptCooldown = true,
                UseSeasonalEcology = false,
            };
            var entry = MakeFlowerEntry(species, nextAttemptHours: 0);
            var req = entry.Requirements;

            Assert.True(WildFlowerMaturation.TryApplySpreadAttemptCooldown(
                entry,
                nowHours: 50,
                api: null,
                entry.Origin,
                req,
                cfg,
                failedChanceRoll: false));

            Assert.Equal(50 + expectedCooldownHours, entry.NextSpawnAllowedAtHours);
        }

        [Fact]
        public void TryApplySpreadAttemptCooldown_FailedChanceRollUsesShorterPause()
        {
            var cfg = new EcosystemConfig
            {
                EnableFlowerSpreadAttemptCooldown = true,
                UseSeasonalEcology = false,
            };
            var entry = MakeFlowerEntry("catmint", nextAttemptHours: 0);
            var req = entry.Requirements;

            Assert.True(WildFlowerMaturation.TryApplySpreadAttemptCooldown(
                entry,
                nowHours: 20,
                api: null,
                entry.Origin,
                req,
                cfg,
                failedChanceRoll: true));

            Assert.Equal(23, entry.NextSpawnAllowedAtHours);
        }

        [Fact]
        public void Redtopgrass_UsesGrassColonizerSpeciesNotFlowerList()
        {
            Assert.False(EcologyFlowerSpecies.IsKnownFlower("redtopgrass"));
            Assert.True(EcologyGrassColonizerSpecies.IsKnown("redtopgrass"));

            var cfg = new EcosystemConfig { EnableFlowerSpreadMaturation = true };
            Assert.True(WildFlowerMaturation.UsesMaturation(cfg, "redtopgrass"));
        }

        [Theory]
        [InlineData("rafflesiared", "flower-rafflesia-red-free")]
        [InlineData("rafflesiabrown", "flower-rafflesia-brown-free")]
        [InlineData("croton", "flower-croton-small-crimson-green-free")]
        public void MatureVanillaCode_UsesVanillaVariantPaths(string species, string expectedPath)
        {
            AssetLocation code = FlowerJuvenileBlocks.MatureVanillaCode(species);
            Assert.Equal("game", code.Domain);
            Assert.Equal(expectedPath, code.Path);
        }
    }
}
