using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Testing;
using WildFarming.Tests.Harness;
using Xunit;

namespace WildFarming.Tests
{
    public class CanopyTreeAgeBoostTests
    {
        [Theory]
        [InlineData(0, 1f)]
        [InlineData(30, 1.25f)]
        [InlineData(60, 1.5f)]
        [InlineData(120, 1.5f)]
        public void SpringBranchyBudMultiplier_ScalesWithAge(int ageYears, float expected)
        {
            EcosystemConfig.Loaded = new EcosystemConfig
            {
                EnableSpringBranchyAgeBoost = true,
                SpringBranchyAgeBoostYearsToMax = 60f,
                SpringBranchyAgeBoostMax = 1.5f,
            };

            float mult = CanopyTreeAgeBoost.SpringBranchyBudMultiplierForAge(ageYears);
            Assert.Equal(expected, mult, precision: 3);
        }
    }

    public class WildVineHelperTests
    {
        [Theory]
        [InlineData("wildvine-end-north", false, true, "north")]
        [InlineData("wildvine-section-east", false, false, "east")]
        [InlineData("wildvine-tropical-end-south", true, true, "south")]
        [InlineData("wildvine-tropical-section-west", true, false, "west")]
        public void TryParse_RecognisesVanillaPaths(string path, bool tropical, bool isEnd, string facingCode)
        {
            var block = new Block { Code = new AssetLocation("game", path) };

            Assert.True(WildVineHelper.TryParse(block, out WildVineInfo info));
            Assert.Equal(tropical, info.Tropical);
            Assert.Equal(isEnd, info.IsEnd);
            Assert.Equal(facingCode, info.Facing.Code);
        }

        [Fact]
        public void HostPosAndVinePosForHost_AreInverse()
        {
            var host = new BlockPos(10, 64, 10);
            BlockFacing facing = BlockFacing.NORTH;

            BlockPos vine = WildVineHelper.VinePosForHost(host, facing);
            BlockPos back = WildVineHelper.HostPos(vine, facing);

            Assert.Equal(host, back);
        }

        [Theory]
        [InlineData("wildvine", true)]
        [InlineData("wildvine-tropical", true)]
        [InlineData("oak", false)]
        public void EcologySpecies_RecognisesVines(string species, bool expected)
        {
            Assert.Equal(expected, WildVineEcology.IsSpecies(species));
        }

        [Theory]
        [InlineData(EnumBlockMaterial.Stone, 0, null, true)]
        [InlineData(EnumBlockMaterial.Wood, 0, null, true)]
        [InlineData(EnumBlockMaterial.Soil, 0, "north", true)]
        [InlineData(EnumBlockMaterial.Soil, 0, "up", false)]
        [InlineData(EnumBlockMaterial.Plant, 0, null, false)]
        [InlineData(EnumBlockMaterial.Stone, 6000, null, false)]
        public void IsStructuralWallHost_RejectsPlantsLooseBlocksAndHorizontalSoil(
            EnumBlockMaterial material,
            int replaceable,
            string facingCode,
            bool expected)
        {
            var block = new Block
            {
                BlockId = 1,
                BlockMaterial = material,
                Replaceable = replaceable,
            };
            BlockFacing facing = string.IsNullOrEmpty(facingCode) ? null : BlockFacing.FromCode(facingCode);

            Assert.Equal(expected, WildVineHelper.IsStructuralWallHost(block, facing));
        }

        [Theory]
        [InlineData("north", "east")]
        [InlineData("south", "east")]
        [InlineData("east", "north")]
        [InlineData("west", "north")]
        public void WallTangent_IsPerpendicularToFacing(string facingCode, string tangentCode)
        {
            BlockFacing facing = BlockFacing.FromCode(facingCode);
            BlockFacing tangent = WildVineHelper.WallTangent(facing);

            Assert.Equal(tangentCode, tangent.Code);
            Assert.NotEqual(facing.Axis, tangent.Axis);
        }

        [Fact]
        public void TryResolveSpreadTip_FromSection_FindsLowestEnd()
        {
            Block[] blocks =
            {
                new Block { BlockId = 0, Code = new AssetLocation("game", "air") },
                new Block { BlockId = 1, Code = new AssetLocation("game", "wildvine-section-north") },
                new Block { BlockId = 2, Code = new AssetLocation("game", "wildvine-end-north") },
            };
            var acc = new EcologyTestBlockAccessor(blocks);
            acc.SetBlock(2, new BlockPos(0, 60, 0));
            acc.SetBlock(1, new BlockPos(0, 61, 0));
            acc.SetBlock(1, new BlockPos(0, 62, 0));

            Assert.True(WildVineHelper.TryResolveSpreadTip(acc, new BlockPos(0, 62, 0), out BlockPos tip));
            Assert.Equal(60, tip.Y);
        }

