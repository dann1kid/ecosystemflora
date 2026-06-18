using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class FoliageChunkSyncBatchTests
    {
        [Fact]
        public void PendingSyncChunks_DefaultZero()
        {
            var scheduler = new FoliageCellScheduler();
            Assert.Equal(0, scheduler.PendingSyncChunks);
        }
    }
}
