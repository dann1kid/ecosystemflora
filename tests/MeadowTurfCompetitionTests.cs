using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class MeadowTurfCompetitionTests
    {
        [Fact]
        public void Flower_DisplacingTallgrass_GetsSpreadBonus()
        {
            float adjusted = MeadowTurfCompetition.AdjustChallengerSpreadScore(
                1f, "wilddaisy", "tallgrass");

            Assert.Equal(1f * MeadowTurfCompetition.FlowerVsTallgrassSpreadBonus, adjusted, 4);
        }

        [Fact]
        public void Tallgrass_DisplacingFlower_GetsSpreadPenalty()
        {
            float adjusted = MeadowTurfCompetition.AdjustChallengerSpreadScore(
                1f, "tallgrass", "cornflower");

            Assert.Equal(1f * MeadowTurfCompetition.TallgrassVsFlowerSpreadPenalty, adjusted, 4);
        }

        [Fact]
        public void GrassColonizer_DoesNotGetFlowerTurfBonus()
        {
            float adjusted = MeadowTurfCompetition.AdjustChallengerSpreadScore(
                1f, EcologyGrassColonizerSpecies.Redtopgrass, "tallgrass");

            Assert.Equal(1f, adjusted);
        }

        [Fact]
        public void Tallgrass_SpreadRate_BelowGrassColonizer()
        {
            Assert.True(WildTallgrassEcology.TryGet("tallgrass", out var matrix));
            Assert.True(WildGrassColonizerEcology.TryGet(EcologyGrassColonizerSpecies.Redtopgrass, out var colonizer));
            Assert.True(colonizer.SpreadRate > matrix.SpreadRate);
            Assert.True(matrix.SpreadRate < 1.5f);
        }
    }
}