        [Fact]
        public void DedupeColumnEnds_KeepsSurfaceAndMobileTip()
        {
            Block[] blocks =
            {
                new Block { BlockId = 0, Code = new AssetLocation("game", "air") },
                new Block { BlockId = 1, Code = new AssetLocation("game", "wildvine-section-north") },
                new Block { BlockId = 2, Code = new AssetLocation("game", "wildvine-end-north") },
            };
            var acc = new EcologyTestBlockAccessor(blocks);
            acc.SetBlock(2, new BlockPos(0, 60, 0));
            acc.SetBlock(2, new BlockPos(0, 61, 0));
            acc.SetBlock(2, new BlockPos(0, 62, 0));

            IWorldAccessor world = CreateVineResolveWorld(blocks);
            WildVineHelper.DedupeColumnEnds(acc, world, new BlockPos(0, 61, 0));

            var info = new WildVineInfo(false, false, BlockFacing.NORTH);
            Assert.True(WildVineHelper.IsEndBlock(acc.GetBlock(new BlockPos(0, 62, 0))));
            Assert.True(WildVineHelper.IsSectionBlock(acc.GetBlock(new BlockPos(0, 61, 0)), info));
            Assert.True(WildVineHelper.IsEndBlock(acc.GetBlock(new BlockPos(0, 60, 0))));
        }

        [Fact]
        public void DedupeColumnEnds_CollapsesMiddleEndToSection()
        {
            Block[] blocks =
            {
                new Block { BlockId = 0, Code = new AssetLocation("game", "air") },
                new Block { BlockId = 1, Code = new AssetLocation("game", "wildvine-section-north") },
                new Block { BlockId = 2, Code = new AssetLocation("game", "wildvine-end-north") },
            };
            var acc = new EcologyTestBlockAccessor(blocks);
            acc.SetBlock(2, new BlockPos(0, 60, 0));
            acc.SetBlock(2, new BlockPos(0, 61, 0));

            IWorldAccessor world = CreateVineResolveWorld(blocks);
            WildVineHelper.DedupeColumnEnds(acc, world, new BlockPos(0, 61, 0));

            var info = new WildVineInfo(false, false, BlockFacing.NORTH);
            Assert.True(WildVineHelper.IsEndBlock(acc.GetBlock(new BlockPos(0, 61, 0))));
            Assert.True(WildVineHelper.IsEndBlock(acc.GetBlock(new BlockPos(0, 60, 0))));
        }

        [Fact]
        public void IsSurfaceAnchorEnd_FalseForSolitaryWallTip()
        {
            Block air = new Block { BlockId = 0, Code = new AssetLocation("game", "air") };
            Block stone = new Block { BlockId = 3, BlockMaterial = EnumBlockMaterial.Stone, Replaceable = 0 };
            Block[] blocks =
            {
                air,
                new Block { BlockId = 2, Code = new AssetLocation("game", "wildvine-end-north") },
                stone,
            };
            var acc = new EcologyTestBlockAccessor(blocks);
            var info = new WildVineInfo(false, false, BlockFacing.NORTH);
            var tip = new BlockPos(0, 64, 0);
            acc.SetBlock(3, new BlockPos(0, 64, 1));
            acc.SetBlock(2, tip);

            IWorldAccessor world = CreateVineResolveWorld(blocks);

            Assert.False(WildVineHelper.IsSurfaceAnchorEnd(acc, world, tip, info));
        }

        [Fact]
        public void IsSurfaceAnchorEnd_TrueOnlyForTopEndWithColumnBelow()
        {
            Block[] blocks =
            {
                new Block { BlockId = 0, Code = new AssetLocation("game", "air") },
                new Block { BlockId = 1, Code = new AssetLocation("game", "wildvine-section-north") },
                new Block { BlockId = 2, Code = new AssetLocation("game", "wildvine-end-north") },
            };
            var acc = new EcologyTestBlockAccessor(blocks);
            var info = new WildVineInfo(false, false, BlockFacing.NORTH);
            acc.SetBlock(2, new BlockPos(0, 62, 0));
            acc.SetBlock(1, new BlockPos(0, 61, 0));
            acc.SetBlock(2, new BlockPos(0, 60, 0));

            IWorldAccessor world = CreateVineResolveWorld(blocks);

            Assert.True(WildVineHelper.IsSurfaceAnchorEnd(acc, world, new BlockPos(0, 62, 0), info));
            Assert.False(WildVineHelper.IsSurfaceAnchorEnd(acc, world, new BlockPos(0, 60, 0), info));
        }

