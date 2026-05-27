using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using WildFarming.Network;
using Xunit;

namespace WildFarming.Tests
{
    internal class StubContext : IEnvironmentalContext
    {
        public BlockPos Position { get; set; } = new BlockPos(0);
        public float Temperature { get; set; } = 15f;
        public float WorldgenRainfall { get; set; } = 0.5f;
        public float LocalForestCover { get; set; } = 0.3f;
        public bool InGreenhouse { get; set; }
        public int GroundFertility { get; set; } = 200;
        public SoilKind GroundSoilKinds { get; set; } = SoilKind.MediumFert;
        public bool GroundSideSolid { get; set; } = true;
        public int SpaceReplaceable { get; set; } = 9999;
        public bool HasClimate { get; set; } = true;
        public bool TouchesFluid { get; set; }
        public bool HasShallowWater { get; set; }
    }

    public class SuitabilityEvaluatorTests
    {
        static PlantRequirements DefaultReq() => new PlantRequirements
        {
            Species = "catmint",
            MinTemp = -5f,
            MaxTemp = 50f,
            MinRain = 0f,
            MaxRain = 1f,
            MinForest = 0f,
            MaxForest = 1f,
        };

        [Fact]
        public void Score_GoodConditions_Positive()
        {
            var ctx = new StubContext();
            float score = SuitabilityEvaluator.Score(DefaultReq(), ctx, harshClimate: true);
            Assert.True(score > 0f, "Score should be positive in good conditions");
        }

        [Fact]
        public void Score_NoClimate_Zero()
        {
            var ctx = new StubContext { HasClimate = false };
            Assert.Equal(0f, SuitabilityEvaluator.Score(DefaultReq(), ctx, harshClimate: true));
        }

        [Fact]
        public void Score_NoSolidGround_Zero()
        {
            var ctx = new StubContext { GroundSideSolid = false };
            Assert.Equal(0f, SuitabilityEvaluator.Score(DefaultReq(), ctx, harshClimate: true));
        }

        [Fact]
        public void Score_TouchesFluid_Zero()
        {
            var ctx = new StubContext { TouchesFluid = true };
            Assert.Equal(0f, SuitabilityEvaluator.Score(DefaultReq(), ctx, harshClimate: true));
        }

        [Fact]
        public void MeetsSurvival_ValidConditions_True()
        {
            var ctx = new StubContext();
            Assert.True(SuitabilityEvaluator.MeetsSurvivalRequirements(DefaultReq(), ctx, harshClimate: true));
        }

        [Fact]
        public void MeetsSurvival_TooCold_HarshClimate_Fails()
        {
            var req = DefaultReq();
            req.MinTemp = 10f;
            var ctx = new StubContext { Temperature = 5f };
            Assert.False(SuitabilityEvaluator.MeetsSurvivalRequirements(req, ctx, harshClimate: true));
        }

        [Fact]
        public void MeetsSurvival_TooCold_NoHarshClimate_Passes()
        {
            var req = DefaultReq();
            req.MinTemp = 10f;
            var ctx = new StubContext { Temperature = 5f };
            Assert.True(SuitabilityEvaluator.MeetsSurvivalRequirements(req, ctx, harshClimate: false));
        }

        [Fact]
        public void MeetsSurvival_InGreenhouse_IgnoresTemp()
        {
            var req = DefaultReq();
            req.MinTemp = 10f;
            var ctx = new StubContext { Temperature = -20f, InGreenhouse = true };
            Assert.True(SuitabilityEvaluator.MeetsSurvivalRequirements(req, ctx, harshClimate: true));
        }

        [Fact]
        public void MeetsSurvival_WrongSoil_Fails()
        {
            var req = DefaultReq();
            req.AllowedSoilKinds = SoilKind.HighFert;
            var ctx = new StubContext { GroundSoilKinds = SoilKind.Sand };
            Assert.False(SuitabilityEvaluator.MeetsSurvivalRequirements(req, ctx, harshClimate: false));
        }

        [Fact]
        public void CanReproduce_Terrestrial_FluidBlocks()
        {
            var req = DefaultReq();
            var ctx = new StubContext { TouchesFluid = true };
            Assert.False(SuitabilityEvaluator.CanReproduce(req, ctx, harshClimate: false));
        }

        [Fact]
        public void CanReproduce_WaterSurface_NeedsShallowWater()
        {
            var req = DefaultReq();
            req.Habitat = EcologyHabitat.WaterSurface;
            var ctx = new StubContext { HasShallowWater = false };
            Assert.False(SuitabilityEvaluator.CanReproduce(req, ctx, harshClimate: false));
        }

        [Fact]
        public void CanReproduce_WaterSurface_WithWater_Passes()
        {
            var req = DefaultReq();
            req.Habitat = EcologyHabitat.WaterSurface;
            var ctx = new StubContext { HasShallowWater = true, SpaceReplaceable = 9999 };
            Assert.True(SuitabilityEvaluator.CanReproduce(req, ctx, harshClimate: false));
        }

        [Fact]
        public void DescribeFailure_NoClimate_ReportsIt()
        {
            var ctx = new StubContext { HasClimate = false };
            string msg = SuitabilityEvaluator.DescribeSurvivalFailure(DefaultReq(), ctx, harshClimate: true);
            Assert.Equal("No climate data.", msg);
        }

        [Fact]
        public void DescribeFailure_AllOk_ReturnsNull()
        {
            var ctx = new StubContext();
            Assert.Null(SuitabilityEvaluator.DescribeSurvivalFailure(DefaultReq(), ctx, harshClimate: false));
            Assert.Null(SuitabilityEvaluator.TryInspectSurvivalFailureLine(DefaultReq(), ctx, harshClimate: false));
        }

        [Fact]
        public void TryInspect_NoClimate_ReturnsNull()
        {
            var ctx = new StubContext { HasClimate = false };
            Assert.Null(SuitabilityEvaluator.TryInspectSurvivalFailureLine(DefaultReq(), ctx, harshClimate: true));
        }

        [Fact]
        public void TryInspectSurvivalLine_WrongSoil_ReturnsSoilTypeLine()
        {
            var req = DefaultReq();
            req.AllowedSoilKinds = SoilKind.HighFert;
            var ctx = new StubContext { GroundSoilKinds = SoilKind.Sand };
            InspectLineLite line = SuitabilityEvaluator.TryInspectSurvivalFailureLine(req, ctx, harshClimate: true);
            Assert.NotNull(line);
            Assert.Equal("ecosystemflora:inspect-survival-fail-soil-type", line.Key);
        }
    }
}
