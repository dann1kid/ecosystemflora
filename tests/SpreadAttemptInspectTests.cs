using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class SpreadAttemptInspectTests
    {
        [Fact]
        public void Record_StoresLastAttemptOutcome()
        {
            var entry = new ReproducerEntry(
                new BlockPos(1, 64, 1),
                null,
                null,
                new PlantRequirements { Species = "coopersreed" },
                nextAttemptHours: 0);

            SpreadAttemptInspect.Record(null, entry, MatSpreadCollectMode.SeedDispersal, placed: true);

            Assert.True(entry.LastSpreadPlaced);
            Assert.Equal(MatSpreadCollectMode.SeedDispersal, entry.LastSpreadCollectMode);
            Assert.Null(entry.LastSpreadFailureReason);
        }

        [Fact]
        public void Record_StoresFailureReasonWhenNotPlaced()
        {
            var entry = new ReproducerEntry(
                new BlockPos(1, 64, 1),
                null,
                null,
                new PlantRequirements { Species = "catmint" },
                nextAttemptHours: 0);

            SpreadAttemptInspect.Record(
                null,
                entry,
                MatSpreadCollectMode.NotApplicable,
                placed: false,
                failureReason: "No qualifying cells");

            Assert.False(entry.LastSpreadPlaced);
            Assert.Equal("No qualifying cells", entry.LastSpreadFailureReason);
        }

        [Fact]
        public void ShouldDeferCooldown_OnlyForBackgroundOrEnqueue()
        {
            Assert.True(FlowerSpreadCooldownTiming.ShouldDeferCooldownToPlacement(backgroundQueued: true, 0));
            Assert.True(FlowerSpreadCooldownTiming.ShouldDeferCooldownToPlacement(false, 2));
            Assert.False(FlowerSpreadCooldownTiming.ShouldDeferCooldownToPlacement(false, 0));
        }
    }
}
