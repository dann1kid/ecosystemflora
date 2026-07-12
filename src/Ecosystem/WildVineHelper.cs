using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal readonly struct WildVineInfo
    {
        public readonly bool Tropical;
        public readonly bool IsEnd;
        public readonly BlockFacing Facing;

        public WildVineInfo(bool tropical, bool isEnd, BlockFacing facing)
        {
            Tropical = tropical;
            IsEnd = isEnd;
            Facing = facing;
        }
    }

    /// <summary>Vanilla <c>wildvine-*</c> block parsing and placement helpers.</summary>
    internal static class WildVineHelper
    {
        public const string TemperateSpecies = "wildvine";
        public const string TropicalSpecies = "wildvine-tropical";

        public static bool IsVineBlock(Block block) => TryParse(block, out _);

        public static bool IsEndBlock(Block block) => TryParse(block, out WildVineInfo info) && info.IsEnd;

        public static bool IsSectionBlock(Block block, in WildVineInfo expected) =>
            TryParse(block, out WildVineInfo info) && !info.IsEnd
            && info.Tropical == expected.Tropical && info.Facing == expected.Facing;

        public static bool TryParse(Block block, out WildVineInfo info)
        {
            info = default;
            if (block?.Code == null || block.Code.Domain != "game") return false;

            string path = block.Code.Path;
            if (string.IsNullOrEmpty(path) || !path.StartsWith("wildvine")) return false;

            bool tropical = path.Contains("-tropical-");
            bool isEnd = path.Contains("-end-");
            bool isSection = path.Contains("-section-");
            if (!isEnd && !isSection) return false;

            BlockFacing facing = ResolveFacing(block, path);
            if (facing == null) return false;

            info = new WildVineInfo(tropical, isEnd, facing);
            return true;
        }

        public static string SpeciesFor(bool tropical) => tropical ? TropicalSpecies : TemperateSpecies;

        public static bool IsKnown(string species) =>
            species == TemperateSpecies || species == TropicalSpecies;

        public static Block ResolveEndBlock(IWorldAccessor world, bool tropical, BlockFacing facing)
        {
            if (world == null || facing == null) return null;
            string path = tropical
                ? "wildvine-tropical-end-" + facing.Code
                : "wildvine-end-" + facing.Code;
            return world.GetBlock(new AssetLocation("game", path));
        }

        public static Block ResolveSectionBlock(IWorldAccessor world, bool tropical, BlockFacing facing)
        {
            if (world == null || facing == null) return null;
            string path = tropical
                ? "wildvine-tropical-section-" + facing.Code
                : "wildvine-section-" + facing.Code;
            return world.GetBlock(new AssetLocation("game", path));
        }

        public static bool MatchesColumn(Block block, WildVineInfo expected)
        {
            if (!TryParse(block, out WildVineInfo info)) return false;
            return info.Tropical == expected.Tropical && info.Facing == expected.Facing;
        }

        public static bool CanHostVine(IBlockAccessor acc, Block vineSample, BlockPos hostPos, BlockFacing vineFacing)
        {
            if (acc == null || vineSample == null || hostPos == null || vineFacing == null) return false;

            Block host = acc.GetBlock(hostPos);
            if (host == null || (host.Id == 0 && host.BlockId == 0)) return false;
            if (!IsStructuralWallHost(host, vineFacing)) return false;

            return host.CanAttachBlockAt(acc, vineSample, hostPos, vineFacing);
        }

        /// <summary>Plants and loose blocks cannot host spread; soil is allowed on vertical faces only.</summary>
        public static bool IsStructuralWallHost(Block host, BlockFacing vineFacing = null)
        {
            if (host == null) return false;
            if (host.Id == 0 && host.BlockId == 0) return false;
            if (host.BlockMaterial == EnumBlockMaterial.Plant) return false;
            if (PlantCodeHelper.IsEcologyPlant(host)) return false;
            if (host.Replaceable >= SuitabilityEvaluator.ReproduceMinReplaceable) return false;

            if (host.BlockMaterial == EnumBlockMaterial.Soil)
            {
                // Side faces of dirt/stone walls are valid; top/bottom soil (meadow patches) are not.
                return vineFacing != null && vineFacing.Axis != EnumAxis.Y;
            }

            return true;
        }

        /// <summary>True when a matching vine section/end touches this cell (same column connectivity).</summary>
        public static bool TouchesVineColumn(IBlockAccessor acc, BlockPos vinePos, in WildVineInfo info)
        {
            if (acc == null || vinePos == null) return false;

            for (int i = 0; i < BlockFacing.ALLFACES.Length; i++)
            {
                BlockFacing face = BlockFacing.ALLFACES[i];
                BlockPos neighbor = vinePos.AddCopy(face);
                if (!acc.IsValidPos(neighbor)) continue;
                if (MatchesColumn(acc.GetBlock(neighbor), info)) return true;
            }

            return false;
        }

        /// <summary>Adjacent vine of the same variant (any facing) — corner connectivity.</summary>
        public static bool TouchesVineNetwork(IBlockAccessor acc, BlockPos vinePos, bool tropical)
        {
            if (acc == null || vinePos == null) return false;

            for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
            {
                BlockFacing face = BlockFacing.HORIZONTALS[i];
                BlockPos neighbor = vinePos.AddCopy(face);
                if (!acc.IsValidPos(neighbor)) continue;

                if (TryParse(acc.GetBlock(neighbor), out WildVineInfo info) && info.Tropical == tropical)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Spread connectivity: 6-neighbors, same-Y corners, or wrap distance from source section.</summary>
        public static bool TouchesVineNetworkForSpread(
            IBlockAccessor acc,
            BlockPos vinePos,
            bool tropical,
            BlockPos sourceSection = null,
            int rimWalkScore = 0)
        {
            if (acc == null || vinePos == null) return false;

            for (int i = 0; i < BlockFacing.ALLFACES.Length; i++)
            {
                BlockPos neighbor = vinePos.AddCopy(BlockFacing.ALLFACES[i]);
                if (!acc.IsValidPos(neighbor)) continue;
                if (TryParse(acc.GetBlock(neighbor), out WildVineInfo info) && info.Tropical == tropical)
                {
                    return true;
                }
            }

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0) continue;

                    BlockPos neighbor = new BlockPos(vinePos.X + dx, vinePos.Y, vinePos.Z + dz);
                    if (!acc.IsValidPos(neighbor)) continue;
                    if (TryParse(acc.GetBlock(neighbor), out WildVineInfo info) && info.Tropical == tropical)
                    {
                        return true;
                    }
                }
            }

            return sourceSection != null
                && (IsAdjacentWrapCell(vinePos, sourceSection)
                    || IsRimWrapCell(vinePos, sourceSection, rimWalkScore));
        }

        /// <summary>Rim-walk placement around an inside corner stays connected within the scan box.</summary>
        public static bool IsRimWrapCell(BlockPos candidate, BlockPos sourceSection, int rimWalkScore)
        {
            if (candidate == null || sourceSection == null || rimWalkScore <= 0) return false;

            int maxScore = InnerCornerRimScan * 10 + InnerCornerRimScan;
            if (rimWalkScore > maxScore) return false;

            int dx = System.Math.Abs(candidate.X - sourceSection.X);
            int dy = System.Math.Abs(candidate.Y - sourceSection.Y);
            int dz = System.Math.Abs(candidate.Z - sourceSection.Z);
            return dx <= InnerCornerRimScan && dy <= 1 && dz <= InnerCornerRimScan;
        }

        /// <summary>Candidate cell is one step around a section on a perpendicular face (corner / vertical wrap).</summary>
        public static bool IsAdjacentWrapCell(BlockPos candidate, BlockPos sectionPos)
        {
            if (candidate == null || sectionPos == null) return false;

            int dx = System.Math.Abs(candidate.X - sectionPos.X);
            int dy = System.Math.Abs(candidate.Y - sectionPos.Y);
            int dz = System.Math.Abs(candidate.Z - sectionPos.Z);
            return dx <= 1 && dy <= 1 && dz <= 1 && (dx + dy + dz) > 0;
        }

        /// <summary>Host and vine cell for a perpendicular wall face around a section corner.</summary>
        public static bool TryResolveAdjacentFacePlacement(
            IBlockAccessor acc,
            BlockPos sectionPos,
            in WildVineInfo sourceInfo,
            BlockFacing adjacentFacing,
            int alongWallStep,
            int dy,
            BlockPos scratchHost,
            out BlockPos vinePos,
            out int rimWalkScore)
        {
            vinePos = null;
            rimWalkScore = 0;
            if (acc == null || sectionPos == null || adjacentFacing == null || scratchHost == null) return false;

            BlockPos sourceHost = HostPos(sectionPos, sourceInfo.Facing);
            BlockFacing alongPerpWall = WallTangent(adjacentFacing);
            OffsetWallPosition(sourceHost, alongPerpWall, alongWallStep, dy, scratchHost);

            BlockPos standardVine = VinePosForHost(scratchHost, adjacentFacing);
            if (IsVacantVineCell(acc, scratchHost, adjacentFacing, standardVine))
            {
                vinePos = standardVine;
                return true;
            }

            if (TryResolveInsideCornerStepAcross(
                    acc,
                    scratchHost,
                    sourceInfo,
                    adjacentFacing,
                    out vinePos,
                    out rimWalkScore))
            {
                return true;
            }

            return TryResolveInsideCornerRimPlacement(
                acc,
                scratchHost,
                sourceInfo,
                adjacentFacing,
                out vinePos,
                out rimWalkScore);
        }

        public static bool TryResolveAdjacentFacePlacement(
            IBlockAccessor acc,
            BlockPos sectionPos,
            in WildVineInfo sourceInfo,
            BlockFacing adjacentFacing,
            int alongWallStep,
            int dy,
            BlockPos scratchHost,
            out BlockPos vinePos)
        {
            return TryResolveAdjacentFacePlacement(
                acc,
                sectionPos,
                sourceInfo,
                adjacentFacing,
                alongWallStep,
                dy,
                scratchHost,
                out vinePos,
                out _);
        }

        internal const int InnerCornerRimScan = 5;

        static bool TryResolveInsideCornerRimPlacement(
            IBlockAccessor acc,
            BlockPos rimOriginHost,
            in WildVineInfo sourceInfo,
            BlockFacing adjacentFacing,
            out BlockPos vinePos,
            out int rimWalkScore)
        {
            vinePos = null;
            rimWalkScore = 0;
            if (acc == null || rimOriginHost == null) return false;

            BlockFacing alongSourceWall = WallTangent(sourceInfo.Facing);

            int bestScore = int.MaxValue;
            BlockPos bestHost = null;
            BlockPos bestVine = null;

            for (int alongStep = 0; alongStep <= InnerCornerRimScan; alongStep++)
            {
                for (int signIndex = 0; signIndex <= (alongStep == 0 ? 0 : 1); signIndex++)
                {
                    int alongSigned = alongStep == 0 ? 0 : (signIndex == 0 ? alongStep : -alongStep);
                    BlockPos rimHost = rimOriginHost.Copy();
                    OffsetWallPosition(rimHost, alongSourceWall, alongSigned, 0, rimHost);

                    for (int perpStep = 0; perpStep <= InnerCornerRimScan; perpStep++)
                    {
                        BlockPos perpHost = perpStep == 0
                            ? rimHost.Copy()
                            : rimHost.AddCopy(adjacentFacing, perpStep);
                        BlockPos candidateVine = VinePosForHost(perpHost, adjacentFacing);
                        if (!IsVacantVineCell(acc, perpHost, adjacentFacing, candidateVine)) continue;

                        int score = alongStep * 10 + perpStep;
                        if (score >= bestScore) continue;

                        bestScore = score;
                        bestHost = perpHost.Copy();
                        bestVine = candidateVine;
                    }
                }
            }

            if (bestHost == null || bestVine == null) return false;

            rimOriginHost.Set(bestHost);
            rimWalkScore = bestScore;
            vinePos = bestVine;
            return true;
        }

        /// <summary>Single-block hop around a re-entrant corner onto the next wall segment.</summary>
        static bool TryResolveInsideCornerStepAcross(
            IBlockAccessor acc,
            BlockPos rimOriginHost,
            in WildVineInfo sourceInfo,
            BlockFacing adjacentFacing,
            out BlockPos vinePos,
            out int rimWalkScore)
        {
            vinePos = null;
            rimWalkScore = 0;
            if (acc == null || rimOriginHost == null) return false;
            if (!HasInsideCornerCavity(acc, rimOriginHost, sourceInfo.Facing, adjacentFacing)) return false;

            BlockFacing alongSourceWall = WallTangent(sourceInfo.Facing);
            for (int alongStep = -1; alongStep <= 1; alongStep += 2)
            {
                BlockPos steppedHost = rimOriginHost.Copy();
                OffsetWallPosition(steppedHost, alongSourceWall, alongStep, 0, steppedHost);
                BlockPos candidateVine = VinePosForHost(steppedHost, adjacentFacing);
                if (!IsVacantVineCell(acc, steppedHost, adjacentFacing, candidateVine)) continue;

                rimOriginHost.Set(steppedHost);
                vinePos = candidateVine;
                rimWalkScore = System.Math.Abs(alongStep);
                return true;
            }

            for (int perpStep = 1; perpStep <= InnerCornerRimScan; perpStep++)
            {
                BlockPos steppedHost = rimOriginHost.AddCopy(adjacentFacing, perpStep);
                BlockPos candidateVine = VinePosForHost(steppedHost, adjacentFacing);
                if (!IsVacantVineCell(acc, steppedHost, adjacentFacing, candidateVine)) continue;

                rimOriginHost.Set(steppedHost);
                vinePos = candidateVine;
                rimWalkScore = perpStep;
                return true;
            }

            return false;
        }

        static bool IsVacantVineCell(IBlockAccessor acc, BlockPos host, BlockFacing facing, BlockPos vinePos)
        {
            if (acc == null || host == null || vinePos == null || facing == null) return false;
            if (!acc.IsValidPos(vinePos)) return false;
            if (!IsStructuralWallHost(acc.GetBlock(host), facing)) return false;
            return PlantVacancyRules.IsVacantPlantSpace(acc.GetBlock(vinePos));
        }

        /// <summary>Air pocket inside a re-entrant corner — exterior wrap would hit solid stone.</summary>
        public static bool HasInsideCornerCavity(
            IBlockAccessor acc,
            BlockPos host,
            BlockFacing sourceFacing,
            BlockFacing adjacentFacing)
        {
            if (acc == null || host == null || sourceFacing == null || adjacentFacing == null) return false;

            BlockPos cavity = host.AddCopy(sourceFacing.Opposite);
            if (IsInsideCavityBlock(acc, cavity)) return true;

            cavity = host.AddCopy(adjacentFacing).AddCopy(sourceFacing.Opposite);
            if (IsInsideCavityBlock(acc, cavity)) return true;

            cavity = host.AddCopy(sourceFacing.Opposite).AddCopy(adjacentFacing);
            return IsInsideCavityBlock(acc, cavity);
        }

        static bool IsInsideCavityBlock(IBlockAccessor acc, BlockPos pos)
        {
            if (acc == null || pos == null || !acc.IsValidPos(pos)) return false;

            Block block = acc.GetBlock(pos);
            return block.Id == 0 || block.Replaceable >= SuitabilityEvaluator.ReproduceMinReplaceable;
        }

        static void OffsetWallPosition(BlockPos origin, BlockFacing alongWall, int alongStep, int dy, BlockPos dest)
        {
            int dx = 0;
            int dz = 0;
            if (alongWall == BlockFacing.EAST) dx = alongStep;
            else if (alongWall == BlockFacing.WEST) dx = -alongStep;
            else if (alongWall == BlockFacing.NORTH) dz = -alongStep;
            else if (alongWall == BlockFacing.SOUTH) dz = alongStep;

            dest.Set(origin.X + dx, origin.Y + dy, origin.Z + dz);
        }

        public static BlockFacing[] PerpendicularHorizontalFacings(BlockFacing vineFacing)
        {
            if (vineFacing == null || vineFacing.Axis == EnumAxis.Y)
            {
                return System.Array.Empty<BlockFacing>();
            }

            if (vineFacing.Axis == EnumAxis.Z)
            {
                return new[] { BlockFacing.EAST, BlockFacing.WEST };
            }

            return new[] { BlockFacing.NORTH, BlockFacing.SOUTH };
        }

        /// <summary>Section with no structural wall behind it (hanging continuation).</summary>
        public static bool IsUnsupportedSection(
            IBlockAccessor acc,
            IWorldAccessor world,
            BlockPos sectionPos,
            in WildVineInfo info)
        {
            if (acc == null || sectionPos == null) return false;
            if (!IsSectionBlock(acc.GetBlock(sectionPos), info)) return false;

            return world == null || !HasStructuralSupportBehind(acc, world, sectionPos, info);
        }

        /// <summary>Candidate vine cell is one step from a hanging section (includes diagonals on the same row).</summary>
        public static bool IsNearbySurfaceLatchCell(BlockPos candidate, BlockPos sourceSection)
        {
            if (candidate == null || sourceSection == null) return false;

            int dx = System.Math.Abs(candidate.X - sourceSection.X);
            int dy = System.Math.Abs(candidate.Y - sourceSection.Y);
            int dz = System.Math.Abs(candidate.Z - sourceSection.Z);
            return dx <= 1 && dy <= 1 && dz <= 1 && (dx + dy + dz) > 0;
        }

        /// <summary>Unsupported section latching onto a nearby solid wall face.</summary>
        public static bool TryResolveNearbySurfaceLatch(
            IBlockAccessor acc,
            IWorldAccessor world,
            BlockPos hangingSectionPos,
            in WildVineInfo info,
            out BlockPos host,
            out BlockFacing placedFacing,
            out BlockPos vinePos)
        {
            host = null;
            placedFacing = null;
            vinePos = null;
            if (acc == null || world == null || hangingSectionPos == null) return false;
            if (!IsUnsupportedSection(acc, world, hangingSectionPos, info)) return false;

            int bestScore = int.MaxValue;
            BlockPos bestHost = null;
            BlockFacing bestFacing = null;
            BlockPos bestVine = null;

            for (int dyIndex = 0; dyIndex < 3; dyIndex++)
            {
                int dy = dyIndex == 0 ? 0 : (dyIndex == 1 ? -1 : 1);
                for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
                {
                    BlockFacing towardSolid = BlockFacing.HORIZONTALS[i];
                    BlockPos solidPos = hangingSectionPos.AddCopy(towardSolid);
                    solidPos.Y += dy;
                    if (!acc.IsValidPos(solidPos)) continue;

                    Block solid = acc.GetBlock(solidPos);
                    if (!IsStructuralWallHost(solid)) continue;

                    for (int f = 0; f < BlockFacing.HORIZONTALS.Length; f++)
                    {
                        BlockFacing facing = BlockFacing.HORIZONTALS[f];
                        if (IsOutwardSurfaceLatchFace(towardSolid, facing, info)) continue;

                        Block vineSample = ResolveEndBlock(world, info.Tropical, facing);
                        if (!CanHostVine(acc, vineSample, solidPos, facing)) continue;

                        BlockPos candidateVine = VinePosForHost(solidPos, facing);
                        if (!acc.IsValidPos(candidateVine)) continue;
                        if (candidateVine.Equals(hangingSectionPos)) continue;
                        if (!IsNearbySurfaceLatchCell(candidateVine, hangingSectionPos)) continue;
                        if (!PlantVacancyRules.IsVacantPlantSpace(acc.GetBlock(candidateVine))) continue;
                        if (IsVineBlock(acc.GetBlock(candidateVine))) continue;
                        if (!TouchesVineNetworkForSpread(acc, candidateVine, info.Tropical, hangingSectionPos)) continue;

                        int score = System.Math.Abs(dy) * 100;
                        if (facing == info.Facing)
                        {
                            score += 0;
                        }
                        else if (facing == towardSolid.Opposite)
                        {
                            score += 10;
                        }
                        else
                        {
                            score += 40;
                        }

                        if (score >= bestScore) continue;

                        bestScore = score;
                        bestHost = solidPos.Copy();
                        bestFacing = facing;
                        bestVine = candidateVine;
                    }
                }
            }

            if (bestHost == null || bestFacing == null || bestVine == null) return false;

            host = bestHost;
            placedFacing = bestFacing;
            vinePos = bestVine;
            return true;
        }

        /// <summary>Face on the far side of a block beside the hanging column (away from the vine).</summary>
        static bool IsOutwardSurfaceLatchFace(BlockFacing towardSolid, BlockFacing candidateFacing, in WildVineInfo info)
        {
            if (towardSolid == null || candidateFacing == null) return true;
            if (towardSolid == info.Facing || towardSolid == info.Facing.Opposite) return false;

            return candidateFacing == towardSolid;
        }

        public static bool HasStructuralSupportBehind(
            IBlockAccessor acc,
            IWorldAccessor world,
            BlockPos vinePos,
            in WildVineInfo info)
        {
            if (acc == null || world == null || vinePos == null) return false;

            Block vineSample = ResolveEndBlock(world, info.Tropical, info.Facing);
            if (vineSample == null || vineSample.Id == 0) return false;

            BlockPos host = HostPos(vinePos, info.Facing);
            return CanHostVine(acc, vineSample, host, info.Facing);
        }

        /// <summary>Air below is allowed while the column top still attaches to a block; else wall step down.</summary>
        public static bool CanContinueDownward(
            IBlockAccessor acc,
            IWorldAccessor world,
            BlockPos vinePos,
            in WildVineInfo info)
        {
            if (acc == null || world == null || vinePos == null) return false;

            BlockPos below = vinePos.DownCopy();
            if (!acc.IsValidPos(below)) return false;
            if (!PlantVacancyRules.IsVacantPlantSpace(acc.GetBlock(below))) return false;

            if (HasWallContinuationBelow(acc, world, vinePos, info)) return true;

            return WildVineColumnSupport.IsColumnTopAnchored(acc, world, vinePos, info);
        }

        public static bool HasWallContinuationBelow(
            IBlockAccessor acc,
            IWorldAccessor world,
            BlockPos vinePos,
            in WildVineInfo info)
        {
            if (acc == null || world == null || vinePos == null) return false;

            BlockPos belowVine = vinePos.DownCopy();
            if (!acc.IsValidPos(belowVine)) return false;
            if (!PlantVacancyRules.IsVacantPlantSpace(acc.GetBlock(belowVine))) return false;

            Block vineSample = ResolveEndBlock(world, info.Tropical, info.Facing);
            if (vineSample == null || vineSample.Id == 0) return false;

            BlockPos hostBelow = HostPos(belowVine, info.Facing);
            return CanHostVine(acc, vineSample, hostBelow, info.Facing);
        }

        public static bool NeedsTipBelowSection(
            IBlockAccessor acc,
            IWorldAccessor world,
            BlockPos sectionPos,
            in WildVineInfo info)
        {
            if (acc == null || world == null || sectionPos == null) return false;
            if (!IsSectionBlock(acc.GetBlock(sectionPos), info)) return false;

            BlockPos below = sectionPos.DownCopy();
            if (!acc.IsValidPos(below)) return false;
            if (MatchesColumn(acc.GetBlock(below), info)) return false;

            return CanContinueDownward(acc, world, sectionPos, info);
        }

        /// <summary>New vine on a perpendicular corner starts as end when growth can continue below.</summary>
        public static bool ShouldPlaceAsEndAt(
            IBlockAccessor acc,
            IWorldAccessor world,
            BlockPos vinePos,
            in WildVineInfo info)
        {
            if (acc == null || world == null || vinePos == null) return false;

            BlockPos below = vinePos.DownCopy();
            if (acc.IsValidPos(below) && MatchesColumn(acc.GetBlock(below), info)) return false;

            return CanContinueDownward(acc, world, vinePos, info);
        }

        /// <summary>Mobile tip at the wall foot that can seed perpendicular faces at a junction.</summary>
        public static bool CanSeedJunctionFromEnd(
            IBlockAccessor acc,
            IWorldAccessor world,
            BlockPos vinePos,
            in WildVineInfo info)
        {
            if (acc == null || world == null || vinePos == null) return false;
            if (!IsEndBlock(acc.GetBlock(vinePos))) return false;
            if (IsSurfaceAnchorEnd(acc, world, vinePos, info)) return false;
            if (HasWallContinuationBelow(acc, world, vinePos, info)) return false;
            if (!HasStructuralSupportBehind(acc, world, vinePos, info)) return false;

            return true;
        }

        /// <summary>T-branch arms: only section blocks spread sideways.</summary>
        public static bool CanSpreadHorizontallyFrom(
            IBlockAccessor acc,
            IWorldAccessor world,
            BlockPos vinePos,
            in WildVineInfo info)
        {
            if (acc == null || vinePos == null) return false;

            Block block = acc.GetBlock(vinePos);
            return IsSectionBlock(block, info);
        }

        /// <summary>Fixed end at the column top while a lower mobile tip grows below (requires vine below the top end).</summary>
        public static bool IsSurfaceAnchorEnd(
            IBlockAccessor acc,
            IWorldAccessor world,
            BlockPos vinePos,
            in WildVineInfo info)
        {
            if (acc == null || vinePos == null) return false;

            BlockPos top = FindHighestColumnCell(acc, vinePos, info);
            if (vinePos.Y != top.Y || !IsEndBlock(acc.GetBlock(top))) return false;

            BlockPos below = top.DownCopy();
            return acc.IsValidPos(below) && MatchesColumn(acc.GetBlock(below), info);
        }

        /// <summary>Lowest end in the column — the tip that crawls downward.</summary>
        public static bool TryFindMobileTip(IBlockAccessor acc, BlockPos start, in WildVineInfo info, out BlockPos tip)
        {
            tip = null;
            if (acc == null || start == null) return false;

            BlockPos bottom = FindLowestColumnCell(acc, start, info);
            BlockPos top = FindHighestColumnCell(acc, start, info);

            for (int y = bottom.Y; y <= top.Y; y++)
            {
                BlockPos pos = new BlockPos(top.X, y, top.Z);
                if (!IsEndBlock(acc.GetBlock(pos))) continue;

                tip = pos;
                return true;
            }

            return false;
        }

        /// <summary>Keep surface anchor + mobile tip; collapse stray ends to sections.</summary>
        public static void DedupeColumnEnds(IBlockAccessor acc, IWorldAccessor world, BlockPos anyInColumn)
        {
            if (acc == null || world == null || anyInColumn == null) return;

            Block start = acc.GetBlock(anyInColumn);
            if (!TryParse(start, out WildVineInfo info)) return;

            BlockPos top = FindHighestColumnCell(acc, anyInColumn, info);
            BlockPos bottom = FindLowestColumnCell(acc, anyInColumn, info);

            bool hasSurfaceEnd = IsEndBlock(acc.GetBlock(top));
            int surfaceY = hasSurfaceEnd ? top.Y : int.MinValue;

            int lowestEndY = int.MinValue;
            for (int y = bottom.Y; y <= top.Y; y++)
            {
                BlockPos pos = new BlockPos(top.X, y, top.Z);
                if (!IsEndBlock(acc.GetBlock(pos))) continue;
                if (lowestEndY == int.MinValue || y < lowestEndY) lowestEndY = y;
            }

            if (lowestEndY == int.MinValue) return;

            Block sectionBlock = ResolveSectionBlock(world, info.Tropical, info.Facing);
            if (sectionBlock == null || sectionBlock.Id == 0) return;

            for (int y = bottom.Y; y <= top.Y; y++)
            {
                BlockPos pos = new BlockPos(top.X, y, top.Z);
                Block current = acc.GetBlock(pos);
                if (!IsEndBlock(current)) continue;

                if (y == lowestEndY) continue;
                if (hasSurfaceEnd && y == surfaceY) continue;

                acc.SetBlock(sectionBlock.BlockId, pos);
                acc.MarkBlockDirty(pos);
            }
        }

        public static BlockFacing WallTangent(BlockFacing vineFacing)
        {
            if (vineFacing == null) return BlockFacing.EAST;
            return vineFacing.Axis == EnumAxis.Z ? BlockFacing.EAST : BlockFacing.NORTH;
        }

        public static BlockPos HostPos(BlockPos vinePos, BlockFacing vineFacing) =>
            vinePos.Copy().AddCopy(vineFacing.Opposite);

        public static BlockPos VinePosForHost(BlockPos hostPos, BlockFacing vineFacing) =>
            hostPos.Copy().AddCopy(vineFacing);

        public static BlockPos FindLowestEnd(IBlockAccessor acc, BlockPos start, in WildVineInfo info) =>
            TryFindMobileTip(acc, start, info, out BlockPos tip) ? tip : FindLowestColumnCell(acc, start, info);

        public static BlockPos FindLowestColumnCell(IBlockAccessor acc, BlockPos start, in WildVineInfo info)
        {
            if (acc == null || start == null) return start;

            var pos = start.Copy();
            while (acc.IsValidPos(pos.DownCopy()))
            {
                Block below = acc.GetBlock(pos.DownCopy());
                if (!MatchesColumn(below, info)) break;
                pos.Down();
            }

            return pos;
        }

        public static BlockPos FindHighestColumnCell(IBlockAccessor acc, BlockPos start, in WildVineInfo info)
        {
            if (acc == null || start == null) return start;

            var pos = start.Copy();
            while (acc.IsValidPos(pos.UpCopy()))
            {
                Block above = acc.GetBlock(pos.UpCopy());
                if (!MatchesColumn(above, info)) break;
                pos.Up();
            }

            return pos;
        }

        /// <summary>Mobile tip for downward spread, when the column has one.</summary>
        public static bool TryResolveSpreadTip(IBlockAccessor acc, BlockPos anyVinePos, out BlockPos tip)
        {
            tip = null;
            if (acc == null || anyVinePos == null) return false;

            Block block = acc.GetBlock(anyVinePos);
            if (!TryParse(block, out WildVineInfo info)) return false;

            return TryFindMobileTip(acc, anyVinePos, info, out tip);
        }

        /// <summary>Registry anchor: mobile tip when present, otherwise lowest column cell.</summary>
        public static bool TryResolveSpreadAnchor(
            IBlockAccessor acc,
            IWorldAccessor world,
            BlockPos anyVinePos,
            out BlockPos anchor)
        {
            anchor = null;
            if (acc == null || anyVinePos == null) return false;

            Block block = acc.GetBlock(anyVinePos);
            if (!TryParse(block, out WildVineInfo info)) return false;

            if (TryFindMobileTip(acc, anyVinePos, info, out anchor)) return true;

            anchor = FindLowestColumnCell(acc, anyVinePos, info);
            return IsVineBlock(acc.GetBlock(anchor));
        }

        public static bool ColumnHasRegistryEntry(
            IBlockAccessor acc,
            BlockPos pos,
            in WildVineInfo info,
            System.Func<BlockPos, bool> isRegistered)
        {
            if (acc == null || pos == null || isRegistered == null) return false;

            BlockPos top = FindHighestColumnCell(acc, pos, info);
            BlockPos bottom = FindLowestColumnCell(acc, pos, info);
            for (int y = bottom.Y; y <= top.Y; y++)
            {
                if (isRegistered(new BlockPos(top.X, y, top.Z))) return true;
            }

            return false;
        }

        static BlockFacing ResolveFacing(Block block, string path)
        {
            if (block?.Variant != null
                && block.Variant.TryGetValue("horizontalorientation", out string orient)
                && !string.IsNullOrEmpty(orient))
            {
                BlockFacing fromVariant = BlockFacing.FromCode(orient);
                if (fromVariant != null) return fromVariant;
            }

            int lastDash = path.LastIndexOf('-');
            if (lastDash < 0 || lastDash >= path.Length - 1) return null;

            return BlockFacing.FromCode(path.Substring(lastDash + 1));
        }
    }
}
