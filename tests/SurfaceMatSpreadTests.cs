using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class SurfaceMatSpreadTests
    {
        [Theory]
        [InlineData(1, 0, true)]
        [InlineData(0, 1, true)]
        [InlineData(1, 1, true)]
        [InlineData(-1, -1, true)]
        [InlineData(2, 0, false)]
        [InlineData(1, 2, false)]
        public void IsMatStep_UsesChebyshevDistanceOne(int dx, int dz, bool expected)
        {
            Assert.Equal(expected, SurfaceMatSpread.IsMatStep(dx, dz));
        }

        [Fact]
        public void ApplyTo_WaterLily_SetsSurfaceMatModeAndRadius()
        {
            EcosystemConfig.Loaded = new EcosystemConfig { UseSurfaceMatSpreadForLilies = true };

            var req = new PlantRequirements
            {
                Habitat = EcologyHabitat.WaterSurface,
                Species = "waterlily",
            };

            SurfaceMatSpread.ApplyTo(req);

            Assert.True(req.UsesSurfaceMatSpread);
            Assert.Equal(1, req.SpreadRadius);
            Assert.Equal(0.05f, req.SeedDispersalChance);
            Assert.Equal(4, req.SeedDispersalRadius);
        }

        [Fact]
        public void ApplyTo_ConfigOff_LeavesIndependentMode()
        {
            EcosystemConfig.Loaded = new EcosystemConfig { UseSurfaceMatSpreadForLilies = false };

            var req = new PlantRequirements
            {
                Habitat = EcologyHabitat.WaterSurface,
                Species = "waterlily",
            };

            SurfaceMatSpread.ApplyTo(req);

            Assert.False(req.UsesSurfaceMatSpread);
            Assert.Equal(0, req.SpreadRadius);
        }

        [Fact]
        public void WildAquaticEcology_WaterLily_HasMatSpreadProfile()
        {
            Assert.True(WildAquaticEcology.TryGet("waterlily", out WildAquaticEcology.Profile lily));

            Assert.Equal(1.2f, lily.SpreadRate);
            Assert.Equal(0.05f, lily.SeedDispersalChance);
            Assert.Equal(4, lily.SeedDispersalRadius);
        }
    }
}
