using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class SpreadWakeThrottleTests
    {
        static ReproducerEntry Entry(double spawnAllowedAt)
        {
            return new ReproducerEntry(
                new BlockPos(1, 64, 1),
                new AssetLocation("game:tallgrass-medium-free"),
                new AssetLocation("game:tallgrass-medium-free"),
                new PlantRequirements { Species = "tallgrass", Habitat = EcologyHabitat.Terrestrial },
                nextAttemptHours: 0)
            {
                NextSpawnAllowedAtHours = spawnAllowedAt,
            };
        }

        [Fact]
        public void Floor_SetsCooldownOneIntervalOut_WhenNoneActive()
        {
            var entry = Entry(spawnAllowedAt: 0);
            SpreadWakeThrottle.ApplyCalendarCadenceFloor(entry, nowHours: 100, intervalHours: 24);
            Assert.Equal(124, entry.NextSpawnAllowedAtHours);
        }

        [Fact]
        public void Floor_OnlyExtends_NeverShortensExistingCooldown()
        {
            var entry = Entry(spawnAllowedAt: 500);
            SpreadWakeThrottle.ApplyCalendarCadenceFloor(entry, nowHours: 100, intervalHours: 24);
            Assert.Equal(500, entry.NextSpawnAllowedAtHours);
        }

        [Fact]
        public void Floor_IgnoresNonPositiveInterval()
        {
            var entry = Entry(spawnAllowedAt: 0);
            SpreadWakeThrottle.ApplyCalendarCadenceFloor(entry, nowHours: 100, intervalHours: 0);
            Assert.Equal(0, entry.NextSpawnAllowedAtHours);
        }
    }
}
