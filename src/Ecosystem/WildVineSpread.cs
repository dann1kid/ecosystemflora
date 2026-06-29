using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>T-shaped spread: surface end stays, mobile tip crawls down leaving sections; sections branch sideways and wrap corners.</summary>
    internal static class WildVineSpread
    {
        static readonly BlockPos scratch = new BlockPos(0);
        static readonly BlockPos scratchHost = new BlockPos(0);

        const int NetworkSectionScanLimit = 48;

        public static bool TrySpread(EcosystemSystem eco, ReproducerEntry entry, ICoreAPI api, EcosystemConfig cfg)
        {
            if (eco == null || entry == null || api == null || cfg == null) return false;
            if (!cfg.EnableWildVineEcology) return false;

            IBlockAccessor acc = api.World.BlockAccessor;
            Block vineBlock = acc.GetBlock(entry.Origin);
            if (!WildVineHelper.TryParse(vineBlock, out WildVineInfo info)) return false;

            WildVineHelper.DedupeColumnEnds(acc, api.World, entry.Origin);

            int maxHangDepth = cfg.WildVineMaxHangDepth;

            if (WildVineHelper.TryFindMobileTip(acc, entry.Origin, info, out BlockPos mobileTip)
                && WildVineHelper.CanContinueDownward(acc, api.World, mobileTip, info, maxHangDepth)
                && TryExtendDown(eco, api, acc, mobileTip, info, maxHangDepth))
            {
                return true;
            }

            if (TrySpawnTipsBelowNetworkSections(eco, api, acc, entry.Origin, info, maxHangDepth))
            {
                return true;
            }

            return TryHorizontalSpread(eco, api, acc, entry.Origin, info);
        }

        static bool TryExtendDown(
            EcosystemSystem eco,
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos tip,
            in WildVineInfo info,
            int maxHangDepth)
        {
            if (!LandClaimGuard.AllowsEcologyChange(api, tip)) return false;

            BlockPos below = tip.DownCopy();
            if (!acc.IsValidPos(below)) return false;
            if (!LandClaimGuard.AllowsEcologyChange(api, below)) return false;

            if (!WildVineHelper.CanContinueDownward(acc, api.World, tip, info, maxHangDepth)) return false;

            Block section = WildVineHelper.ResolveSectionBlock(api.World, info.Tropical, info.Facing);
            Block end = WildVineHelper.ResolveEndBlock(api.World, info.Tropical, info.Facing);
            if (section == null || end == null || section.Id == 0 || end.Id == 0) return false;

            bool keepSurfaceEnd = WildVineHelper.IsSurfaceAnchorEnd(acc, api.World, tip, info);
            if (!keepSurfaceEnd)
            {
                acc.SetBlock(section.BlockId, tip);
                acc.MarkBlockDirty(tip);
            }

            acc.SetBlock(end.BlockId, below);
            acc.MarkBlockDirty(below);

            eco.RelocateVineTip(tip, below);
            return true;
        }

        static bool TryHorizontalSpread(
            EcosystemSystem eco,
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos origin,
            in WildVineInfo startInfo)
        {
            var visited = new HashSet<BlockPos>();
            var queue = new Queue<BlockPos>();
            queue.Enqueue(origin.Copy());
            visited.Add(origin);

            int scanned = 0;
            while (queue.Count > 0 && scanned < NetworkSectionScanLimit)
            {
                scanned++;
                BlockPos pos = queue.Dequeue();
                Block block = acc.GetBlock(pos);
                if (!WildVineHelper.TryParse(block, out WildVineInfo info)) continue;

                if (WildVineHelper.IsSectionBlock(block, info))
                {
                    if (TrySpreadFromSection(eco, api, acc, pos, info))
                    {
                        return true;
                    }
                }
                else if (WildVineHelper.CanSeedJunctionFromEnd(acc, api.World, pos, info)
                         && TrySpreadJunctionFromEnd(eco, api, acc, pos, info))
                {
                    return true;
                }

                EnqueueNetworkNeighbors(acc, pos, startInfo.Tropical, visited, queue);
            }

            return false;
        }

        static bool TrySpreadFromSection(
            EcosystemSystem eco,
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos sectionPos,
            in WildVineInfo info)
        {
            if (!WildVineHelper.HasStructuralSupportBehind(acc, api.World, sectionPos, info))
            {
                return TrySpreadHangingSectionToNearbySurface(eco, api, acc, sectionPos, info);
            }

            BlockFacing tangent = WildVineHelper.WallTangent(info.Facing);

            for (int step = -1; step <= 1; step += 2)
            {
                if (TryPlaceOnSameWall(eco, api, acc, sectionPos, info, tangent, step))
                {
                    return true;
                }
            }

            return TrySpreadAdjacentFaces(eco, api, acc, sectionPos, info, includeAlongWallSteps: true);
        }

        static bool TrySpreadHangingSectionToNearbySurface(
            EcosystemSystem eco,
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos sectionPos,
            in WildVineInfo info)
        {
            if (!WildVineHelper.TryResolveNearbySurfaceLatch(
                    acc,
                    api.World,
                    sectionPos,
                    info,
                    out BlockPos host,
                    out BlockFacing facing,
                    out BlockPos _))
            {
                return false;
            }

            return TryPlaceVine(
                eco,
                api,
                acc,
                host,
                facing,
                info,
                requireColumnTouch: false,
                sourceAnchor: sectionPos,
                allowCornerEnd: true);
        }

        static bool TrySpreadJunctionFromEnd(
            EcosystemSystem eco,
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos endPos,
            in WildVineInfo info)
        {
            return TrySpreadAdjacentFaces(eco, api, acc, endPos, info, includeAlongWallSteps: false);
        }

        static bool TrySpreadAdjacentFaces(
            EcosystemSystem eco,
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos sourcePos,
            in WildVineInfo info,
            bool includeAlongWallSteps)
        {
            BlockFacing[] adjacentFacings = WildVineHelper.PerpendicularHorizontalFacings(info.Facing);
            if (adjacentFacings.Length == 0) return false;

            for (int dy = -1; dy <= 1; dy++)
            {
                foreach (BlockFacing adjacentFacing in adjacentFacings)
                {
                    if (TryPlaceOnAdjacentFace(eco, api, acc, sourcePos, info, adjacentFacing, alongWallStep: 0, dy))
                    {
                        return true;
                    }
                }
            }

            if (!includeAlongWallSteps) return false;

            for (int dy = -1; dy <= 1; dy++)
            {
                foreach (BlockFacing adjacentFacing in adjacentFacings)
                {
                    for (int step = -1; step <= 1; step += 2)
                    {
                        if (TryPlaceOnAdjacentFace(eco, api, acc, sourcePos, info, adjacentFacing, step, dy))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        static bool TryPlaceOnSameWall(
            EcosystemSystem eco,
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos sourcePos,
            in WildVineInfo sourceInfo,
            BlockFacing tangent,
            int tangentStep)
        {
            BlockPos host = WildVineHelper.HostPos(sourcePos, sourceInfo.Facing);
            WildVineSpreadGeometry.OffsetTangent(host, tangent, tangentStep, 0, scratchHost);
            host = scratchHost;

            return TryPlaceVine(
                eco,
                api,
                acc,
                host,
                sourceInfo.Facing,
                sourceInfo,
                requireColumnTouch: true);
        }

        static bool TryPlaceOnAdjacentFace(
            EcosystemSystem eco,
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos sourcePos,
            in WildVineInfo sourceInfo,
            BlockFacing adjacentFacing,
            int alongWallStep,
            int dy)
        {
            if (!WildVineHelper.TryResolveAdjacentFacePlacement(
                    acc,
                    sourcePos,
                    sourceInfo,
                    adjacentFacing,
                    alongWallStep,
                    dy,
                    scratchHost,
                    out BlockPos _,
                    out int rimWalkScore))
            {
                return false;
            }

            BlockPos host = scratchHost.Copy();
            return TryPlaceVine(
                eco,
                api,
                acc,
                host,
                adjacentFacing,
                sourceInfo,
                requireColumnTouch: false,
                sourceAnchor: sourcePos,
                allowCornerEnd: alongWallStep == 0,
                rimWalkScore: rimWalkScore);
        }

        static bool TryPlaceVine(
            EcosystemSystem eco,
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos host,
            BlockFacing facing,
            in WildVineInfo sourceInfo,
            bool requireColumnTouch,
            BlockPos sourceAnchor = null,
            bool allowCornerEnd = false,
            int rimWalkScore = 0)
        {
            Block end = WildVineHelper.ResolveEndBlock(api.World, sourceInfo.Tropical, facing);
            if (end == null || end.Id == 0) return false;
            if (!WildVineHelper.CanHostVine(acc, end, host, facing)) return false;

            BlockPos newVine = WildVineHelper.VinePosForHost(host, facing);
            if (!acc.IsValidPos(newVine)) return false;
            if (!LandClaimGuard.AllowsEcologyChange(api, newVine)) return false;

            Block space = acc.GetBlock(newVine);
            if (!PlantVacancyRules.IsVacantPlantSpace(space)) return false;
            if (WildVineHelper.IsVineBlock(space)) return false;

            var placedInfo = new WildVineInfo(sourceInfo.Tropical, isEnd: false, facing);

            if (requireColumnTouch)
            {
                if (!WildVineHelper.TouchesVineColumn(acc, newVine, sourceInfo)) return false;
            }
            else if (!WildVineHelper.TouchesVineNetworkForSpread(
                         acc,
                         newVine,
                         sourceInfo.Tropical,
                         sourceAnchor,
                         rimWalkScore))
            {
                return false;
            }

            bool placeAsEnd = allowCornerEnd
                && WildVineHelper.ShouldPlaceAsEndAt(acc, api.World, newVine, placedInfo);
            Block section = WildVineHelper.ResolveSectionBlock(api.World, sourceInfo.Tropical, facing);
            if (section == null || section.Id == 0) return false;

            Block block = placeAsEnd ? end : section;
            acc.SetBlock(block.BlockId, newVine);
            acc.MarkBlockDirty(newVine);

            if (placeAsEnd)
            {
                eco.TryRegisterVineSpreadTip(newVine);
            }
            else if (WildVineHelper.TryParse(acc.GetBlock(newVine), out WildVineInfo spawnedInfo))
            {
                TrySpawnTipBelowSection(eco, api, acc, newVine, spawnedInfo, EcosystemConfig.Loaded.WildVineMaxHangDepth);
            }

            return true;
        }

        static bool TrySpawnTipsBelowNetworkSections(
            EcosystemSystem eco,
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos origin,
            in WildVineInfo startInfo,
            int maxHangDepth)
        {
            var visited = new HashSet<BlockPos>();
            var queue = new Queue<BlockPos>();
            queue.Enqueue(origin.Copy());
            visited.Add(origin);

            int scanned = 0;
            while (queue.Count > 0 && scanned < NetworkSectionScanLimit)
            {
                scanned++;
                BlockPos pos = queue.Dequeue();
                Block block = acc.GetBlock(pos);
                if (!WildVineHelper.TryParse(block, out WildVineInfo info)) continue;

                if (WildVineHelper.IsSectionBlock(block, info)
                    && TrySpawnTipBelowSection(eco, api, acc, pos, info, maxHangDepth))
                {
                    return true;
                }

                EnqueueNetworkNeighbors(acc, pos, startInfo.Tropical, visited, queue);
            }

            return false;
        }

        static void EnqueueNetworkNeighbors(
            IBlockAccessor acc,
            BlockPos pos,
            bool tropical,
            HashSet<BlockPos> visited,
            Queue<BlockPos> queue)
        {
            for (int i = 0; i < BlockFacing.ALLFACES.Length; i++)
            {
                BlockFacing face = BlockFacing.ALLFACES[i];
                BlockPos neighbor = pos.AddCopy(face);
                if (!acc.IsValidPos(neighbor) || visited.Contains(neighbor)) continue;

                if (!WildVineHelper.TryParse(acc.GetBlock(neighbor), out WildVineInfo neighborInfo)) continue;
                if (neighborInfo.Tropical != tropical) continue;

                visited.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }

        static bool TrySpawnTipBelowSection(
            EcosystemSystem eco,
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos sectionPos,
            in WildVineInfo info,
            int maxHangDepth)
        {
            if (!WildVineHelper.NeedsTipBelowSection(acc, api.World, sectionPos, info, maxHangDepth)) return false;

            BlockPos below = sectionPos.DownCopy();
            if (!LandClaimGuard.AllowsEcologyChange(api, below)) return false;

            Block end = WildVineHelper.ResolveEndBlock(api.World, info.Tropical, info.Facing);
            if (end == null || end.Id == 0) return false;

            acc.SetBlock(end.BlockId, below);
            acc.MarkBlockDirty(below);

            eco.TryRegisterVineSpreadTip(below);
            return true;
        }
    }

    internal static class WildVineSpreadGeometry
    {
        public static void OffsetTangent(BlockPos origin, BlockFacing tangent, int steps, int dy, BlockPos dest)
        {
            int dx = 0;
            int dz = 0;
            if (tangent == BlockFacing.EAST) dx = steps;
            else if (tangent == BlockFacing.WEST) dx = -steps;
            else if (tangent == BlockFacing.NORTH) dz = -steps;
            else if (tangent == BlockFacing.SOUTH) dz = steps;

            dest.Set(origin.X + dx, origin.Y + dy, origin.Z + dz);
        }
    }
}
