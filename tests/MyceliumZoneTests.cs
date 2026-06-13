using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class MyceliumZoneTests
    {
        const int Radius = 7;

        [Theory]
        [InlineData(PlantSoilRole.MeadowColonizer, 0, MyceliumNiche.ForestAnyTree, 0.35f)]
        [InlineData(PlantSoilRole.MeadowColonizer, 7, MyceliumNiche.ForestAnyTree, 1f)]
        [InlineData(PlantSoilRole.MeadowColonizer, 0, MyceliumNiche.MeadowOpen, 1f)]
        [InlineData(PlantSoilRole.MeadowColonizer, 3, MyceliumNiche.MeadowOpen, 1f)]
        [InlineData(PlantSoilRole.GrassMatrix, 3, MyceliumNiche.ForestAnyTree, 0.62857144f)]
        [InlineData(PlantSoilRole.ForestUnderstory, 0, MyceliumNiche.ForestAnyTree, 1.22f)]
        [InlineData(PlantSoilRole.ForestUnderstory, 7, MyceliumNiche.ForestAnyTree, 1f)]
        [InlineData(PlantSoilRole.ForestUnderstory, 0, MyceliumNiche.MeadowOpen, 1f)]
        [InlineData(PlantSoilRole.ForestEdge, 2, MyceliumNiche.ForestDeciduous, 1.1571429f)]
        [InlineData(PlantSoilRole.WetlandHerb, 1, MyceliumNiche.ForestAnyTree, 1f)]
        [InlineData(PlantSoilRole.SoilDepleter, 2, MyceliumNiche.ForestAnyTree, 1f)]
        public void SpreadMultiplierForRole_MatchesTaper(
            PlantSoilRole role,
            int distance,
            MyceliumNiche nearestNiche,
            float expected)
        {
            float mult = MyceliumZone.SpreadMultiplierForRole(
                role,
                distance,
                Radius,
                nearestNiche,
                meadowPenaltyAtZero: 0.35f,
                forestBonusAtZero: 1.22f);

            Assert.Equal(expected, mult, precision: 3);
        }

        [Fact]
        public void SpreadMultiplierForRole_OutOfRange_ReturnsNeutral()
        {
            float mult = MyceliumZone.SpreadMultiplierForRole(
                PlantSoilRole.MeadowColonizer,
                distance: 8,
                zoneRadius: Radius,
                MyceliumNiche.ForestAnyTree,
                meadowPenaltyAtZero: 0.35f,
                forestBonusAtZero: 1.22f);

            Assert.Equal(1f, mult);
        }

        [Fact]
        public void VanillaGrowRange_MatchesVanillaMycelium()
        {
            Assert.Equal(7, MyceliumZone.VanillaGrowRange);
        }
    }
}
