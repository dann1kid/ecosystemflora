using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class WildCanopySeasonTests
    {
        [Theory]
        [InlineData("birch")]
        [InlineData("oak")]
        [InlineData("acacia")]
        public void TryGet_DeciduousWood_ReturnsProfile(string wood)
        {
            Assert.True(WildCanopySeason.TryGet(wood, out WildCanopySeason.Profile profile));
            Assert.True(profile.DefoliateInterpolated(0.75f) > 0f);
        }

        [Fact]
        public void TryGet_Conifer_ReturnsFalse()
        {
            Assert.False(WildCanopySeason.TryGet("pine", out _));
            Assert.False(WildCanopySeason.TryGet("larch", out _));
        }

        [Fact]
        public void Birch_HasEarlierBudThanOak()
        {
            var birch = WildCanopySeason.Resolve("birch");
            var oak = WildCanopySeason.Resolve("oak");
            float springBirch = birch.BudInterpolated(0.2f);
            float springOak = oak.BudInterpolated(0.2f);
            Assert.True(springBirch > springOak);
        }

        [Fact]
        public void SpringCatchUpProfiles_DifferBySpeciesRealism()
        {
            var birch = WildCanopySeason.Resolve("birch");
            var oak = WildCanopySeason.Resolve("oak");
            var kapok = WildCanopySeason.Resolve("kapok");

            Assert.True(birch.BranchyCatchUpScale > kapok.BranchyCatchUpScale);
            Assert.True(birch.LeafCatchUpScale > kapok.LeafCatchUpScale);
            Assert.True(oak.MaxBranchyNearLog > kapok.MaxBranchyNearLog);
            Assert.True(oak.MaxBranchyNearLog > birch.MaxBranchyNearLog);
        }
    }

    public class CanopyBlockHelperTests
    {
        [Theory]
        [InlineData("leaves-grown-oak-n", "oak")]
        [InlineData("leaves-grown-oak", "oak")]
        [InlineData("leaves-grown3-oak", "oak")]
        [InlineData("leaves-grown7-birch", "birch")]
        [InlineData("leaves-oak-grown-n", "oak")]
        [InlineData("leavesbranchy-grown-birch-e", "birch")]
        [InlineData("leavesbranchy-grown2-birch", "birch")]
        [InlineData("leavesbranchy-birch-grown-e", "birch")]
        [InlineData("leaves-placed-maple-s", "maple")]
        [InlineData("leaves-placed4-maple", "maple")]
        public void GetWoodFromFoliageBlock_ParsesPath(string path, string wood)
        {
            var block = new Vintagestory.API.Common.Block
            {
                Code = new Vintagestory.API.Common.AssetLocation("game", path),
            };
            Assert.Equal(wood, CanopyBlockHelper.GetWoodFromFoliageBlock(block));
        }

        [Fact]
        public void IsRegularLeaf_ExcludesBranchy()
        {
            Assert.True(CanopyBlockHelper.IsRegularLeafPath("leaves-grown-oak-n"));
            Assert.False(CanopyBlockHelper.IsRegularLeafPath("leavesbranchy-grown-oak-n"));
        }

        [Fact]
        public void DeterministicNoise_IsStable()
        {
            var pos = new BlockPos(10, 64, 20);
            float a = CanopyBlockHelper.DeterministicNoise(pos, "oak", 3);
            float b = CanopyBlockHelper.DeterministicNoise(pos, "oak", 3);
            Assert.Equal(a, b);
        }

        [Theory]
        [InlineData(0, 0, 1, "s")]
        [InlineData(0, 0, -1, "n")]
        [InlineData(1, 0, 0, "e")]
        [InlineData(-1, 0, 0, "w")]
        [InlineData(0, 1, 0, "u")]
        [InlineData(0, -1, 0, "d")]
        public void FaceTowardAnchor_MapsNeighborDelta(int dx, int dy, int dz, string face)
        {
            var target = new BlockPos(10, 64, 10);
            var anchor = new BlockPos(10 + dx, 64 + dy, 10 + dz);
            Assert.Equal(face, CanopyBlockHelper.FaceTowardAnchor(target, anchor));
        }
    }

    public class FoliageSyncModeTests
    {
        [Theory]
        [InlineData("chunk", FoliageSyncMode.Chunk)]
        [InlineData("hybrid", FoliageSyncMode.Hybrid)]
        [InlineData("random", FoliageSyncMode.Random)]
        [InlineData("legacy", FoliageSyncMode.Random)]
        public void Resolve_ParsesMode(string raw, FoliageSyncMode expected)
        {
            var cfg = new EcosystemConfig { FoliageSyncMode = raw };
            Assert.Equal(expected, FoliageSyncModeHelper.Resolve(cfg));
        }

        [Fact]
        public void ChunkMode_DisablesRandomTickByDefault()
        {
            var cfg = new EcosystemConfig { FoliageSyncMode = "chunk", MaxFoliageCellsTickedPerTick = 0 };
            Assert.True(FoliageSyncModeHelper.UsesChunkSync(cfg));
            Assert.False(FoliageSyncModeHelper.UsesRandomTick(cfg));
        }

        [Fact]
        public void HybridMode_EnablesRandomTickWhenConfigured()
        {
            var cfg = new EcosystemConfig { FoliageSyncMode = "hybrid", MaxFoliageCellsTickedPerTick = 16 };
            Assert.True(FoliageSyncModeHelper.UsesChunkSync(cfg));
            Assert.True(FoliageSyncModeHelper.UsesRandomTick(cfg));
        }
    }

    public class CanopySeasonSyncTests
    {
        [Theory]
        [InlineData(9, true)]
        [InlineData(10, true)]
        [InlineData(11, false)]
        [InlineData(0, false)]
        [InlineData(1, false)]
        public void ShouldUsePatchyRegularLeafStrip_AutumnPhase_ByMonth(int month, bool expectPatchy)
        {
            Assert.Equal(
                expectPatchy,
                CanopySeasonSync.ShouldUsePatchyRegularLeafStripForMonth(CanopySeasonPhase.Autumn, month));
        }

        [Fact]
        public void ShouldUsePatchyRegularLeafStrip_IdlePhase_NeverPatchy()
        {
            Assert.False(CanopySeasonSync.ShouldUsePatchyRegularLeafStripForMonth(CanopySeasonPhase.Idle, 5));
        }
    }

    public class CanopyFoliageRulesTests
    {
        [Theory]
        [InlineData("log-grown-oak-ud", FoliageCellKind.LogGrown)]
        [InlineData("leavesbranchy-grown-birch-e", FoliageCellKind.BranchyLeaf)]
        [InlineData("leaves-grown-maple-n", FoliageCellKind.RegularLeaf)]
        [InlineData("leaves-oak-grown-n", FoliageCellKind.RegularLeaf)]
        [InlineData("leaves-grown2-oak", FoliageCellKind.RegularLeaf)]
        [InlineData("leavesbranchy-grown3-birch", FoliageCellKind.BranchyLeaf)]
        [InlineData("log-grown-pine-ud", FoliageCellKind.None)]
        public void Classify_BlockPaths(string path, FoliageCellKind expected)
        {
            var block = new Vintagestory.API.Common.Block
            {
                Code = new Vintagestory.API.Common.AssetLocation("game", path),
            };
            Assert.Equal(expected, CanopyFoliageRules.Classify(block));
        }
        [Fact]
        public void IsBranchyLeaf_LeavesGrownPath_IsRegularEvenWithBranchyClass()
        {
            var block = new Vintagestory.API.Common.Block
            {
                Code = new Vintagestory.API.Common.AssetLocation("game", "leaves-grown2-oak"),
                Class = "BlockLeavesBranchy",
            };
            Assert.False(CanopyBlockHelper.IsBranchyLeaf(block));
            Assert.Equal(FoliageCellKind.RegularLeaf, CanopyFoliageRules.Classify(block));
        }

        [Fact]
        public void ShouldCatchUpStripRegularLeaf_ReturnsFalseWithoutApi()
        {
            Assert.False(CanopyFoliageRules.ShouldCatchUpStripRegularLeaf(null, new BlockPos(0), "oak", out _));
        }

        [Fact]
        public void TryCatchUpStripOnScan_ReturnsFalseWithoutApi()
        {
            var block = new Vintagestory.API.Common.Block
            {
                Code = new Vintagestory.API.Common.AssetLocation("game", "leaves-grown2-oak"),
            };
            Assert.False(CanopyFoliageRules.TryCatchUpStripOnScan(null, null, new BlockPos(0), block));
        }

        [Theory]
        [InlineData(0.05f, true)]   // Jan dormant
        [InlineData(0.15f, false)]  // Mar — bud rising
        public void IsBareCrownSeasonForProgress_WinterIdleOnly(float yearProgress, bool expected)
        {
            Assert.Equal(
                expected,
                CanopyFoliageRules.IsBareCrownSeasonForProgress(yearProgress, CanopySeasonPhase.Idle, 1f));
        }

        [Fact]
        public void IsBareCrownSeasonForProgress_NotDuringAutumnOrSpring()
        {
            Assert.False(CanopyFoliageRules.IsBareCrownSeasonForProgress(0.05f, CanopySeasonPhase.Autumn, 1f));
            Assert.False(CanopyFoliageRules.IsBareCrownSeasonForProgress(0.2f, CanopySeasonPhase.Spring, 1f));
        }

        [Fact]
        public void ShouldCatchUpBud_ReturnsFalseWithoutApi()
        {
            Assert.False(CanopyFoliageRules.ShouldCatchUpBud(
                null, new BlockPos(0), "oak", FoliageCellKind.LogGrown, out _));
        }

        [Fact]
        public void TryCatchUpBudOnScan_ReturnsFalseWithoutApi()
        {
            var block = new Vintagestory.API.Common.Block
            {
                Code = new Vintagestory.API.Common.AssetLocation("game", "log-grown-oak-ud"),
            };
            Assert.False(CanopyFoliageRules.TryCatchUpBudOnScan(
                null, null, new BlockPos(0), block, index: null));
        }
    }

    public class FoliageColumnScannerTests
    {
        [Theory]
        [InlineData("log-grown-oak-ud", true)]
        [InlineData("log-grown-aged-ud", true)]
        [InlineData("leaves-grown2-oak", true)]
        [InlineData("leavesbranchy-grown3-oak", true)]
        [InlineData("wildbeehive-inlog-oak", true)]
        public void ContinueColumnScan_TreeBlocksPassThrough(string path, bool expected)
        {
            var block = new Vintagestory.API.Common.Block
            {
                Code = new Vintagestory.API.Common.AssetLocation("game", path),
            };
            Assert.Equal(expected, FoliageColumnScanner.ContinueColumnScan(block));
        }
    }

    public class FoliageCellIndexTests
    {
        [Fact]
        public void AddRemove_RoundTrips()
        {
            var index = new FoliageCellIndex();
            var pos = new BlockPos(10, 64, 20);
            index.Add(pos);
            Assert.True(index.Contains(pos));
            index.Remove(pos);
            Assert.False(index.Contains(pos));
        }
    }
}
