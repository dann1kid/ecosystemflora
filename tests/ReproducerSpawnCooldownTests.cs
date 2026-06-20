using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class ReproducerSpawnCooldownTests
    {
        static ReproducerEntry MakeEntry(double nextAttemptHours, double spawnAllowedAt = 0)
        {
            var entry = new ReproducerEntry(
                new BlockPos(1, 64, 1),
                new AssetLocation("game:flower-cowparsley-free"),
                new AssetLocation("game:flower-cowparsley-free"),
                new PlantRequirements { Species = "cowparsley" },
                nextAttemptHours)
            {
                NextSpawnAllowedAtHours = spawnAllowedAt,
            };
            return entry;
        }

        [Fact]
        public void IsEntryDue_BlocksWakeBeforeSpawnCooldown()
        {
            var entry = MakeEntry(nextAttemptHours: 0, spawnAllowedAt: 100);
            Assert.False(ReproducerRegistry.IsEntryDue(entry, now: 10, eventDriven: true));
        }

        [Fact]
        public void IsEntryDue_AllowsWakeAfterSpawnCooldownEvenBeforeCalendar()
        {
            var entry = MakeEntry(nextAttemptHours: 1000, spawnAllowedAt: 0);
            entry.WakeGeneration = 2;
            entry.LastProcessedWakeGeneration = 0;
            Assert.True(ReproducerRegistry.IsEntryDue(entry, now: 10, eventDriven: true));
        }

        [Fact]
        public void WakeAround_PullsRetryForwardWhenCooldownElapsed()
        {
            var registry = new ReproducerRegistry();
            var entry = MakeEntry(nextAttemptHours: 100, spawnAllowedAt: 0);
            registry.Add(entry);

            registry.WakeAround(new BlockPos(1, 64, 1), radiusBlocks: 4, now: 10, new EcosystemConfig
            {
                EnableEventDrivenSpread = true,
                EventWakeRetryHours = 6,
            });

            Assert.Equal(16, entry.NextAttemptHours);
        }

        [Fact]
        public void WakeAround_DoesNotPullRetryDuringSpawnCooldown()
        {
            var registry = new ReproducerRegistry();
            var entry = MakeEntry(nextAttemptHours: 100, spawnAllowedAt: 50);
            registry.Add(entry);

            registry.WakeAround(new BlockPos(1, 64, 1), radiusBlocks: 4, now: 10, new EcosystemConfig
            {
                EnableEventDrivenSpread = true,
                EventWakeRetryHours = 6,
            });

            Assert.Equal(100, entry.NextAttemptHours);
        }
    }
}
