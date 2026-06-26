using Vintagestory.API.Common;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class WildSoilBlockMapperTests
    {
        static WildSoilComposition MeadowComposition => new WildSoilComposition
        {
            FertilityPoints = 180,
            Moisture = 45f,
        };

        static SoilImpact SpreadImpact => new SoilImpact
        {
            MoistureDelta = 6f,
            FertilityTierDelta = 0.10f,
        };

        [Theory]
        [InlineData("wilddaisy", PlantSoilRole.MeadowColonizer)]
        [InlineData("catmint", PlantSoilRole.MeadowPerennial)]
        [InlineData("bluebell", PlantSoilRole.ForestUnderstory)]
        [InlineData("eaglefern", PlantSoilRole.ForestUnderstory)]
        [InlineData("hartstongue", PlantSoilRole.WetlandHerb)]
        public void PlantSpread_OnMeadowSoil_NeverCreatesForestFloor(string species, PlantSoilRole role)
        {
            Assert.True(WildSpeciesSoilSuccession.TryGetImpact(
                species, SoilSuccessionEvent.Spread, out SoilImpact impact));

            Assert.True(WildSoilBlockMapper.TryResolveGroundCode(
                "soil-medium-normal",
                MeadowComposition,
                role,
                SoilSuccessionEvent.Spread,
                impact,
                out AssetLocation code));

            Assert.StartsWith("game:soil-", code.ToString());
            Assert.DoesNotContain("forestfloor", code.Path);
        }

        [Fact]
        public void ForestUnderstory_OnExistingForestFloor_MaintainsLitterLayer()
        {
            Assert.True(WildSoilBlockMapper.TryResolveGroundCode(
                "forestfloor-3",
                MeadowComposition,
                PlantSoilRole.ForestUnderstory,
                SoilSuccessionEvent.Spread,
                SpreadImpact,
                out AssetLocation code));

            Assert.StartsWith("forestfloor-", code.Path);
        }

        [Fact]
        public void WetlandHerb_OnWetSoil_NeverCreatesPeat()
        {
            var wet = new WildSoilComposition { FertilityPoints = 120, Moisture = 90f };
            Assert.True(WildSpeciesSoilSuccession.TryGetImpact(
                "hartstongue", SoilSuccessionEvent.Spread, out SoilImpact impact));

            Assert.True(WildSoilBlockMapper.TryResolveGroundCode(
                "soil-low-sparse",
                wet,
                PlantSoilRole.WetlandHerb,
                SoilSuccessionEvent.Spread,
                impact,
                out AssetLocation code));

            Assert.StartsWith("game:soil-", code.ToString());
            Assert.DoesNotContain("peat", code.Path);
        }

        [Fact]
        public void Hartstongue_HasWetlandHerbRole_NotForestUnderstory()
        {
            Assert.True(WildSpeciesSoilSuccession.TryGetRole("hartstongue", out PlantSoilRole role));
            Assert.Equal(PlantSoilRole.WetlandHerb, role);
            Assert.False(role.IsForestRole());
        }

        [Fact]
        public void Hartstongue_IsNotTreeSymbiont()
        {
            Assert.False(FloraSymbiosis.TryGetRule("hartstongue", out _));
        }
    }
}