        [Fact]
        public void ExtendDown_ConvertsFormerTipToSection()
        {
            Block air = new Block { BlockId = 0, Code = new AssetLocation("game", "air") };
            Block stone = new Block { BlockId = 3, BlockMaterial = EnumBlockMaterial.Stone, Replaceable = 0 };
            Block section = new Block { BlockId = 1, Code = new AssetLocation("game", "wildvine-section-north") };
            Block end = new Block { BlockId = 2, Code = new AssetLocation("game", "wildvine-end-north") };
            Block[] blocks = { air, section, end, stone };
            var acc = new EcologyTestBlockAccessor(blocks);
            var info = new WildVineInfo(false, false, BlockFacing.NORTH);
            var tip = new BlockPos(0, 64, 0);
            acc.SetBlock(3, new BlockPos(0, 64, 1));
            acc.SetBlock(2, tip);

            IWorldAccessor world = CreateVineResolveWorld(blocks);

            Assert.False(WildVineHelper.IsSurfaceAnchorEnd(acc, world, tip, info));
            acc.SetBlock(section.BlockId, tip);
            acc.SetBlock(end.BlockId, tip.DownCopy());

            Assert.True(WildVineHelper.IsSectionBlock(acc.GetBlock(tip), info));
            Assert.True(WildVineHelper.IsEndBlock(acc.GetBlock(tip.DownCopy())));
        }

        [Fact]
        public void TouchesVineNetworkForSpread_IncludesCornerDiagonal()
        {
            Block[] blocks =
            {
                new Block { BlockId = 0, Code = new AssetLocation("game", "air") },
                new Block { BlockId = 1, Code = new AssetLocation("game", "wildvine-section-north") },
                new Block { BlockId = 2, Code = new AssetLocation("game", "wildvine-section-east") },
            };
            var acc = new EcologyTestBlockAccessor(blocks);
            var host = new BlockPos(10, 64, 10);
            BlockPos northVine = WildVineHelper.VinePosForHost(host, BlockFacing.NORTH);
            BlockPos eastVine = WildVineHelper.VinePosForHost(host, BlockFacing.EAST);
            acc.SetBlock(1, northVine);
            acc.SetBlock(2, eastVine);

            Assert.False(WildVineHelper.TouchesVineNetwork(acc, eastVine, tropical: false));
            Assert.True(WildVineHelper.TouchesVineNetworkForSpread(acc, eastVine, tropical: false));
        }

        [Fact]
        public void IsAdjacentWrapCell_AllowsCornerWrapDistance()
        {
            var section = new BlockPos(10, 64, 9);
            var eastWrap = new BlockPos(11, 65, 10);

            Assert.True(WildVineHelper.IsAdjacentWrapCell(eastWrap, section));
        }

        [Fact]
        public void TryResolveAdjacentFacePlacement_UsesSharedCornerHost()
        {
            Block air = new Block { BlockId = 0, Code = new AssetLocation("game", "air") };
            Block stone = new Block { BlockId = 1, BlockMaterial = EnumBlockMaterial.Stone, Replaceable = 0 };
            var acc = new EcologyTestBlockAccessor(new[] { air, stone });

            var host = new BlockPos(10, 64, 10);
            acc.SetBlock(1, host);
            BlockPos northSection = WildVineHelper.VinePosForHost(host, BlockFacing.NORTH);
            var info = new WildVineInfo(false, false, BlockFacing.NORTH);
            var scratch = new BlockPos(0);

            Assert.True(WildVineHelper.TryResolveAdjacentFacePlacement(
                acc,
                northSection,
                info,
                BlockFacing.EAST,
                alongWallStep: 0,
                dy: 0,
                scratch,
                out BlockPos eastVine));

            Assert.Equal(WildVineHelper.VinePosForHost(host, BlockFacing.EAST), eastVine);
        }

        [Fact]
        public void TryResolveAdjacentFacePlacement_InsideCornerWalksRimToVacantCell()
        {
            Block air = new Block { BlockId = 0, Code = new AssetLocation("game", "air") };
            Block stone = new Block { BlockId = 1, BlockMaterial = EnumBlockMaterial.Stone, Replaceable = 0 };
            var acc = new EcologyTestBlockAccessor(new[] { air, stone });

            BlockPos[] stones =
            {
                new BlockPos(0, 64, 0),
                new BlockPos(1, 64, 0),
                new BlockPos(2, 64, 0),
                new BlockPos(0, 64, 1),
                new BlockPos(2, 64, 1),
                new BlockPos(0, 64, 2),
                new BlockPos(1, 64, 2),
                new BlockPos(2, 64, 2),
            };
            foreach (BlockPos stonePos in stones)
            {
                acc.SetBlock(1, stonePos);
            }

            BlockPos northSection = WildVineHelper.VinePosForHost(new BlockPos(1, 64, 0), BlockFacing.NORTH);
            var info = new WildVineInfo(false, false, BlockFacing.NORTH);
            var scratch = new BlockPos(0);

            Assert.True(WildVineHelper.TryResolveAdjacentFacePlacement(
                acc,
                northSection,
                info,
                BlockFacing.EAST,
                alongWallStep: 0,
                dy: 0,
                scratch,
                out BlockPos eastVine));

            Assert.Equal(new BlockPos(3, 64, 0), eastVine);
            Assert.Equal(new BlockPos(2, 64, 0), scratch);
        }

