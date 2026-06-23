using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class MeadowSoilSuccessionTests
    {
        [Theory]
        [InlineData("wilddaisy")]
        [InlineData("cornflower")]
        [InlineData("goldenpoppy")]
        [InlineData("forgetmenot")]
        [InlineData("cowparsley")]
        [InlineData("catmint")]
        [InlineData("daffodil")]
        [InlineData("lupine")]
        [InlineData("mugwort")]
        [InlineData("woad")]
        [InlineData("orangemallow")]
        [InlineData("tallgrass")]
        [InlineData(EcologyGrassColonizerSpecies.Redtopgrass)]
        public void OpenMeadowSpecies_UseMeadowSoilRole_NotForest(string species)
        {
            Assert.True(WildSpeciesSoilSuccession.TryGetRole(species, out PlantSoilRole role), species);
            Assert.True(role.IsMeadowRole(), species + " → " + role);
            Assert.False(role.IsForestRole(), species + " → " + role);
        }

        [Theory]
        [InlineData(PlantSoilRole.MeadowColonizer)]
        [InlineData(PlantSoilRole.MeadowPerennial)]
        [InlineData(PlantSoilRole.GrassMatrix)]
        [InlineData(PlantSoilRole.GrassColonizer)]
        [InlineData(PlantSoilRole.NitrogenFixer)]
        public void MeadowRoles_NeverClassifiedAsForest(PlantSoilRole role)
        {
            Assert.True(role.IsMeadowRole());
            Assert.False(role.IsForestRole());
        }
    }
}
