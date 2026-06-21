using WildFarming.Ecosystem;
using WildFarming.Network;
using Xunit;

namespace WildFarming.Tests
{
    public class CatmintEcologyTests
    {
        [Fact]
        public void CanReproduce_OpenMeadow_NoMinForestBlock()
        {
            Assert.True(WildFlowerClimate.TryGet("catmint", out WildFlowerClimate.EcologyEntry entry));
            Assert.Equal(0f, entry.MinForest);

            var req = new PlantRequirements
            {
                Species = "catmint",
                MinRain = entry.MinRain,
                MaxRain = entry.MaxRain,
                MinForest = entry.MinForest,
                MaxForest = entry.MaxForest,
            };
            var ctx = new StubEnvironmentalContext { LocalForestCover = 0f, HasClimate = true };

            Assert.True(SuitabilityEvaluator.CanReproduce(req, ctx, harshClimate: true));
        }

        [Fact]
        public void SoilRole_IsMeadowNotForest()
        {
            Assert.True(WildSpeciesSoilSuccession.TryGetRole("catmint", out PlantSoilRole role));
            Assert.Equal(PlantSoilRole.MeadowPerennial, role);
            Assert.False(role.IsForestRole());
        }

        sealed class StubEnvironmentalContext : IEnvironmentalContext
        {
            public Vintagestory.API.MathTools.BlockPos Position { get; set; }
            public float Temperature { get; set; } = 15f;
            public float WorldgenRainfall { get; set; } = 0.5f;
            public float LocalForestCover { get; set; }
            public bool InGreenhouse { get; set; }
            public int GroundFertility { get; set; } = 150;
            public SoilKind GroundSoilKinds { get; set; } = SoilKind.MediumFert;
            public bool GroundSideSolid { get; set; } = true;
            public int SpaceReplaceable { get; set; } = 9999;
            public bool HasClimate { get; set; } = true;
            public bool TouchesFluid { get; set; }
            public bool HasShallowWater { get; set; }
        }
    }
}
