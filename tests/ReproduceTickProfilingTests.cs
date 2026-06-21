using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class ReproduceTickProfilingTests
    {
        static ReproducerEntry Entry(
            int x,
            double nextAttempt,
            double spawnAllowed = 0,
            ulong wakeGen = 1,
            ulong lastProcessed = 0)
        {
            var entry = new ReproducerEntry(
                new BlockPos(x, 64, 10),
                new AssetLocation("game:flower-catmint-free"),
                new AssetLocation("game:flower-catmint-free"),
                new PlantRequirements { Species = "catmint" },
                nextAttempt);
            entry.NextSpawnAllowedAtHours = spawnAllowed;
            entry.WakeGeneration = wakeGen;
            entry.LastProcessedWakeGeneration = lastProcessed;
            return entry;
        }

        [Fact]
        public void ClassifyDueReason_CalendarDue_ReturnsCalendar()
        {
            var entry = Entry(x: 1, nextAttempt: 10);
            Assert.Equal(
                ReproducerRegistry.SpreadDueReason.Calendar,
                ReproducerRegistry.ClassifyDueReason(entry, now: 12, eventDriven: true));
        }

        [Fact]
        public void ClassifyDueReason_WakeOnly_ReturnsWake()
        {
            var entry = Entry(x: 2, nextAttempt: 100, wakeGen: 5, lastProcessed: 2);
            Assert.Equal(
                ReproducerRegistry.SpreadDueReason.Wake,
                ReproducerRegistry.ClassifyDueReason(entry, now: 10, eventDriven: true));
        }

        [Fact]
        public void ClassifyDueReason_CalendarPreemptsWake()
        {
            var entry = Entry(x: 3, nextAttempt: 10, wakeGen: 5, lastProcessed: 0);
            Assert.Equal(
                ReproducerRegistry.SpreadDueReason.Calendar,
                ReproducerRegistry.ClassifyDueReason(entry, now: 12, eventDriven: true));
        }

        [Fact]
        public void ClassifyDueReason_BeforeSpawnCooldown_ReturnsNone()
        {
            var entry = Entry(x: 4, nextAttempt: 0, spawnAllowed: 20, wakeGen: 5);
            Assert.Equal(
                ReproducerRegistry.SpreadDueReason.None,
                ReproducerRegistry.ClassifyDueReason(entry, now: 10, eventDriven: true));
        }

        [Fact]
        public void TryProcessDueEntry_CountsWakeAndCalendar()
        {
            var registry = new ReproducerRegistry();
            registry.Add(Entry(x: 10, nextAttempt: 100, wakeGen: 2, lastProcessed: 0));
            registry.Add(Entry(x: 11, nextAttempt: 5, wakeGen: 1, lastProcessed: 0));

            int processed = registry.ProcessDue(
                now: 10,
                maxAttempts: 4,
                _ => 24,
                _ => true,
                eventDriven: true);

            Assert.Equal(2, processed);
            Assert.Equal(1, registry.LastWakeDrivenAttempts);
            Assert.Equal(1, registry.LastCalendarDrivenAttempts);
        }
    }
}
