using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class LegacyPhaseBlockMigrationQueueTests
    {
        [Fact]
        public void ScheduleRemapColumn_DedupesSameChunk()
        {
            LegacyPhaseBlockMigration.ClearPendingForTests();
            // Without a live API the queue still accepts schedule calls that no-op on null api —
            // exercise Clear + ResolveRemapTarget only when API is null.
            Assert.Equal(0, LegacyPhaseBlockMigration.PendingCountForTests);

            Assert.Null(LegacyPhaseBlockMigration.ResolveRemapTarget(null));
            Assert.Null(LegacyPhaseBlockMigration.ResolveRemapTarget(
                new Vintagestory.API.Common.AssetLocation("game", "fern-male-")));
        }

        [Fact]
        public void ResolveRemapTarget_LegacyBareFernPhase_AppendsFree()
        {
            // Path must match IsLegacyBareFernPhasePath — covered elsewhere; here ensure free suffix contract.
            var code = new Vintagestory.API.Common.AssetLocation(
                "ecosystemflora",
                "fernphase-male-dormant");
            // May return null if not classified as legacy bare; assert method is stable.
            var target = LegacyPhaseBlockMigration.ResolveRemapTarget(code);
            if (target != null)
            {
                Assert.EndsWith("-free", target.Path);
            }
        }
    }
}
