using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class FlowerPhenologyBlocksTests
    {
        [Theory]
        [InlineData("catmint", FlowerPhenologyPhase.Vegetative, "flowerphase-catmint-vegetative-free")]
        [InlineData("cornflower", FlowerPhenologyPhase.Dormant, "flowerphase-cornflower-dormant-free")]
        [InlineData("wilddaisy", FlowerPhenologyPhase.Dieback, "flowerphase-wilddaisy-dieback-free")]
        public void CodeForPhase_BuildsExpectedPath(string species, FlowerPhenologyPhase phase, string expectedPath)
        {
            AssetLocation code = FlowerPhenologyBlocks.CodeForPhase(species, phase);
            Assert.NotNull(code);
            Assert.Equal("ecosystemflora", code.Domain);
            Assert.Equal(expectedPath, code.Path);
        }

        [Fact]
        public void CodeForPhase_BloomReturnsNull()
        {
            Assert.Null(FlowerPhenologyBlocks.CodeForPhase("catmint", FlowerPhenologyPhase.Bloom));
        }

        [Theory]
        [InlineData("ecosystemflora:flowerphase-catmint-vegetative-free", "catmint", FlowerPhenologyPhase.Vegetative)]
        [InlineData("ecosystemflora:flowerphase-lupine-dormant-free", "lupine", FlowerPhenologyPhase.Dormant)]
        [InlineData("ecosystemflora:flowerphase-heather-dieback-free", "heather", FlowerPhenologyPhase.Dieback)]
        public void TryParse_RecognizesPhaseBlocks(string codePath, string species, FlowerPhenologyPhase phase)
        {
            var code = new AssetLocation(codePath);
            Assert.Equal(species, FlowerPhenologyBlocks.SpeciesFromPhaseCode(code));
            Assert.True(FlowerPhenologyBlocks.TryGetPhase(code, out FlowerPhenologyPhase parsed));
            Assert.Equal(phase, parsed);
        }
    }
}
