using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class SurfacePlacementTests
    {
        [Fact]
        public void ReproduceMinReplaceable_Is5000()
        {
            Assert.Equal(5000, SuitabilityEvaluator.ReproduceMinReplaceable);
        }

        [Theory]
        [InlineData(6000, true)]
        [InlineData(5000, true)]
        [InlineData(4999, false)]
        public void TallgrassReplaceable_MeetsStickSurfaceThreshold(int replaceable, bool canReplace)
        {
            Assert.Equal(
                canReplace,
                PlantVacancyRules.MeetsMinReplaceable(replaceable, SuitabilityEvaluator.ReproduceMinReplaceable));
        }
    }
}
