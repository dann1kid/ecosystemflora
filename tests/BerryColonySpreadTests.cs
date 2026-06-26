using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class BerryColonySpreadTests
    {
        [Theory]
        [InlineData("blueberry")]
        [InlineData("blackcurrant")]
        [InlineData("blackberry")]
        public void ApplyTo_WhenEnabled_SetsBerryColonyMat(string species)
        {
            EcosystemConfig.Loaded = new EcosystemConfig { EnableBerryColonySpread = true };

            var req = new PlantRequirements { Species = species, Habitat = EcologyHabitat.Terrestrial };
            BerryColonySpread.ApplyTo(req);

            Assert.True(req.UsesBerryColonySpread);
            Assert.Equal(1, req.SpreadRadius);
        }

        [Fact]
        public void ApplyTo_Beautyberry_StaysIndependent()
        {
            EcosystemConfig.Loaded = new EcosystemConfig { EnableBerryColonySpread = true };

            var req = new PlantRequirements { Species = "beautyberry", Habitat = EcologyHabitat.Terrestrial };
            BerryColonySpread.ApplyTo(req);

            Assert.False(req.UsesBerryColonySpread);
            Assert.Equal(5, req.SpreadRadius);
        }

        [Fact]
        public void ApplyTo_WhenDisabled_FallsBackToIndependentRadius()
        {
            EcosystemConfig.Loaded = new EcosystemConfig { EnableBerryColonySpread = false };

            var req = new PlantRequirements { Species = "blueberry", Habitat = EcologyHabitat.Terrestrial };
            BerryColonySpread.ApplyTo(req);

            Assert.False(req.UsesBerryColonySpread);
        }

        [Theory]
        [InlineData("blueberry", 1, 0, true)]
        [InlineData("blueberry", 1, 1, false)]
        [InlineData("blackberry", 1, 1, true)]
        [InlineData("blackberry", 2, 0, false)]
        public void IsStep_RespectsConnectivity(string species, int dx, int dz, bool expected)
        {
            Assert.Equal(expected, BerryColonySpread.IsStep(dx, dz, species));
        }

        [Fact]
        public void WildBerryEcology_AllTypesHaveProfiles()
        {
            foreach (string type in WildBerryEcology.AllTypes)
            {
                Assert.True(WildBerryEcology.TryGet(type, out WildBerryEcology.Profile profile), type);
                Assert.True(profile.SpreadRate > 0f, type);
            }
        }

        [Theory]
        [InlineData("blueberry", FloraContextAffinity.Forest)]
        [InlineData("blackcurrant", FloraContextAffinity.Edge)]
        [InlineData("cranberry", FloraContextAffinity.Open)]
        [InlineData("beautyberry", FloraContextAffinity.Edge)]
        public void WildSpeciesModifiers_BerriesMatchHabitat(string species, FloraContextAffinity expected)
        {
            Assert.True(WildSpeciesModifiers.TryGet(species, out WildSpeciesModifiers.Profile profile));
            Assert.Equal(expected, profile.ContextAffinity);
        }
    }
}
