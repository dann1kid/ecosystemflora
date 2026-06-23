using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class FlowerSpreadCooldownTimingTests
    {
        [Theory]
        [InlineData(true, 0, true)]
        [InlineData(false, 0, false)]
        [InlineData(false, 2, true)]
        public void ShouldDeferCooldownToPlacement(bool backgroundQueued, int placementCount, bool expected)
        {
            Assert.Equal(
                expected,
                FlowerSpreadCooldownTiming.ShouldDeferCooldownToPlacement(backgroundQueued, placementCount));
        }
    }
}
