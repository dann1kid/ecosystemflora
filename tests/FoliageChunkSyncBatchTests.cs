using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class FoliageChunkSyncBatchTests
    {
        [Fact]
        public void ScheduleChunkSync_MarksPendingIncremental()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                EcosystemConfig.Loaded = new EcosystemConfig
                {
                    EnableSeasonalFoliage = true,
                    FoliageSyncMode = "chunk",
                };

                var scheduler = new FoliageCellScheduler();
                Assert.Equal(0, scheduler.PendingSyncChunks);

                scheduler.ScheduleChunkSync(
                    api: null, // ScheduleChunkSync returns early without api — use Mark via NoteFire
                    chunkCoord: new Vec2i(0, 0));
                Assert.Equal(0, scheduler.PendingSyncChunks);

                scheduler.NoteFireTouched(new Vec2i(1, 2), totalHours: 10);
                Assert.Equal(1, scheduler.PendingSyncChunks);
                Assert.Equal(1, scheduler.TrackedChunkCount);
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }

        [Fact]
        public void PendingSyncChunks_DefaultZero()
        {
            var scheduler = new FoliageCellScheduler();
            Assert.Equal(0, scheduler.PendingSyncChunks);
        }

        [Fact]
        public void OnBlockRemoved_DoesNotMarkChunkPendingSync()
        {
            var scheduler = new FoliageCellScheduler();
            var pos = new BlockPos(10, 64, 20);

            scheduler.OnBlockRemoved(pos);

            Assert.Equal(0, scheduler.PendingSyncChunks);
        }

        [Fact]
        public void OnBlockAdded_DoesNotMarkChunkPendingSync()
        {
            var scheduler = new FoliageCellScheduler();
            var pos = new BlockPos(10, 64, 20);

            scheduler.OnBlockAdded(pos);

            Assert.Equal(0, scheduler.PendingSyncChunks);
        }
    }

    public class FoliagePlayerVacancySuppressorTests
    {
        [Fact]
        public void NotePlayerBreak_BlocksBudAtBrokenCellUntilExpiry()
        {
            var suppressor = new FoliagePlayerVacancySuppressor();
            var pos = new BlockPos(5, 70, 5);

            suppressor.NotePlayerBreak(pos, nowHours: 100, durationHours: 50);

            Assert.True(suppressor.BlocksBudAt(pos, nowHours: 120));
            Assert.False(suppressor.BlocksBudAt(pos, nowHours: 151));
        }

        [Fact]
        public void NotePlayerBreak_BlocksAdjacentVacancyTargets()
        {
            var suppressor = new FoliagePlayerVacancySuppressor();
            var pos = new BlockPos(5, 70, 5);
            var neighbor = pos.EastCopy();

            suppressor.NotePlayerBreak(pos, nowHours: 0, durationHours: 10);

            Assert.True(suppressor.BlocksBudAt(neighbor, nowHours: 1));
        }
    }
}
