using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class WildFernEcologyDifferentiationTests
    {
        [Fact]
        public void Cinnamonfern_ColderAndWetterThanDeerfern()
        {
            Assert.True(WildFernEcology.TryGet("cinnamonfern", out WildFernEcology.EcologyEntry cinnamon));
            Assert.True(WildFernEcology.TryGet("deerfern", out WildFernEcology.EcologyEntry deer));

            Assert.True(cinnamon.MinTemp < deer.MinTemp);
            Assert.True(cinnamon.MaxTemp < deer.MaxTemp);
            Assert.True(cinnamon.MinRain > deer.MinRain);
            Assert.True(cinnamon.SpreadRate < deer.SpreadRate);
        }

        [Fact]
        public void Eaglefern_HasCoolBorealEnvelope()
        {
            Assert.True(WildFernEcology.TryGet("eaglefern", out WildFernEcology.EcologyEntry eagle));
            Assert.True(WildFernEcology.TryGet("tallfern", out WildFernEcology.EcologyEntry tall));

            Assert.True(eagle.MaxTemp < tall.MinTemp);
            Assert.True(eagle.MinRain < 0.4f);
        }

        [Fact]
        public void Tallfern_PrefersWarmerForestEdge_NotDeepInterior()
        {
            Assert.True(WildFernEcology.TryGet("tallfern", out WildFernEcology.EcologyEntry tall));
            Assert.Equal(0.45f, tall.MinForest);
            Assert.Equal(0.88f, tall.MaxForest);
        }

        [Fact]
        public void Hartstongue_OpenWetMeadow_NotForestSymbiont()
        {
            Assert.True(WildFernEcology.TryGet("hartstongue", out WildFernEcology.EcologyEntry hart));
            Assert.Equal(0f, hart.MinForest);
            Assert.False(FloraSymbiosis.TryGetRule("hartstongue", out _));
            Assert.True(WildSpeciesSoilSuccession.TryGetRole("hartstongue", out PlantSoilRole hartRole));
            Assert.Equal(PlantSoilRole.WetlandHerb, hartRole);
        }

        [Fact]
        public void SeasonProfiles_DifferAcrossWetForestFerns()
        {
            var eagle = WildSpeciesSeason.Resolve("eaglefern");
            var cinnamon = WildSpeciesSeason.Resolve("cinnamonfern");
            var deer = WildSpeciesSeason.Resolve("deerfern");

            Assert.NotEqual(eagle.SpreadMultiplier(4), cinnamon.SpreadMultiplier(4));
            Assert.NotEqual(cinnamon.SpreadMultiplier(3), deer.SpreadMultiplier(3));
        }

        [Fact]
        public void NicheProfiles_DifferCinnamonDeepShadeVsDeerShade()
        {
            Assert.True(WildSpeciesNiche.TryGet("cinnamonfern", out WildSpeciesNiche.Profile cinnamon));
            Assert.True(WildSpeciesNiche.TryGet("deerfern", out WildSpeciesNiche.Profile deer));

            Assert.Equal(MoistureLevel.Wet, cinnamon.PreferredMoisture);
            Assert.Equal(LightLevel.DeepShade, cinnamon.PreferredLight);
            Assert.Equal(LightLevel.Shade, deer.PreferredLight);
        }

        [Fact]
        public void AllFerns_HaveJuvenileSpreadAssets()
        {
            foreach (string species in EcologyFernSpecies.All)
            {
                Assert.NotNull(FernJuvenileBlocks.CodeForSpecies(species));
            }
        }
    }
}