        [Fact]
        public void TryResolveAdjacentFacePlacement_TwoBlockInsideCornerStepsOntoNeighborFace()
        {
            Block air = new Block { BlockId = 0, Code = new AssetLocation("game", "air") };
            Block stone = new Block { BlockId = 1, BlockMaterial = EnumBlockMaterial.Stone, Replaceable = 0 };
            var acc = new EcologyTestBlockAccessor(new[] { air, stone });

            acc.SetBlock(1, new BlockPos(0, 64, 1));
            acc.SetBlock(1, new BlockPos(0, 64, 0));

            BlockPos eastSection = WildVineHelper.VinePosForHost(new BlockPos(0, 64, 0), BlockFacing.EAST);
            var info = new WildVineInfo(false, false, BlockFacing.EAST);
            var scratch = new BlockPos(0);

            Assert.True(WildVineHelper.TryResolveAdjacentFacePlacement(
                acc,
                eastSection,
                info,
                BlockFacing.NORTH,
                alongWallStep: 0,
                dy: 0,
                scratch,
                out BlockPos northVine,
                out int rimScore));

            Assert.Equal(new BlockPos(0, 64, -1), northVine);
            Assert.Equal(new BlockPos(0, 64, 0), scratch);
            Assert.Equal(0, rimScore);
        }

        [Fact]
        public void TouchesVineNetworkForSpread_AllowsRimWalkAroundInsideCorner()
        {
            Block air = new Block { BlockId = 0, Code = new AssetLocation("game", "air") };
            Block stone = new Block { BlockId = 1, BlockMaterial = EnumBlockMaterial.Stone, Replaceable = 0 };
            Block section = new Block { BlockId = 2, Code = new AssetLocation("game", "wildvine-section-north") };
            var acc = new EcologyTestBlockAccessor(new[] { air, stone, section });

            BlockPos[] stones =
            {
                new BlockPos(0, 64, 0),
                new BlockPos(1, 64, 0),
                new BlockPos(2, 64, 0),
                new BlockPos(0, 64, 1),
                new BlockPos(2, 64, 1),
                new BlockPos(0, 64, 2),
                new BlockPos(1, 64, 2),
                new BlockPos(2, 64, 2),
            };
            foreach (BlockPos stonePos in stones)
            {
                acc.SetBlock(1, stonePos);
            }

            BlockPos northSection = WildVineHelper.VinePosForHost(new BlockPos(1, 64, 0), BlockFacing.NORTH);
            acc.SetBlock(2, northSection);
            BlockPos eastRimVine = new BlockPos(3, 64, 0);

            Assert.False(WildVineHelper.TouchesVineNetworkForSpread(acc, eastRimVine, tropical: false, northSection));
            Assert.True(WildVineHelper.TouchesVineNetworkForSpread(
                acc,
                eastRimVine,
                tropical: false,
                northSection,
                rimWalkScore: 10));
        }

        [Fact]
        public void IsRimWrapCell_AllowsInsideCornerRimPlacement()
        {
            var source = new BlockPos(1, 64, 1);
            var rimVine = new BlockPos(3, 64, 0);

            Assert.False(WildVineHelper.IsRimWrapCell(rimVine, source, rimWalkScore: 0));
            Assert.True(WildVineHelper.IsRimWrapCell(rimVine, source, rimWalkScore: 10));
        }

        [Fact]
        public void HasInsideCornerCavity_DetectsReEntrantAirPocket()
        {
            Block air = new Block { BlockId = 0, Code = new AssetLocation("game", "air") };
            Block stone = new Block { BlockId = 1, BlockMaterial = EnumBlockMaterial.Stone, Replaceable = 0 };
            var acc = new EcologyTestBlockAccessor(new[] { air, stone });
            acc.SetBlock(1, new BlockPos(1, 64, 0));
            acc.SetBlock(1, new BlockPos(2, 64, 0));
            acc.SetBlock(1, new BlockPos(2, 64, 1));

            Assert.True(WildVineHelper.HasInsideCornerCavity(
                acc,
                new BlockPos(1, 64, 0),
                BlockFacing.NORTH,
                BlockFacing.EAST));
        }

        [Fact]
        public void ShouldPlaceAsEndAt_FalseWhenVineDirectlyBelow()
        {
            Block[] blocks =
            {
                new Block { BlockId = 0, Code = new AssetLocation("game", "air") },
                new Block { BlockId = 1, Code = new AssetLocation("game", "wildvine-section-north") },
                new Block { BlockId = 2, Code = new AssetLocation("game", "wildvine-end-north") },
            };
            var acc = new EcologyTestBlockAccessor(blocks);
            var info = new WildVineInfo(false, false, BlockFacing.NORTH);
            acc.SetBlock(1, new BlockPos(0, 60, 0));
            acc.SetBlock(2, new BlockPos(0, 61, 0));

            IWorldAccessor world = CreateVineResolveWorld(blocks);

            Assert.False(WildVineHelper.ShouldPlaceAsEndAt(acc, world, new BlockPos(0, 61, 0), info));
        }

