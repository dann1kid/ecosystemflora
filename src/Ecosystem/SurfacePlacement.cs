using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class SurfacePlacement
    {
        static readonly BlockPos scanPos = new BlockPos(0);
        static readonly BlockPos groundScratch = new BlockPos(0);

        public static bool TryFindPlantPos(
            IBlockAccessor acc,
            BlockPos origin,
            int dx,
            int dz,
            int verticalSearch,
            out BlockPos plantPos,
            out string failureReason,
            PlantRequirements requirements = null)
        {
            plantPos = null;
            failureReason = null;

            int x = origin.X + dx;
            int z = origin.Z + dz;
            BlockPos best = null;
            int bestDist = int.MaxValue;

            for (int dy = verticalSearch; dy >= -verticalSearch; dy--)
            {
                int y = origin.Y + dy;
                scanPos.Set(x, y, z);
                if (!TryValidatePlantSite(acc, scanPos, requirements, out _)) continue;

                int dist = System.Math.Abs(dy);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = scanPos.Copy();
                }
            }

            if (best == null)
            {
                scanPos.Set(x, origin.Y, z);
                TryValidatePlantSite(acc, scanPos, requirements, out string atSameY);
                failureReason =
                    $"No valid surface in column [{x},{z}] dy=±{verticalSearch}; dy=0: {atSameY}";
                return false;
            }

            plantPos = best;
            return true;
        }

        /// <summary>True when a terrestrial plant cell is physically valid (cheap block gates).</summary>
        public static bool IsValidPlantSite(IBlockAccessor acc, BlockPos pos, PlantRequirements requirements = null) =>
            TryValidatePlantSite(acc, pos, requirements, out _);

        /// <summary>
        /// Topmost valid surface cell in a column below <paramref name="from"/> (first hit when scanning down).
        /// Air-only placement for spread diagnostics; fallen sticks use <see cref="CanopyFallenSticks.TryFindGroundStickCell"/>.
        /// </summary>
        public static bool TryFindSurfaceCellBelow(
            IBlockAccessor acc,
            BlockPos from,
            int maxDropBlocks,
            out BlockPos surfacePos,
            PlantRequirements requirements = null)
        {
            surfacePos = null;
            if (acc == null || from == null || maxDropBlocks <= 0) return false;

            int minY = from.Y - maxDropBlocks;
            if (minY < 0) minY = 0;

            for (int y = from.Y - 1; y >= minY; y--)
            {
                scanPos.Set(from.X, y, from.Z);
                if (!acc.IsValidPos(scanPos)) break;
                if (!TryValidatePlantSite(acc, scanPos, requirements, out _)) continue;

                surfacePos = scanPos.Copy();
                return true;
            }

            return false;
        }

        /// <summary>Returns false with a concise reason suitable for diagnostics (VerboseLogging).</summary>
        static bool TryValidatePlantSite(
            IBlockAccessor acc,
            BlockPos pos,
            PlantRequirements requirements,
            out string rejectReason)
        {
            rejectReason = null;
            Block space = acc.GetBlock(pos);
            groundScratch.Set(pos.X, pos.Y - 1, pos.Z);
            Block ground = acc.GetBlock(groundScratch);

            Block fluidAt = acc.GetBlock(pos, BlockLayersAccess.Fluid);
            Block fluidBelow = acc.GetBlock(groundScratch, BlockLayersAccess.Fluid);
            if (PlantVacancyRules.TouchesSpreadBlockingFluid(space, ground, fluidAt, fluidBelow))
            {
                if (BlockFluidHelper.IsFluid(space))
                    rejectReason = $"space is fluid ({space.Code})";
                else if (BlockFluidHelper.IsFluid(ground))
                    rejectReason = $"ground is fluid ({ground.Code})";
                else if (fluidAt != null && fluidAt.Id != 0 && BlockFluidHelper.IsFluid(fluidAt))
                    rejectReason = $"fluidAt layer ({fluidAt.Code})";
                else
                    rejectReason = $"fluidBelow layer ({fluidBelow?.Code})";
                return false;
            }

            if (!PlantVacancyRules.IsSupportingGround(ground))
            {
                rejectReason =
                    $"ground up not solid ({ground.Code?.Path}, SideSolid.UP={ground.SideSolid[BlockFacing.UP.Index]}, farmland={WildSoilGroundRules.IsFarmland(ground)})";
                return false;
            }

            if (WildSoilGroundRules.HasActiveMycelium(acc, groundScratch)
                && !MyceliumCoexistence.AllowsMeadowFloraOverMycelium(acc, groundScratch, requirements))
            {
                rejectReason = $"active mycelium BE under cell ({ground.Code?.Path})";
                return false;
            }

            if (PlantVacancyRules.IsVacantPlantSpace(space))
            {
                return true;
            }

            string path = space.Code?.Path ?? ("id=" + space.Id);

            if (PlantCodeHelper.IsEcologySpreadParent(space))
            {
                if (PlantCodeHelper.IsArborealHostBlock(space))
                {
                    rejectReason = $"space is tree trunk ({path})";
                    return false;
                }

                return true;
            }

            int rep = space.Replaceable;
            int minRep = SuitabilityEvaluator.ReproduceMinReplaceable;
            rejectReason = $"space replaceable {rep} < {minRep} ({path})";
            return false;
        }

        /// <summary>Logs every Y in the vertical search window (VerboseLogging + ReproduceDebug).</summary>
        public static void LogColumnDyProbe(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos origin,
            int dx,
            int dz,
            int verticalSearch,
            string label)
        {
            if (api == null || acc == null || origin == null) return;
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.VerboseLogging || !cfg.ReproduceDebug) return;

            int x = origin.X + dx;
            int z = origin.Z + dz;

            api.Logger.Notification(
                "[ecosystemflora] surface column probe {0} origin={1} column=[{2},{3}] dy=±{4}",
                label ?? "?",
                origin,
                x,
                z,
                verticalSearch);

            for (int dy = verticalSearch; dy >= -verticalSearch; dy--)
            {
                int y = origin.Y + dy;
                scanPos.Set(x, y, z);
                bool ok = TryValidatePlantSite(acc, scanPos, null, out string reason);
                Block space = acc.GetBlock(scanPos);
                groundScratch.Set(x, y - 1, z);
                Block ground = acc.GetBlock(groundScratch);

                api.Logger.Notification(
                    "[ecosystemflora]   dy={0,3} y={1,4} space={2} rep={3} ground={4} solidUp={5} -> {6}",
                    dy,
                    y,
                    space.Id == 0 ? "air" : space.Code?.Path ?? ("id=" + space.Id),
                    space.Replaceable,
                    ground.Code?.Path ?? ("id=" + ground.Id),
                    ground.SideSolid[BlockFacing.UP.Index],
                    ok ? "OK" : reason);
            }
        }
    }
}
