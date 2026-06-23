using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class PendingFlowerMaturationTests
    {
        [Fact]
        public void TryGetHoursUntilMature_ReturnsRemainingTime()
        {
            var queue = new PendingFlowerMaturation();
            var pos = new BlockPos(4, 64, 9);
            queue.Add(
                pos,
                new AssetLocation("game:flower-catmint-free"),
                "catmint",
                matureAtHours: 100);

            Assert.True(queue.TryGetHoursUntilMature(pos, nowHours: 88, out double hoursLeft));
            Assert.Equal(12, hoursLeft);
        }
    }
}