        [Fact]
        public void CanSeedJunctionFromEnd_OnlyAtWallFoot()
        {
            Block air = new Block { BlockId = 0, Code = new AssetLocation("game", "air") };
            Block stone = new Block { BlockId = 3, BlockMaterial = EnumBlockMaterial.Stone, Replaceable = 0 };
            Block[] blocks =
            {
                air,
                new Block { BlockId = 1, Code = new AssetLocation("game", "wildvine-section-north") },
                new Block { BlockId = 2, Code = new AssetLocation("game", "wildvine-end-north") },
                stone,
            };
            var acc = new EcologyTestBlockAccessor(blocks);
            var info = new WildVineInfo(false, false, BlockFacing.NORTH);
            acc.SetBlock(3, new BlockPos(0, 60, 1));
            acc.SetBlock(2, new BlockPos(0, 60, 0));

            IWorldAccessor world = CreateVineResolveWorld(blocks);

            Assert.True(WildVineHelper.CanSeedJunctionFromEnd(acc, world, new BlockPos(0, 60, 0), info));
        }

        [Fact]
        public void TryResolveNearbySurfaceLatch_FindsSolidFaceBesideHangingSection()
        {
            Block air = new Block { BlockId = 0, Code = new AssetLocation("game", "air") };
            Block stone = new Block { BlockId = 3, BlockMaterial = EnumBlockMaterial.Stone, Replaceable = 0 };
            Block[] blocks =
            {
                air,
                new Block { BlockId = 1, Code = new AssetLocation("game", "wildvine-section-north") },
                new Block { BlockId = 2, Code = new AssetLocation("game", "wildvine-end-north") },
                stone,
            };
            var acc = new EcologyTestBlockAccessor(blocks);
            var info = new WildVineInfo(false, false, BlockFacing.NORTH);
            acc.SetBlock(3, new BlockPos(0, 64, 1));
            acc.SetBlock(1, new BlockPos(0, 64, 0));
            acc.SetBlock(1, new BlockPos(0, 63, 0));
            acc.SetBlock(3, new BlockPos(1, 63, 0));

            IWorldAccessor world = CreateVineResolveWorld(blocks);

            Assert.True(WildVineHelper.IsUnsupportedSection(acc, world, new BlockPos(0, 63, 0), info));
            Assert.True(WildVineHelper.TryResolveNearbySurfaceLatch(
                acc,
                world,
                new BlockPos(0, 63, 0),
                info,
                out BlockPos host,
                out BlockFacing facing,
                out BlockPos vinePos));

            Assert.Equal(new BlockPos(1, 63, 0), host);
            Assert.Equal(BlockFacing.NORTH, facing);
            Assert.Equal(new BlockPos(1, 63, -1), vinePos);
        }

        [Fact]
        public void TryResolveNearbySurfaceLatch_RejectsFarSideOfAdjacentBlock()
        {
            Block air = new Block { BlockId = 0, Code = new AssetLocation("game", "air") };
            Block stone = new Block { BlockId = 3, BlockMaterial = EnumBlockMaterial.Stone, Replaceable = 0 };
            Block[] blocks =
            {
                air,
                new Block { BlockId = 1, Code = new AssetLocation("game", "wildvine-section-north") },
                new Block { BlockId = 2, Code = new AssetLocation("game", "wildvine-end-north") },
                stone,
            };
            var acc = new EcologyTestBlockAccessor(blocks);
            var info = new WildVineInfo(false, false, BlockFacing.NORTH);
            acc.SetBlock(3, new BlockPos(0, 64, 1));
            acc.SetBlock(1, new BlockPos(0, 64, 0));
            acc.SetBlock(1, new BlockPos(0, 63, 0));
            acc.SetBlock(3, new BlockPos(1, 63, 0));

            IWorldAccessor world = CreateVineResolveWorld(blocks);

            Assert.True(WildVineHelper.TryResolveNearbySurfaceLatch(
                acc,
                world,
                new BlockPos(0, 63, 0),
                info,
                out _,
                out BlockFacing facing,
                out BlockPos vinePos));

            Assert.NotEqual(BlockFacing.EAST, facing);
            Assert.NotEqual(new BlockPos(2, 63, 0), vinePos);
        }

        [Fact]
        public void TryResolveNearbySurfaceLatch_SkipsSupportedSections()
        {
            Block air = new Block { BlockId = 0, Code = new AssetLocation("game", "air") };
            Block stone = new Block { BlockId = 3, BlockMaterial = EnumBlockMaterial.Stone, Replaceable = 0 };
            Block[] blocks =
            {
                air,
                new Block { BlockId = 1, Code = new AssetLocation("game", "wildvine-section-north") },
                stone,
            };
            var acc = new EcologyTestBlockAccessor(blocks);
            var info = new WildVineInfo(false, false, BlockFacing.NORTH);
            acc.SetBlock(3, new BlockPos(0, 64, 1));
            acc.SetBlock(1, new BlockPos(0, 64, 0));
            acc.SetBlock(3, new BlockPos(1, 64, 0));

            IWorldAccessor world = CreateVineResolveWorld(blocks);

            Assert.False(WildVineHelper.TryResolveNearbySurfaceLatch(
                acc,
                world,
                new BlockPos(0, 64, 0),
                info,
                out _,
                out _,
                out _));
        }

