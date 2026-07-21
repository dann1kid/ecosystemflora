using System.Diagnostics;
using System.Threading;
using Vintagestory.API.Common;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class BudgetDeadlineTests
    {
        [Fact]
        public void FromBudgetMs_ZeroOrNegative_MeansUnlimited()
        {
            Assert.Equal(0, BudgetDeadline.FromBudgetMs(0));
            Assert.Equal(0, BudgetDeadline.FromBudgetMs(-1));
            Assert.False(BudgetDeadline.IsExpired(0));
        }

        [Fact]
        public void FromBudgetMs_Positive_ExpiresAfterBudget()
        {
            long start = Stopwatch.GetTimestamp();
            long deadline = BudgetDeadline.FromBudgetMs(1, start);
            Assert.True(deadline > start);
            Assert.False(BudgetDeadline.IsExpired(deadline));

            Thread.Sleep(5);
            Assert.True(BudgetDeadline.IsExpired(deadline));
        }
    }

    public class ReproducePrepBudgetTests
    {
        [Fact]
        public void ResolveReproducePrepBudgetMs_UsesTickBudgetMs()
        {
            var cfg = new EcosystemConfig { TickBudgetMs = 7, SpreadBudgetMs = 3 };
            Assert.Equal(7, cfg.ResolveReproducePrepBudgetMs());
            Assert.Equal(3, cfg.ResolveSpreadBudgetMs());
        }

        [Fact]
        public void ResolveSpreadBudgetMs_IndependentOfPrep()
        {
            var cfg = new EcosystemConfig { TickBudgetMs = 5, SpreadBudgetMs = 0 };
            Assert.Equal(5, cfg.ResolveReproducePrepBudgetMs());
            Assert.Equal(5, cfg.ResolveSpreadBudgetMs());
        }
    }

    public class FlowerDrygrassDropsSyncTests
    {
        [Fact]
        public void SyncBlock_PatchThenDisable_RestoresOriginalDrops()
        {
            FlowerDrygrassDrops.ClearOriginalsForTests();

            var original = new[]
            {
                new BlockDropItemStack { Code = new AssetLocation("game", "flower-cornflower") },
            };
            var drygrass = new[]
            {
                new BlockDropItemStack { Code = new AssetLocation("game", "drygrass") },
            };

            var block = new Block
            {
                BlockId = 901,
                Code = new AssetLocation("game", "flower-cornflower-free"),
                Drops = original,
            };

            FlowerDrygrassDrops.SyncBlockForTests(block, enabled: true, drygrass);
            Assert.Same(drygrass, block.Drops);

            FlowerDrygrassDrops.SyncBlockForTests(block, enabled: false, drygrassDrops: null);
            Assert.Same(original, block.Drops);

            FlowerDrygrassDrops.ClearOriginalsForTests();
        }
    }

    public class MyceliumHarmonyGateTests
    {
        [Fact]
        public void PassesModDisplacementGate_RespectsWorldConfig_EvenWhenEcologyWasOffAtBoot()
        {
            // Runtime gate is authoritative; Harmony apply no longer requires ecology on at StartPre.
            EcosystemConfig.Loaded = new EcosystemConfig
            {
                EnableMyceliumEcology = false,
                EnableMyceliumCapDisplacement = true,
            };

            var grass = new Block
            {
                BlockId = 42,
                Code = new AssetLocation("game", "tallgrass-veryshort-free"),
            };
            Assert.False(MyceliumCapPlacement.PassesModDisplacementGate(grass));

            EcosystemConfig.Loaded.EnableMyceliumEcology = true;
            Assert.True(MyceliumCapPlacement.PassesModDisplacementGate(grass));
        }
    }
}
