using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class ArborealSpreadProtectionTests
    {
        static Block Block(string code) => new Block { Code = new AssetLocation(code) };

        [Theory]
        [InlineData("game:log-grown-oak-ud")]
        [InlineData("game:ferntree-normal-trunk")]
        public void IsArborealHostBlock_recognizes_trunk_blocks(string code)
        {
            Assert.True(PlantCodeHelper.IsArborealHostBlock(Block(code)));
        }

        [Fact]
        public void CanDisplaceFromSolveCell_rejects_log_grown_trunk()
        {
            var challenger = new PlantRequirements
            {
                Species = "cornflower",
                Habitat = EcologyHabitat.Terrestrial,
            };
            var trunk = Block("game:log-grown-oak-ud");
            SpreadSolveCell cell = default;

            bool canDisplace = CellCompetition.CanDisplaceFromSolveCell(
                challenger,
                trunk,
                new BlockPos(0, 64, 0),
                in cell,
                harshClimate: false,
                seasonSpreadMult: 1f,
                out float challengerScore,
                out _);

            Assert.False(canDisplace);
            Assert.Equal(0f, challengerScore);
        }

        [Fact]
        public void IsEcologySpreadParent_includes_trunk_but_arboreal_guard_blocks_meadow_use()
        {
            var trunk = Block("game:log-grown-birch-ud");
            Assert.True(PlantCodeHelper.IsEcologySpreadParent(trunk));
            Assert.True(PlantCodeHelper.IsArborealHostBlock(trunk));
        }
    }
}
