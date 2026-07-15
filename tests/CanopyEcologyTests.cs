using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Testing;
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

        [Theory]
        [InlineData("birch")]
        [InlineData("maple")]
        [InlineData("oak")]
        public void TemperateBud_NoFebruaryAndNoLateSummerTail(string wood)
        {
            var profile = WildCanopySeason.Resolve(wood);
            // Start of February / July (interpolation into next month may ramp later)
            Assert.True(profile.BudInterpolated(1f / 12f) < 0.05f);
            Assert.True(profile.BudInterpolated(6f / 12f) < 0.05f);
        }

        [Fact]
        public void ResolvePhase_AutumnWinsEqualActivityTie()
        {
            CanopySeasonPhase phase = CanopyEcology.ResolvePhaseFromActivity(
                defol: 0.1f, bud: 0.1f, out float activity);
            Assert.Equal(CanopySeasonPhase.Autumn, phase);
            Assert.Equal(0.1f, activity);
        }

        [Fact]
        public void ResolvePhase_SpringWhenBudClearlyLeads()
        {
            Assert.Equal(
                CanopySeasonPhase.Spring,
                CanopyEcology.ResolvePhaseFromActivity(0.05f, 0.4f, out _));
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

        [Fact]
        public void DeciduousProfiles_AllowDenseLocalLeafFill()
        {
            var oak = WildCanopySeason.Resolve("oak");
            Assert.True(oak.MaxRegularNearBranchy >= 10);
            Assert.True(oak.LeafCatchUpScale >= 0.9f);
            Assert.True(oak.BranchyCatchUpScale >= 0.8f);
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
        [InlineData(0f, 1f)]
        [InlineData(1f, 1.35f)]
        public void StripActivityScaleForPeriphery_IncreasesTowardEdge(float peripheryNorm, float expected)
        {
            Assert.Equal(expected, CanopyCrownBias.StripActivityScaleForPeriphery(peripheryNorm), 3);
        }

        [Theory]
        [InlineData(0f, 1.35f)]
        [InlineData(1f, 1f)]
        public void BudActivityScaleForPeriphery_IncreasesTowardTrunk(float peripheryNorm, float expected)
        {
            Assert.Equal(expected, CanopyCrownBias.BudActivityScaleForPeriphery(peripheryNorm), 3);
        }

        [Theory]
        [InlineData(9, true)]   // Oct
        [InlineData(10, true)]  // Nov
        [InlineData(8, true)]   // Sep
        [InlineData(11, false)] // Dec → winter force path
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
            Assert.False(CanopySeasonSync.ShouldUsePatchyRegularLeafStripForMonth(CanopySeasonPhase.Spring, 4));
        }

        [Theory]
        [InlineData(5)] // June
        [InlineData(6)] // July
        public void SummerIdle_IsNotWinterBareMonth(int month)
        {
            Assert.False(CanopyFoliageRules.IsWinterBareMonth(month));
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
        [InlineData(0.55f, false)]  // Jul — warm Idle must NOT be bare/strip season
        public void IsBareCrownSeasonForProgress_WinterIdleOnly(float yearProgress, bool expected)
        {
            Assert.Equal(
                expected,
                CanopyFoliageRules.IsBareCrownSeasonForProgress(
                    yearProgress, CanopySeasonPhase.Idle, 1f, "oak"));
        }

        [Fact]
        public void Birch_MidSummer_IsIdleNotAutumn()
        {
            // Warm mid-summer must keep full canopy (was early Jul defol → patchy strip).
            float midJulyStart = 6f / 12f;
            float defol = WildCanopySeason.Resolve("birch").DefoliateInterpolated(midJulyStart);
            float bud = WildCanopySeason.Resolve("birch").BudInterpolated(midJulyStart);
            Assert.True(defol < 0.02f);
            Assert.True(bud < 0.02f);
            Assert.Equal(
                CanopySeasonPhase.Idle,
                CanopyEcology.ResolvePhaseFromActivity(defol, bud, out _));
        }

        [Fact]
        public void IsBareCrownSeasonForProgress_NotDuringAutumnOrSpring()
        {
            Assert.False(CanopyFoliageRules.IsBareCrownSeasonForProgress(
                0.05f, CanopySeasonPhase.Autumn, 1f, "oak"));
            Assert.False(CanopyFoliageRules.IsBareCrownSeasonForProgress(
                0.2f, CanopySeasonPhase.Spring, 1f, "oak"));
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

    public class CanopyOrphanPruneTests
    {
        static Block[] BuildTreeBlocks()
        {
            var air = new Block { BlockId = 0, Code = new AssetLocation("game:air") };
            var log = new Block { BlockId = 1, Code = new AssetLocation("game:log-grown-oak-ud") };
            var branchy = new Block { BlockId = 2, Code = new AssetLocation("game:leavesbranchy-grown-oak-n") };
            var leaf = new Block { BlockId = 3, Code = new AssetLocation("game:leaves-grown-oak-n") };
            var placed = new Block { BlockId = 4, Code = new AssetLocation("game:leaves-placed-oak-n") };
            return new[] { air, log, branchy, leaf, placed };
        }

        [Fact]
        public void IsOrphan_FloatingLeafWithoutTrunk()
        {
            Block[] blocks = BuildTreeBlocks();
            var acc = new EcologyTestBlockAccessor(blocks);
            var pos = new BlockPos(5, 70, 5);
            acc.SetBlock(blocks[3].BlockId, pos);

            Assert.True(CanopyOrphanPrune.IsOrphan(acc, pos, blocks[3]));
        }

        [Fact]
        public void IsOrphan_LeafAdjacentToLog_IsSupported()
        {
            Block[] blocks = BuildTreeBlocks();
            var acc = new EcologyTestBlockAccessor(blocks);
            var logPos = new BlockPos(5, 69, 5);
            var leafPos = new BlockPos(5, 70, 5);
            acc.SetBlock(blocks[1].BlockId, logPos);
            acc.SetBlock(blocks[3].BlockId, leafPos);

            Assert.False(CanopyOrphanPrune.IsOrphan(acc, leafPos, blocks[3]));
        }

        [Fact]
        public void IsOrphan_LeafChainToLog_IsSupported()
        {
            Block[] blocks = BuildTreeBlocks();
            var acc = new EcologyTestBlockAccessor(blocks);
            acc.SetBlock(blocks[1].BlockId, new BlockPos(5, 68, 5));
            acc.SetBlock(blocks[2].BlockId, new BlockPos(5, 69, 5));
            var leafPos = new BlockPos(5, 70, 5);
            acc.SetBlock(blocks[3].BlockId, leafPos);

            Assert.False(CanopyOrphanPrune.IsOrphan(acc, leafPos, blocks[3]));
        }

        [Fact]
        public void IsWildPrunableLeaf_SkipsPlayerPlaced()
        {
            Block[] blocks = BuildTreeBlocks();
            Assert.False(CanopyOrphanPrune.IsWildPrunableLeaf(blocks[4]));
            Assert.True(CanopyOrphanPrune.IsWildPrunableLeaf(blocks[3]));
        }
    }

    public class CanopyBurnGuardTests
    {
        [Theory]
        [InlineData("fire", true)]
        [InlineData("fire-blue", true)]
        [InlineData("leaves-grown-oak-n", false)]
        [InlineData("air", false)]
        public void IsActiveFireBlock_DetectsFireCodes(string path, bool expected)
        {
            var block = new Block
            {
                BlockId = 1,
                Code = new AssetLocation("game", path),
            };
            if (expected) block.BlockMaterial = EnumBlockMaterial.Fire;

            Assert.Equal(expected, CanopyBurnGuard.IsActiveFireBlock(block));
        }

        [Fact]
        public void IsActiveFireBlock_DetectsFireByPathWithoutMaterial()
        {
            var block = new Block
            {
                BlockId = 2,
                Code = new AssetLocation("game", "fire"),
            };
            Assert.True(CanopyBurnGuard.IsActiveFireBlock(block));
        }

        [Fact]
        public void SuppressesFoliagePlacement_WhenFireWithinRadius()
        {
            var air = new Vintagestory.API.Common.Block { BlockId = 0, Code = new Vintagestory.API.Common.AssetLocation("game:air") };
            var fire = new Vintagestory.API.Common.Block
            {
                BlockId = 1,
                Code = new Vintagestory.API.Common.AssetLocation("game:fire"),
                BlockMaterial = EnumBlockMaterial.Fire,
            };
            var acc = new EcologyTestBlockAccessor(new[] { air, fire });

            var center = new BlockPos(10, 64, 10);
            acc.SetBlock(fire.BlockId, new BlockPos(12, 64, 10));

            Assert.True(CanopyBurnGuard.SuppressesFoliagePlacement(acc, center, radius: 3));
            Assert.False(CanopyBurnGuard.SuppressesFoliagePlacement(acc, center, radius: 1));
        }

        [Fact]
        public void SuppressesBudTarget_UsesCandidateRadius()
        {
            var air = new Vintagestory.API.Common.Block { BlockId = 0, Code = new Vintagestory.API.Common.AssetLocation("game:air") };
            var fire = new Vintagestory.API.Common.Block
            {
                BlockId = 1,
                Code = new Vintagestory.API.Common.AssetLocation("game:fire"),
                BlockMaterial = EnumBlockMaterial.Fire,
            };
            var acc = new EcologyTestBlockAccessor(new[] { air, fire });

            var target = new BlockPos(5, 70, 5);
            acc.SetBlock(fire.BlockId, new BlockPos(7, 70, 5));

            Assert.True(CanopyBurnGuard.SuppressesBudTarget(acc, target));
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
