using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Wild vine reproduce steps: extend tip downward, then capture adjacent wall faces.</summary>
    internal static class WildVineSpread
    {
        static readonly BlockPos scratch = new BlockPos(0);

        public static bool TrySpread(EcosystemSystem eco, ReproducerEntry entry, ICoreAPI api, EcosystemConfig cfg)
        {
            if (eco == null || entry == null || api == null || cfg == null) return false;
            if (!cfg.EnableWildVineEcology) return false;

            IBlockAccessor acc = api.World.BlockAccessor;
            Block vineBlock = acc.GetBlock(entry.Origin);
            if (!WildVineHelper.IsEndBlock(vineBlock)) return false;
            if (!WildVineHelper.TryParse(vineBlock, out WildVineInfo info)) return false;

            BlockPos tip = WildVineHelper.FindLowestEnd(acc, entry.Origin, info);
            Block tipBlock = acc.GetBlock(tip);
            if (!WildVineHelper.IsEndBlock(tipBlock)) return false;

            if (TryExtendDown(eco, api, acc, entry, tip, info))
            {
                return true;
            }

            return TryCaptureWallFaces(eco, api, acc, entry, tip, info, cfg);
        }

        static bool TryExtendDown(
            EcosystemSystem eco,
            ICoreAPI api,
            IBlockAccessor acc,
            ReproducerEntry entry,
            BlockPos tip,
            in WildVineInfo info)
        {
            if (!LandClaimGuard.AllowsEcologyChange(api, tip)) return false;

            BlockPos below = tip.DownCopy();
            if (!acc.IsValidPos(below)) return false;
            if (!LandClaimGuard.AllowsEcologyChange(api, below)) return false;

            Block belowSpace = acc.GetBlock(below);
            if (!PlantVacancyRules.IsVacantPlantSpace(belowSpace)) return false;

            Block vineSample = WildVineHelper.ResolveEndBlock(api.World, info.Tropical, info.Facing);
            BlockPos hostPos = WildVineHelper.HostPos(below, info.Facing);
            if (!WildVineHelper.CanHostVine(acc, vineSample, hostPos, info.Facing)) return false;

            Block section = WildVineHelper.ResolveSectionBlock(api.World, info.Tropical, info.Facing);
            Block end = vineSample;
            if (section == null || end == null || section.Id == 0 || end.Id == 0) return false;

            acc.SetBlock(section.BlockId, tip);
            acc.MarkBlockDirty(tip);

            acc.SetBlock(end.BlockId, below);
            acc.MarkBlockDirty(below);

            eco.RelocateVineTip(tip, below);
            return true;
        }

        static bool TryCaptureWallFaces(
            EcosystemSystem eco,
            ICoreAPI api,
            IBlockAccessor acc,
            ReproducerEntry entry,
            BlockPos tip,
            in WildVineInfo info,
            EcosystemConfig cfg)
        {
            BlockPos anchorHost = WildVineHelper.HostPos(tip, info.Facing);
            int radius = cfg.WildVineWallCaptureRadius > 0 ? cfg.WildVineWallCaptureRadius : 4;
            int height = cfg.WildVineWallCaptureHeight > 0 ? cfg.WildVineWallCaptureHeight : 6;

            for (int faceIndex = 0; faceIndex < BlockFacing.HORIZONTALS.Length; faceIndex++)
            {
                BlockFacing face = BlockFacing.HORIZONTALS[faceIndex];
                if (TryPlaceOnFace(eco, api, acc, entry, anchorHost, face, info, radius, height))
                {
                    return true;
                }
            }

            return false;
        }

        static bool TryPlaceOnFace(
            EcosystemSystem eco,
            ICoreAPI api,
            IBlockAccessor acc,
            ReproducerEntry entry,
            BlockPos anchorHost,
            BlockFacing face,
            in WildVineInfo info,
            int radius,
            int height)
        {
            Block vineSample = WildVineHelper.ResolveEndBlock(api.World, info.Tropical, face);
            if (vineSample == null || vineSample.Id == 0) return false;

            int halfHeight = height / 2;
            for (int dy = -halfHeight; dy <= halfHeight; dy++)
            {
                for (int reach = 0; reach <= radius; reach++)
                {
                    BlockFacing tangent = PerpendicularTangent(face, reach % 2 == 1);
                    int tangentStep = (reach + 1) / 2;
                    if (reach % 2 == 1) tangentStep = -tangentStep;

                    OffsetTangent(anchorHost, tangent, tangentStep, dy, scratch);

                    if (!acc.IsValidPos(scratch)) continue;

                    Block host = acc.GetBlock(scratch);
                    if (host.Id == 0) continue;
                    if (!WildVineHelper.CanHostVine(acc, vineSample, scratch, face)) continue;

                    BlockPos vinePos = WildVineHelper.VinePosForHost(scratch, face);
                    if (!acc.IsValidPos(vinePos)) continue;
                    if (!LandClaimGuard.AllowsEcologyChange(api, vinePos)) continue;

                    Block space = acc.GetBlock(vinePos);
                    if (!PlantVacancyRules.IsVacantPlantSpace(space)) continue;
                    if (WildVineHelper.IsVineBlock(space)) continue;

                    acc.SetBlock(vineSample.BlockId, vinePos);
                    acc.MarkBlockDirty(vinePos);

                    if (EcosystemParticipant.TryFromBlock(vineSample, out IEcosystemParticipant participant))
                    {
                        eco.RegisterReproducer(vinePos, participant, spawnBurst: false);
                    }

                    return true;
                }
            }

            return false;
        }

        static void OffsetTangent(BlockPos origin, BlockFacing tangent, int steps, int dy, BlockPos dest)
        {
            int dx = 0;
            int dz = 0;
            if (tangent == BlockFacing.EAST) dx = steps;
            else if (tangent == BlockFacing.WEST) dx = -steps;
            else if (tangent == BlockFacing.NORTH) dz = -steps;
            else if (tangent == BlockFacing.SOUTH) dz = steps;

            dest.Set(origin.X + dx, origin.Y + dy, origin.Z + dz);
        }

        static BlockFacing PerpendicularTangent(BlockFacing face, bool alternate)
        {
            if (face.Axis == EnumAxis.Z)
            {
                return alternate ? BlockFacing.EAST : BlockFacing.WEST;
            }

            return alternate ? BlockFacing.NORTH : BlockFacing.SOUTH;
        }
    }
}
