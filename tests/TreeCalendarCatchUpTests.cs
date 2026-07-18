using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Testing;
using Xunit;

namespace WildFarming.Tests
{
    public class TreeCalendarCatchUpTests
    {
        [Fact]
        public void Normalize_SnapsWorldStartLagForYoungTree()
        {
            // Defaulted LastGrowthYear=0 after 80 world years must not burn 80 lifespan years.
            int normalized = TreeCalendarCatchUp.NormalizeLastGrowthYear(
                lastGrowthYear: 0,
                gameYear: 80,
                treeAgeYears: 0,
                catchUpLimit: 4);

            Assert.Equal(76, normalized);
        }

        [Fact]
        public void Normalize_AllowsHonestCatchUpBehindLivedAge()
        {
            int normalized = TreeCalendarCatchUp.NormalizeLastGrowthYear(
                lastGrowthYear: 90,
                gameYear: 100,
                treeAgeYears: 50,
                catchUpLimit: 4);

            Assert.Equal(90, normalized);
        }

        [Fact]
        public void Normalize_UnsetLastYear_BecomesOneBehindCurrent()
        {
            int normalized = TreeCalendarCatchUp.NormalizeLastGrowthYear(
                lastGrowthYear: int.MinValue,
                gameYear: 12,
                treeAgeYears: 0,
                catchUpLimit: 4);

            Assert.Equal(11, normalized);
        }

        [Fact]
        public void RejectRestoredAge_YoungSeedlingWithAncientSave()
        {
            Block air = new Block { BlockId = 0, Code = new AssetLocation("game:air") };
            Block log = new Block { BlockId = 1, Code = new AssetLocation("game:log-grown-birch-ud") };
            var acc = new EcologyTestBlockAccessor(new[] { air, log });
            var basePos = new BlockPos(0, 64, 0);
            acc.SetBlock(1, basePos);

            var entry = new ReproducerEntry(
                basePos,
                new AssetLocation("game:sapling-birch-free"),
                new AssetLocation("game:log-grown-birch-ud"),
                new PlantRequirements { Species = "birch", Habitat = EcologyHabitat.TerrestrialTree },
                0)
            {
                TreeAgeYears = 90,
                TreeSenescencePhase = TreeSenescencePhase.None,
            };

            Assert.True(TreeRegistrationAge.ShouldRejectRestoredAge(acc, basePos, "birch", entry));
        }

        [Fact]
        public void RejectRestoredAge_KeepsMidSenescence()
        {
            Block air = new Block { BlockId = 0, Code = new AssetLocation("game:air") };
            Block log = new Block { BlockId = 1, Code = new AssetLocation("game:log-grown-birch-ud") };
            var acc = new EcologyTestBlockAccessor(new[] { air, log });
            var basePos = new BlockPos(0, 64, 0);
            acc.SetBlock(1, basePos);

            var entry = new ReproducerEntry(
                basePos,
                new AssetLocation("game:sapling-birch-free"),
                new AssetLocation("game:log-grown-birch-ud"),
                new PlantRequirements { Species = "birch", Habitat = EcologyHabitat.TerrestrialTree },
                0)
            {
                TreeAgeYears = 90,
                TreeSenescencePhase = TreeSenescencePhase.Declining,
            };

            Assert.False(TreeRegistrationAge.ShouldRejectRestoredAge(acc, basePos, "birch", entry));
        }
    }
}