        [Fact]
        public void CanContinueDownward_AllowsHangingBelowSupportedColumn()
        {
            Block air = new Block { BlockId = 0, Code = new AssetLocation("game", "air") };
            Block stone = new Block { BlockId = 3, BlockMaterial = EnumBlockMaterial.Stone, Replaceable = 0 };
            Block[] blocks =
            {
                air,
                new Block { BlockId = 1, Code = new AssetLocation("game", "wildvine-section-north") },
                new Block { BlockId = 2, Code = new AssetLocation("game", "wildvine-end-north") },
                stone,
            };
            var acc = new EcologyTestBlockAccessor(blocks);
            var info = new WildVineInfo(false, false, BlockFacing.NORTH);
            acc.SetBlock(3, new BlockPos(0, 64, 1));
            acc.SetBlock(1, new BlockPos(0, 64, 0));
            acc.SetBlock(1, new BlockPos(0, 63, 0));
            acc.SetBlock(2, new BlockPos(0, 62, 0));

            IWorldAccessor world = CreateVineResolveWorld(blocks);

            Assert.True(WildVineHelper.CanContinueDownward(acc, world, new BlockPos(0, 62, 0), info));
        }

        [Fact]
        public void CanContinueDownward_AllowsLongHangWhenTopAnchored()
        {
            Block air = new Block { BlockId = 0, Code = new AssetLocation("game", "air") };
            Block stone = new Block { BlockId = 3, BlockMaterial = EnumBlockMaterial.Stone, Replaceable = 0 };
            Block[] blocks =
            {
                air,
                new Block { BlockId = 1, Code = new AssetLocation("game", "wildvine-section-north") },
                new Block { BlockId = 2, Code = new AssetLocation("game", "wildvine-end-north") },
                stone,
            };
            var acc = new EcologyTestBlockAccessor(blocks);
            var info = new WildVineInfo(false, false, BlockFacing.NORTH);
            acc.SetBlock(3, new BlockPos(0, 64, 1));
            acc.SetBlock(1, new BlockPos(0, 64, 0));
            for (int y = 63; y >= 50; y--)
            {
                acc.SetBlock(1, new BlockPos(0, y, 0));
            }

            acc.SetBlock(2, new BlockPos(0, 49, 0));

            IWorldAccessor world = CreateVineResolveWorld(blocks);

            Assert.True(WildVineHelper.CanContinueDownward(acc, world, new BlockPos(0, 49, 0), info));
        }

        [Fact]
        public void CanSeedJunctionFromEnd_FalseForUnsupportedHangingTip()
        {
            Block[] blocks =
            {
                new Block { BlockId = 0, Code = new AssetLocation("game", "air") },
                new Block { BlockId = 1, Code = new AssetLocation("game", "wildvine-section-north") },
                new Block { BlockId = 2, Code = new AssetLocation("game", "wildvine-end-north") },
            };
            var acc = new EcologyTestBlockAccessor(blocks);
            var info = new WildVineInfo(false, false, BlockFacing.NORTH);
            acc.SetBlock(2, new BlockPos(0, 60, 0));

            IWorldAccessor world = CreateVineResolveWorld(blocks);

            Assert.False(WildVineHelper.CanSeedJunctionFromEnd(acc, world, new BlockPos(0, 60, 0), info));
        }

        [Fact]
        public void NeedsTipBelowSection_TrueWhenHangingContinuationBelow()
        {
            Block air = new Block { BlockId = 0, Code = new AssetLocation("game", "air") };
            Block stone = new Block { BlockId = 3, BlockMaterial = EnumBlockMaterial.Stone, Replaceable = 0 };
            Block[] blocks =
            {
                air,
                new Block { BlockId = 1, Code = new AssetLocation("game", "wildvine-section-north") },
                new Block { BlockId = 2, Code = new AssetLocation("game", "wildvine-end-north") },
                stone,
            };
            var acc = new EcologyTestBlockAccessor(blocks);
            var info = new WildVineInfo(false, false, BlockFacing.NORTH);
            acc.SetBlock(3, new BlockPos(0, 64, 1));
            acc.SetBlock(1, new BlockPos(0, 64, 0));
            acc.SetBlock(1, new BlockPos(0, 63, 0));

            IWorldAccessor world = CreateVineResolveWorld(blocks);

            Assert.True(WildVineHelper.NeedsTipBelowSection(acc, world, new BlockPos(0, 63, 0), info));
        }

