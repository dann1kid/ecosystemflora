using Vintagestory.API.Common;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class FernPhenologyBlocksTests
    {
        [Theory]
        [InlineData("eaglefern", FernPhenologyPhase.Dormant, "fernphase-eaglefern-dormant-free")]
        [InlineData("eaglefern", FernPhenologyPhase.Dieback, "fernphase-eaglefern-dieback-free")]
        [InlineData("eaglefern", FernPhenologyPhase.Sporulating, "fernphase-eaglefern-sporulating-free")]
        public void CodeForPhase_BuildsExpectedPath(string species, FernPhenologyPhase phase, string expectedPath)
        {
            AssetLocation code = FernPhenologyBlocks.CodeForPhase(species, phase);
            Assert.NotNull(code);
            Assert.Equal("ecosystemflora", code.Domain);
            Assert.Equal(expectedPath, code.Path);
        }

        [Theory]
        [InlineData("ecosystemflora:fernphase-eaglefern-dormant-free", "eaglefern", FernPhenologyPhase.Dormant)]
        [InlineData("ecosystemflora:fernphase-cinnamonfern-dieback-snow", "cinnamonfern", FernPhenologyPhase.Dieback)]
        [InlineData("ecosystemflora:fernphase-hartstongue-sporulating-snow", "hartstongue", FernPhenologyPhase.Sporulating)]
        public void PhaseFromCode_RecognizesPhaseBlocks(string codePath, string species, FernPhenologyPhase phase)
        {
            var code = new AssetLocation(codePath);
            Assert.Equal(species, FernPhenologyBlocks.SpeciesFromPhaseCode(code));
            Assert.Equal(phase, FernPhenologyBlocks.PhaseFromCode(code));
        }

        [Fact]
        public void CodeForPhase_SporulatingNeverUsesVanillaMatureBlock()
        {
            AssetLocation code = FernPhenologyBlocks.CodeForPhase("eaglefern", FernPhenologyPhase.Sporulating);
            Assert.Equal("ecosystemflora", code.Domain);
            Assert.StartsWith("fernphase-", code.Path);
        }

        [Fact]
        public void CodeForPhase_PreservesSnowFromReference()
        {
            var snowBlock = new Block { Code = new AssetLocation("ecosystemflora:fernphase-eaglefern-dormant-snow") };
            AssetLocation code = FernPhenologyBlocks.CodeForPhase(
                "eaglefern",
                FernPhenologyPhase.Dieback,
                snowBlock);
            Assert.Equal("fernphase-eaglefern-dieback-snow", code.Path);
        }

        [Fact]
        public void CodeForPhase_SnowFlag_SetsSnowSuffix()
        {
            AssetLocation snow = FernPhenologyBlocks.CodeForPhase("eaglefern", FernPhenologyPhase.Sporulating, snow: true);
            Assert.Equal("fernphase-eaglefern-sporulating-snow", snow.Path);
        }
    }
}
