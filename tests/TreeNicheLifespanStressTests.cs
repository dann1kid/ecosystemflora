using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class TreeNicheLifespanStressTests
    {
        static readonly EcosystemConfig Cfg = new EcosystemConfig
        {
            EnableTreeAging = true,
            EnableTreeSenescence = true,
            EnableTreeNicheLifespanStress = true,
            EnableTreeSeralSuccession = true,
            ApplyWorldgenRainForest = true,
            TreeNicheLifespanStressGraceYears = 8,
            TreeNicheLifespanStressHardDebtPerYear = 2,
            TreeNicheLifespanStressSoftDebtPerYear = 1,
            TreeNicheLifespanStressRecoveryPerYear = 1,
            TreeNicheLifespanStressMaxDebtFraction = 0.5f,
            TreeNicheLifespanStressSeralSoftThreshold = 0.35f,
        };

        static PlantRequirements OakReq() => new PlantRequirements
        {
            Species = "oak",
            Habitat = EcologyHabitat.TerrestrialTree,
            MinTemp = -2f,
            MaxTemp = 30f,
            MinRain = 0.35f,
            MaxRain = 0.78f,
            MinForest = 0f,
            MaxForest = 0.75f,
        };

        sealed class TreeNicheStubContext : IEnvironmentalContext
        {
            public BlockPos Position { get; set; } = new BlockPos(0, 64, 0, 0);
            public float Temperature { get; set; }
            public float WorldgenRainfall { get; set; }
            public float LocalForestCover { get; set; }
            public bool InGreenhouse { get; set; }
            public int GroundFertility { get; set; } = 50;
            public SoilKind GroundSoilKinds { get; set; } = SoilKind.MediumFert;
            public bool GroundSideSolid { get; set; } = true;
            public int SpaceReplaceable { get; set; } = 6000;
            public bool HasClimate { get; set; } = true;
            public bool TouchesFluid { get; set; }
            public bool HasShallowWater { get; set; }
        }

        static TreeNicheStubContext InNicheCtx() => new TreeNicheStubContext
        {
            HasClimate = true,
            Temperature = 12f,
            WorldgenRainfall = 0.5f,
            LocalForestCover = 0.35f,
        };

        [Fact]
        public void EffectiveHorizon_SubtractsDebt_AndCaps()
        {
            Assert.Equal(120, TreeNicheLifespanStress.EffectiveHorizon(120, 0, Cfg));
            Assert.Equal(100, TreeNicheLifespanStress.EffectiveHorizon(120, 20, Cfg));
            Assert.Equal(60, TreeNicheLifespanStress.EffectiveHorizon(120, 999, Cfg));
            Assert.Equal(5, TreeNicheLifespanStress.EffectiveHorizon(10, 999, Cfg));
            Assert.Equal(1, TreeNicheLifespanStress.EffectiveHorizon(2, 999, Cfg));
        }

        [Fact]
        public void EffectiveHorizon_IgnoresDebt_WhenFeatureOff()
        {
            var cfg = new EcosystemConfig
            {
                EnableTreeAging = true,
                EnableTreeSenescence = true,
                EnableTreeNicheLifespanStress = false,
                TreeNicheLifespanStressMaxDebtFraction = 0.5f,
            };
            var entry = new ReproducerEntry(
                new BlockPos(0, 64, 0, 0),
                new AssetLocation("game:sapling-oak-free"),
                new AssetLocation("game:log-grown-oak-ud"),
                OakReq(),
                0)
            {
                TreeAgeYears = 50,
                TreeLifespanDebtYears = 40,
            };

            var profile = WildTreeGrowthProfiles.Resolve("oak");
            Assert.Equal(120, TreeNicheLifespanStress.EffectiveHorizon(profile, entry, cfg));
            Assert.False(TreeSenescence.IsPastHorizon(entry, profile, cfg));
        }

        [Fact]
        public void IsPastHorizon_UsesDebt()
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");
            var entry = new ReproducerEntry(
                new BlockPos(0, 64, 0, 0),
                new AssetLocation("game:sapling-oak-free"),
                new AssetLocation("game:log-grown-oak-ud"),
                OakReq(),
                0)
            {
                TreeAgeYears = 100,
                TreeLifespanDebtYears = 30,
            };

            Assert.True(TreeSenescence.IsPastHorizon(entry, profile, Cfg));
            Assert.False(TreeSenescence.IsPastHorizon(100, profile, Cfg, lifespanDebtYears: 0));
        }

        [Fact]
        public void ClassifyYear_HardMiss_OnCold()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                EcosystemConfig.Loaded = Cfg;
                TreeNicheStubContext ctx = InNicheCtx();
                ctx.Temperature = -20f;

                Assert.Equal(
                    TreeNicheLifespanStress.YearOutcome.HardMiss,
                    TreeNicheLifespanStress.ClassifyYear(OakReq(), ctx, "oak", Cfg));
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }

        [Fact]
        public void ClassifyYear_HardMiss_OnForestTooDense_ForPioneer()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                EcosystemConfig.Loaded = Cfg;
                var birch = new PlantRequirements
                {
                    Species = "birch",
                    Habitat = EcologyHabitat.TerrestrialTree,
                    MinTemp = -12f,
                    MaxTemp = 28f,
                    MinRain = 0.35f,
                    MaxRain = 1f,
                    MinForest = 0f,
                    MaxForest = 0.38f,
                };
                TreeNicheStubContext ctx = InNicheCtx();
                ctx.LocalForestCover = 0.9f;

                Assert.Equal(
                    TreeNicheLifespanStress.YearOutcome.HardMiss,
                    TreeNicheLifespanStress.ClassifyYear(birch, ctx, "birch", Cfg));
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }

        [Fact]
        public void ClassifyYear_InNiche_ForOakMidForest()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                EcosystemConfig.Loaded = Cfg;
                Assert.Equal(
                    TreeNicheLifespanStress.YearOutcome.InNiche,
                    TreeNicheLifespanStress.ClassifyYear(OakReq(), InNicheCtx(), "oak", Cfg));
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }

        [Fact]
        public void ApplyOutcome_AccruesAndRecoversDebt()
        {
            var entry = new ReproducerEntry(
                new BlockPos(0, 64, 0, 0),
                new AssetLocation("game:sapling-oak-free"),
                new AssetLocation("game:log-grown-oak-ud"),
                OakReq(),
                0)
            {
                TreeAgeYears = 20,
                TreeLifespanDebtYears = 0,
            };

            TreeNicheLifespanStress.ApplyOutcome(
                entry,
                TreeNicheLifespanStress.YearOutcome.HardMiss,
                120,
                Cfg);
            Assert.Equal(2, entry.TreeLifespanDebtYears);

            TreeNicheLifespanStress.ApplyOutcome(
                entry,
                TreeNicheLifespanStress.YearOutcome.InNiche,
                120,
                Cfg);
            Assert.Equal(1, entry.TreeLifespanDebtYears);
        }

        [Fact]
        public void ShouldEvaluate_False_DuringGrace()
        {
            var entry = new ReproducerEntry(
                new BlockPos(0, 64, 0, 0),
                new AssetLocation("game:sapling-oak-free"),
                new AssetLocation("game:log-grown-oak-ud"),
                OakReq(),
                0)
            {
                TreeAgeYears = 3,
            };

            Assert.False(TreeNicheLifespanStress.ShouldEvaluate(entry, Cfg));
        }

        [Fact]
        public void ApplyYear_Skipped_DuringGrace_WithoutApi()
        {
            var entry = new ReproducerEntry(
                new BlockPos(0, 64, 0, 0),
                new AssetLocation("game:sapling-oak-free"),
                new AssetLocation("game:log-grown-oak-ud"),
                OakReq(),
                0)
            {
                TreeAgeYears = 3,
                TreeLifespanDebtYears = 0,
            };

            Assert.Equal(
                TreeNicheLifespanStress.YearOutcome.Skipped,
                TreeNicheLifespanStress.ApplyYear(api: null, entry, "oak", 120, Cfg));
            Assert.Equal(0, entry.TreeLifespanDebtYears);
        }

        [Fact]
        public void CalendarStore_PersistsDebt()
        {
            var store = new TreeCalendarAgeStore();
            var pos = new BlockPos(10, 70, -3, 0);
            var entry = new ReproducerEntry(
                pos,
                new AssetLocation("game:sapling-oak-free"),
                new AssetLocation("game:log-grown-oak-ud"),
                OakReq(),
                0)
            {
                TreeAgeYears = 44,
                LastTreeGrowthYear = 90,
                TreeLifespanDebtYears = 12,
                TreeSenescencePhase = TreeSenescencePhase.None,
            };

            store.Capture(entry, "oak");
            byte[] bytes = store.SerializeForTests();

            var loaded = new TreeCalendarAgeStore();
            loaded.LoadFromBytes(bytes);
            Assert.True(loaded.TryGetRecord(pos, out TreeCalendarAgeRecord rec));
            Assert.Equal(12, rec.LifespanDebtYears);

            var restored = new ReproducerEntry(
                pos,
                new AssetLocation("game:sapling-oak-free"),
                new AssetLocation("game:log-grown-oak-ud"),
                OakReq(),
                0);
            Assert.True(loaded.TryRestore(restored, pos, "oak"));
            Assert.Equal(12, restored.TreeLifespanDebtYears);
            Assert.Equal(44, restored.TreeAgeYears);
        }
    }
}
