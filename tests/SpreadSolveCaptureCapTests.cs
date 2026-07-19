using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class SpreadSolveCaptureCapTests
    {
        [Fact]
        public void ResolveCaptureCellCap_ClampsToMax()
        {
            var request = new SpreadSolveRequest { MaxSpawns = 8 };
            Assert.Equal(
                SpreadSolveBatchBuilder.MaxCapturedCellsPerSolve,
                SpreadSolveBatchBuilder.ResolveCaptureCellCap(request));
        }

        [Fact]
        public void ResolveCaptureCellCap_ScalesWithMaxSpawns()
        {
            var request = new SpreadSolveRequest { MaxSpawns = 1 };
            Assert.Equal(8, SpreadSolveBatchBuilder.ResolveCaptureCellCap(request));
        }

        [Fact]
        public void MainUnloadDefaults_LowerRegistrationFeed()
        {
            var cfg = new EcosystemConfig();
            Assert.Equal(340, cfg.MaxRegistrationSnapshotCellsPerTick);
            Assert.Equal(9, cfg.RegistrationBudgetMs);
            Assert.Equal(2, cfg.FoliageChunkSyncBudgetMs);
        }
    }
}
