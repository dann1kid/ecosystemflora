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
    }
}
