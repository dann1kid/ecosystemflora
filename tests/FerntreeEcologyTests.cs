using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class FerntreeEcologyTests
    {
        [Theory]
        [InlineData("game:ferntree-normal-trunk", true)]
        [InlineData("game:ferntree-normal-foliage", true)]
        [InlineData("game:ferntree-normal-trunk-top-young", true)]
        [InlineData("game:fern-eaglefern", false)]
        [InlineData("game:log-grown-oak-ud", false)]
        public void SpeciesResolution_FerntreeBlocks(string path, bool isFerntree)
        {
            string species = PlantCodeHelper.GetEcologySpecies(new AssetLocation(path));
            if (isFerntree)
            {
                Assert.Equal(WildFerntreeEcology.Species, species);
                Assert.Equal(EcologyHabitat.Ferntree, PlantCodeHelper.GetEcologyHabitat(new AssetLocation(path)));
            }
            else
            {
                Assert.NotEqual(WildFerntreeEcology.Species, species);
            }
        }

        [Fact]
        public void MaturityForAge_YoungMediumOld()
        {
            WildFerntreeEcology.Profile profile = WildFerntreeEcology.Resolve();

            Assert.Equal(FerntreeTopMaturity.Young, FerntreeStructure.MaturityForAge(10, profile));
            Assert.Equal(FerntreeTopMaturity.Medium, FerntreeStructure.MaturityForAge(40, profile));
            Assert.Equal(FerntreeTopMaturity.Old, FerntreeStructure.MaturityForAge(70, profile));
        }

        [Fact]
        public void FerntreeSenescence_IsPastHorizon()
        {
            var cfg = new EcosystemConfig { EnableTreeSenescence = true, EnableFerntreeEcology = true };
            WildFerntreeEcology.Profile profile = WildFerntreeEcology.Resolve();

            Assert.False(FerntreeSenescence.IsPastHorizon(79, profile, cfg));
            Assert.True(FerntreeSenescence.IsPastHorizon(80, profile, cfg));
        }
    }
}