        [Fact]
        public void NeedsTipBelowSection_FalseWhenVineDirectlyBelow()
        {
            Block[] blocks =
            {
                new Block { BlockId = 0, Code = new AssetLocation("game", "air") },
                new Block { BlockId = 1, Code = new AssetLocation("game", "wildvine-section-north") },
                new Block { BlockId = 2, Code = new AssetLocation("game", "wildvine-end-north") },
            };
            var acc = new EcologyTestBlockAccessor(blocks);
            var info = new WildVineInfo(false, false, BlockFacing.NORTH);
            acc.SetBlock(1, new BlockPos(0, 61, 0));
            acc.SetBlock(1, new BlockPos(0, 60, 0));

            IWorldAccessor world = CreateVineResolveWorld(blocks);

            Assert.False(WildVineHelper.NeedsTipBelowSection(acc, world, new BlockPos(0, 61, 0), info));
        }

        [Fact]
        public void NeedsTipBelowSection_FalseWhenEndDirectlyBelow()
        {
            Block[] blocks =
            {
                new Block { BlockId = 0, Code = new AssetLocation("game", "air") },
                new Block { BlockId = 1, Code = new AssetLocation("game", "wildvine-section-north") },
                new Block { BlockId = 2, Code = new AssetLocation("game", "wildvine-end-north") },
            };
            var acc = new EcologyTestBlockAccessor(blocks);
            var info = new WildVineInfo(false, false, BlockFacing.NORTH);
            acc.SetBlock(1, new BlockPos(0, 61, 0));
            acc.SetBlock(2, new BlockPos(0, 60, 0));

            IWorldAccessor world = CreateVineResolveWorld(blocks);

            Assert.False(WildVineHelper.NeedsTipBelowSection(acc, world, new BlockPos(0, 61, 0), info));
        }

        [Fact]
        public void CanSpreadHorizontallyFrom_SectionsOnly()
        {
            Block[] blocks =
            {
                new Block { BlockId = 0, Code = new AssetLocation("game", "air") },
                new Block { BlockId = 1, Code = new AssetLocation("game", "wildvine-section-north") },
                new Block { BlockId = 2, Code = new AssetLocation("game", "wildvine-end-north") },
            };
            var acc = new EcologyTestBlockAccessor(blocks);
            var info = new WildVineInfo(false, false, BlockFacing.NORTH);
            acc.SetBlock(1, new BlockPos(0, 64, 0));
            acc.SetBlock(2, new BlockPos(0, 60, 0));

            IWorldAccessor world = CreateVineResolveWorld(blocks);

            Assert.True(WildVineHelper.CanSpreadHorizontallyFrom(acc, world, new BlockPos(0, 64, 0), info));
            Assert.False(WildVineHelper.CanSpreadHorizontallyFrom(acc, world, new BlockPos(0, 60, 0), info));
        }

        [Fact]
        public void PlacedVineSection_RegistersImmediately()
        {
            using var host = EcosystemSimHost.Create(new EcosystemConfig
            {
                EcosystemEnabled = true,
                EnableWildVineEcology = true,
                OnlyActivateNearPlayers = false,
            });

            var pos = new BlockPos(12, 64, 12);
            host.Accessor.SetBlockCode("game:wildvine-section-north", pos);

            host.Eco.Test_TryRegisterPlacedBlock(pos);

            Assert.True(host.Eco.Test_TryGetRegistryEntry(pos, out ReproducerEntry entry));
            Assert.Equal(EcologyHabitat.WildVine, entry.Requirements.Habitat);
            Assert.True(WildVineHelper.IsSectionBlock(host.Accessor.GetBlock(pos), new WildVineInfo(false, false, BlockFacing.NORTH)));
        }

        [Fact]
        public void SectionBlock_IsEcologySpreadParentAndParticipant()
        {
            var block = new Block { Code = new AssetLocation("game", "wildvine-section-north") };

            Assert.True(PlantCodeHelper.IsEcologySpreadParent(block));
            Assert.True(EcosystemParticipant.TryFromBlock(block, out IEcosystemParticipant participant));
            Assert.Equal("wildvine-end-north", participant.SpreadBlockCode.Path);
        }

        [Fact]
        public void PlacedVineColumn_DoesNotDuplicateRegistryWhenSectionAdded()
        {
            using var host = EcosystemSimHost.Create(new EcosystemConfig
            {
                EcosystemEnabled = true,
                EnableWildVineEcology = true,
                OnlyActivateNearPlayers = false,
            });

            var pos = new BlockPos(10, 64, 10);
            host.Accessor.SetBlockCode("game:wildvine-end-north", pos);
            host.Accessor.SetBlockCode("game:wildvine-section-north", pos.UpCopy());

            host.Eco.Test_TryRegisterPlacedBlock(pos);
            host.Eco.Test_TryRegisterPlacedBlock(pos.UpCopy());

            Assert.True(host.Eco.Test_TryGetRegistryEntry(pos, out ReproducerEntry entry));
            Assert.Equal(EcologyHabitat.WildVine, entry.Requirements.Habitat);
            Assert.False(host.Eco.Test_TryGetRegistryEntry(pos.UpCopy(), out _));
        }

