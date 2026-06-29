using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class WildTreeSeralTests
    {
        [Theory]
        [InlineData("birch", "Pioneer")]
        [InlineData("acacia", "Pioneer")]
        [InlineData("maple", "Mid")]
        [InlineData("oak", "Mid")]
        [InlineData("larch", "Climax")]
        [InlineData("ebony", "Climax")]
        public void Profile_AssignsExpectedSeralRole(string wood, string expectedRole)
        {
            Assert.True(WildTreeEcology.TryGet(wood, out WildTreeEcology.Profile profile));
            Assert.Equal(expectedRole, profile.SeralRole.ToString());
        }

        [Fact]
        public void Pioneer_PrefersOpenCoverOverClimax()
        {
            float pioneerOpen = WildTreeEcology.SeralSpreadMultiplier(
                TreeSeralRole.Pioneer, localForestCover: 0.08f);
            float climaxOpen = WildTreeEcology.SeralSpreadMultiplier(
                TreeSeralRole.Climax, localForestCover: 0.08f);

            Assert.True(pioneerOpen > climaxOpen);
        }

        [Fact]
        public void Climax_PrefersMatureCoverOverPioneer()
        {
            float pioneerMature = WildTreeEcology.SeralSpreadMultiplier(
                TreeSeralRole.Pioneer, localForestCover: 0.55f);
            float climaxMature = WildTreeEcology.SeralSpreadMultiplier(
                TreeSeralRole.Climax, localForestCover: 0.55f);

            Assert.True(climaxMature > pioneerMature);
        }

        [Theory]
        [InlineData("birch", 0.45f)]
        [InlineData("acacia", 0.40f)]
        public void Pioneer_ReproduceFitnessZeroInDenseCover(string wood, float denseCover)
        {
            Assert.True(WildTreeEcology.TryGet(wood, out WildTreeEcology.Profile profile));

            var req = new PlantRequirements
            {
                Species = wood,
                Habitat = EcologyHabitat.TerrestrialTree,
                MinForest = profile.MinForest,
                MaxForest = profile.MaxForest,
                MinRain = 0f,
                MaxRain = 1f,
            };

            var ctx = new StubContext { LocalForestCover = denseCover };
            Assert.Equal(0f, SuitabilityEvaluator.ReproduceFitness(req, ctx));
        }

        [Fact]
        public void ReproduceFitness_AppliesSeralMultiplierForTrees()
        {
            EcosystemConfig.Loaded = new EcosystemConfig
            {
                EnableTreeSeralSuccession = true,
                ApplyWorldgenRainForest = false,
            };

            Assert.True(WildTreeEcology.TryGet("birch", out WildTreeEcology.Profile profile));

            var req = new PlantRequirements
            {
                Species = "birch",
                Habitat = EcologyHabitat.TerrestrialTree,
                MinForest = profile.MinForest,
                MaxForest = profile.MaxForest,
                MinRain = 0f,
                MaxRain = 1f,
            };

            float openFitness = SuitabilityEvaluator.ReproduceFitness(
                req, new StubContext { LocalForestCover = 0.08f });
            float midFitness = SuitabilityEvaluator.ReproduceFitness(
                req, new StubContext { LocalForestCover = 0.32f });

            Assert.True(openFitness > midFitness);
        }

        [Fact]
        public void SeralSpreadMultiplier_OffWhenConfigDisabled()
        {
            EcosystemConfig.Loaded = new EcosystemConfig { EnableTreeSeralSuccession = false };
            Assert.Equal(1f, WildTreeEcology.SeralSpreadMultiplier("birch", 0.5f));
        }

        [Fact]
        public void Pioneer_HasHigherSaplingSunlightThanClimax()
        {
            Assert.True(WildTreeEcology.TryGet("birch", out WildTreeEcology.Profile pioneer));
            Assert.True(WildTreeEcology.TryGet("ebony", out WildTreeEcology.Profile climax));
            Assert.True(pioneer.SaplingMinSunlight > climax.SaplingMinSunlight);
        }

        [Fact]
        public void Oak_PrefersSemiOpenCoverOverDenseForest()
        {
            EcosystemConfig.Loaded = new EcosystemConfig
            {
                EnableTreeSeralSuccession = true,
                ApplyWorldgenRainForest = false,
            };

            Assert.True(WildTreeEcology.TryGet("oak", out WildTreeEcology.Profile profile));

            var req = new PlantRequirements
            {
                Species = "oak",
                Habitat = EcologyHabitat.TerrestrialTree,
                MinForest = profile.MinForest,
                MaxForest = profile.MaxForest,
                MinRain = 0f,
                MaxRain = 1f,
            };

            float semiOpen = SuitabilityEvaluator.ReproduceFitness(
                req, new StubContext { LocalForestCover = 0.30f });
            float dense = SuitabilityEvaluator.ReproduceFitness(
                req, new StubContext { LocalForestCover = 0.85f });

            Assert.True(semiOpen > dense);
            Assert.Equal(0f, dense);
        }

        [Fact]
        public void Oak_RequiresHigherSaplingSunlightThanDefaultMid()
        {
            Assert.True(WildTreeEcology.TryGet("oak", out WildTreeEcology.Profile oak));
            Assert.True(WildTreeEcology.TryGet("maple", out WildTreeEcology.Profile maple));
            Assert.Equal(11, oak.SaplingMinSunlight);
            Assert.True(oak.SaplingMinSunlight > maple.SaplingMinSunlight);
        }

        [Fact]
        public void Oak_SpeciesSeralPeak_PrefersSemiOpenOverDeepForest()
        {
            EcosystemConfig.Loaded = new EcosystemConfig { EnableTreeSeralSuccession = true };

            float semiOpen = WildTreeEcology.SeralSpreadMultiplier("oak", 0.35f);
            float deep = WildTreeEcology.SeralSpreadMultiplier("oak", 0.85f);

            Assert.True(semiOpen > deep);
        }

        [Fact]
        public void Pine_ToleratesCloserSpacingThanOak()
        {
            Assert.True(WildTreeEcology.TryGet("pine", out WildTreeEcology.Profile pine));
            Assert.True(WildTreeEcology.TryGet("oak", out WildTreeEcology.Profile oak));
            Assert.True(pine.SameSpeciesSpacing < oak.SameSpeciesSpacing);
        }

        [Theory]
        [InlineData("birch", FloraContextAffinity.Open)]
        [InlineData("oak", FloraContextAffinity.Edge)]
        public void ModifierProfile_MatchesSeralRole(string wood, FloraContextAffinity expectedAffinity)
        {
            Assert.True(WildTreeEcology.TryGetModifierProfile(wood, out WildSpeciesModifiers.Profile mod));
            Assert.Equal(expectedAffinity, mod.ContextAffinity);
        }
    }
}
