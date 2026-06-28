using Vintagestory.API.Common;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class ExtendedVanillaFloraTests
    {
        [Theory]
        [InlineData("game:tallplant-brownsedge-land-normal-free", "brownsedge")]
        [InlineData("game:barrelcactus-normal", "barrelcactus")]
        [InlineData("game:silvertorchcactus", "silvertorchcactus")]
        [InlineData("game:flower-rafflesia-brown", "rafflesiabrown")]
        [InlineData("game:flower-rafflesia-red", "rafflesiared")]
        [InlineData("game:flower-croton-small-crimson-green", "croton")]
        [InlineData("game:frostedtallgrass-fern-free", "tallgrass")]
        public void NewVanillaFlora_ParsesAsEcologySpecies(string code, string expectedSpecies)
        {
            Assert.Equal(expectedSpecies, PlantCodeHelper.GetEcologySpecies(new AssetLocation(code)));
            Assert.True(PlantCodeHelper.IsEcologyPlant(new Block { Code = new AssetLocation(code) }));
        }

        [Fact]
        public void Brownsedge_UsesShoreSedgeEcology_AsSlowWetlandClump()
        {
            const string species = EcologyShoreSedgeSpecies.Brownsedge;
            Assert.True(WildShoreSedgeEcology.TryGet(species, out var entry));
            Assert.False(WildFlowerClimate.TryGet(species, out _));
            Assert.False(TurfColonizerSpread.PrefersOccupiedTurf(species));
            Assert.Equal(PlantSoilRole.WetlandHerb, ResolveRole(species));
            Assert.Equal(0.35f, entry.SpreadRate);
            Assert.Equal(0.78f, entry.MinRain);
            Assert.Equal(0f, entry.SeedDispersalChance);
            Assert.Equal(0, entry.SeedDispersalRadius);
            Assert.Equal(1, entry.SameSpeciesSpacing);

            EcosystemConfig.Loaded = new EcosystemConfig { EnableShoreSedgeMatSpread = true };
            var req = new PlantRequirements { Species = species, Habitat = EcologyHabitat.Terrestrial };
            ShoreSedgeMatSpread.ApplyTo(req);
            Assert.True(req.UsesShoreSedgeMatSpread);
        }

        [Fact]
        public void Brownsedge_UsesPostSpreadAttemptCooldown_NotJuvenileMaturation()
        {
            var cfg = new EcosystemConfig { EnableFlowerSpreadMaturation = false };
            const string species = EcologyShoreSedgeSpecies.Brownsedge;
            Assert.False(WildFlowerMaturation.UsesMaturation(cfg, species));
            Assert.True(SpreadMaturationPolicies.UsesPostSpreadAttemptCooldown(cfg, species));
            Assert.True(WildFlowerMaturation.TryGetProfile(species, out var profile));
            Assert.Equal(48, profile.PostSpreadAttemptCooldownHours);
        }

        [Fact]
        public void DesertCacti_HaveDesertEcology_AndStrongHold()
        {
            Assert.True(WildDesertEcology.TryGet(EcologyDesertSpecies.Barrelcactus, out _));
            Assert.True(WildDesertEcology.TryGet(EcologyDesertSpecies.Silvertorchcactus, out _));
            Assert.True(WildSpeciesModifiers.TryGet(EcologyDesertSpecies.Barrelcactus, out var barrel));
            Assert.True(WildSpeciesModifiers.TryGet("tallgrass", out var grass));
            Assert.True(barrel.HoldStrength > grass.HoldStrength);
        }

        [Theory]
        [InlineData("croton")]
        [InlineData("rafflesiabrown")]
        [InlineData("rafflesiared")]
        public void TropicalFlowers_HaveFlowerClimateProfiles(string species)
        {
            Assert.True(EcologyFlowerSpecies.IsKnownFlower(species));
            Assert.True(WildFlowerClimate.TryGet(species, out _));
        }

        [Fact]
        public void FrostedTallgrass_MapsToTallgrassEcology()
        {
            Assert.True(WildTallgrassEcology.TryGet("tallgrass", out var matrix));
            Assert.True(WildSpeciesModifiers.TryGet("tallgrass", out _));
            Assert.Equal("tallgrass", PlantCodeHelper.GetEcologySpecies(new AssetLocation("game:frostedtallgrass-tall-free")));
        }

        static PlantSoilRole ResolveRole(string species)
        {
            WildSpeciesSoilSuccession.TryGetRole(species, out PlantSoilRole role);
            return role;
        }
    }
}