        [Fact]
        public void PruneUnsupportedColumn_RemovesVinesWhenHostIsGone()
        {
            Block air = new Block { BlockId = 0, Code = new AssetLocation("game", "air") };
            Block stone = new Block { BlockId = 3, BlockMaterial = EnumBlockMaterial.Stone, Replaceable = 0 };
            Block[] blocks =
            {
                air,
                new Block { BlockId = 1, Code = new AssetLocation("game", "wildvine-section-north") },
                new Block { BlockId = 2, Code = new AssetLocation("game", "wildvine-end-north") },
                stone,
            };
            var acc = new EcologyTestBlockAccessor(blocks);
            var info = new WildVineInfo(false, false, BlockFacing.NORTH);
            acc.SetBlock(3, new BlockPos(0, 64, 1));
            acc.SetBlock(1, new BlockPos(0, 64, 0));
            acc.SetBlock(1, new BlockPos(0, 63, 0));
            acc.SetBlock(2, new BlockPos(0, 62, 0));

            IWorldAccessor world = CreateVineResolveWorld(blocks);

            acc.SetBlock(0, new BlockPos(0, 64, 1));

            int removed = WildVineColumnSupport.PruneUnsupportedColumn(acc, world, new BlockPos(0, 64, 0));

            Assert.Equal(3, removed);
            Assert.Equal(0, acc.GetBlock(new BlockPos(0, 64, 0)).Id);
            Assert.Equal(0, acc.GetBlock(new BlockPos(0, 63, 0)).Id);
            Assert.Equal(0, acc.GetBlock(new BlockPos(0, 62, 0)).Id);
        }

        [Fact]
        public void PruneUnsupportedColumn_KeepsHangingBelowSupportedTop()
        {
            Block air = new Block { BlockId = 0, Code = new AssetLocation("game", "air") };
            Block stone = new Block { BlockId = 3, BlockMaterial = EnumBlockMaterial.Stone, Replaceable = 0 };
            Block[] blocks =
            {
                air,
                new Block { BlockId = 1, Code = new AssetLocation("game", "wildvine-section-north") },
                new Block { BlockId = 2, Code = new AssetLocation("game", "wildvine-end-north") },
                stone,
            };
            var acc = new EcologyTestBlockAccessor(blocks);
            acc.SetBlock(3, new BlockPos(0, 64, 1));
            acc.SetBlock(1, new BlockPos(0, 64, 0));
            acc.SetBlock(1, new BlockPos(0, 63, 0));
            acc.SetBlock(2, new BlockPos(0, 62, 0));

            IWorldAccessor world = CreateVineResolveWorld(blocks);

            int removed = WildVineColumnSupport.PruneUnsupportedColumn(acc, world, new BlockPos(0, 64, 0));

            Assert.Equal(0, removed);
            Assert.True(WildVineHelper.IsVineBlock(acc.GetBlock(new BlockPos(0, 62, 0))));
        }

        [Fact]
        public void PruneUnsupportedColumn_KeepsLongHangWhenTopIsAnchored()
        {
            Block air = new Block { BlockId = 0, Code = new AssetLocation("game", "air") };
            Block stone = new Block { BlockId = 3, BlockMaterial = EnumBlockMaterial.Stone, Replaceable = 0 };
            Block[] blocks =
            {
                air,
                new Block { BlockId = 1, Code = new AssetLocation("game", "wildvine-section-north") },
                new Block { BlockId = 2, Code = new AssetLocation("game", "wildvine-end-north") },
                stone,
            };
            var acc = new EcologyTestBlockAccessor(blocks);
            acc.SetBlock(3, new BlockPos(0, 64, 1));
            acc.SetBlock(1, new BlockPos(0, 64, 0));
            for (int y = 63; y >= 55; y--)
            {
                acc.SetBlock(1, new BlockPos(0, y, 0));
            }

            acc.SetBlock(2, new BlockPos(0, 54, 0));

            IWorldAccessor world = CreateVineResolveWorld(blocks);

            int removed = WildVineColumnSupport.PruneUnsupportedColumn(acc, world, new BlockPos(0, 64, 0));

            Assert.Equal(0, removed);
            Assert.True(WildVineHelper.IsVineBlock(acc.GetBlock(new BlockPos(0, 54, 0))));
        }

        static IWorldAccessor CreateVineResolveWorld(Block[] blocks)
        {
            var mock = new Mock<IWorldAccessor>();
            mock.Setup(w => w.GetBlock(It.IsAny<AssetLocation>()))
                .Returns((AssetLocation loc) =>
                {
                    if (loc == null) return blocks[0];
                    for (int i = 0; i < blocks.Length; i++)
                    {
                        if (blocks[i]?.Code != null && blocks[i].Code.Equals(loc)) return blocks[i];
                    }

                    return blocks[0];
                });
            return mock.Object;
        }
    }
}
