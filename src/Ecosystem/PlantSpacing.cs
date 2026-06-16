using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Enforces minimum distance to existing ecology flowers when spreading.</summary>
    public static class PlantSpacing
    {
        public static bool MeetsSpacing(
            IBlockAccessor acc,
            BlockPos candidatePos,
            PlantRequirements requirements,
            out string failureReason)
        {
            failureReason = null;
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.PlantSpacingEnabled) return true;
            if (string.IsNullOrEmpty(requirements.Species)) return true;

            int searchRadius = requirements.GetSpacingSearchRadius(cfg);
            if (searchRadius <= 0) return true;

            EcologySpacingIndex index = EcosystemSystem.Instance?.SpacingIndex;
            if (index != null)
            {
                return index.MeetsSpacing(candidatePos, requirements, cfg, out failureReason);
            }

            return MeetsSpacingBruteForce(acc, candidatePos, requirements, cfg, searchRadius, out failureReason);
        }

        static bool MeetsSpacingBruteForce(
            IBlockAccessor acc,
            BlockPos candidatePos,
            PlantRequirements requirements,
            EcosystemConfig cfg,
            int searchRadius,
            out string failureReason)
        {
            failureReason = null;
            int y0 = candidatePos.Y - cfg.SpacingVerticalSearch;
            int y1 = candidatePos.Y + cfg.SpacingVerticalSearch;

            for (int x = candidatePos.X - searchRadius; x <= candidatePos.X + searchRadius; x++)
            {
                for (int z = candidatePos.Z - searchRadius; z <= candidatePos.Z + searchRadius; z++)
                {
                    for (int y = y0; y <= y1; y++)
                    {
                        if (x == candidatePos.X && y == candidatePos.Y && z == candidatePos.Z) continue;

                        BlockPos checkPos = new BlockPos(x, y, z);
                        Block block = acc.GetBlock(checkPos);
                        if (!PlantCodeHelper.IsEcologyPlant(block)) continue;

                        string otherSpecies = PlantCodeHelper.ResolveEcologySpecies(block);
                        if (string.IsNullOrEmpty(otherSpecies)) continue;

                        EcologyHabitat otherHabitat = PlantCodeHelper.GetEcologyHabitat(block.Code);
                        if (!ShouldApplySpacingBetween(requirements.Habitat, otherHabitat, cfg))
                        {
                            continue;
                        }

                        int required = requirements.GetRequiredSpacingTo(otherSpecies, cfg);
                        if (required <= 0) continue;

                        int dist = HorizontalChebyshev(candidatePos, checkPos);
                        bool sameColumn = candidatePos.X == checkPos.X && candidatePos.Z == checkPos.Z;

                        if (sameColumn && otherSpecies == requirements.Species
                            && PlantCodeHelper.IsReedBlock(block))
                        {
                            failureReason = "Reed already in column at y=" + checkPos.Y;
                            return false;
                        }

                        if (dist < required)
                        {
                            failureReason = "Too close to " + otherSpecies
                                + " (dist " + dist + ", need " + required + ")";
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        internal static bool ShouldApplySpacingBetween(
            EcologyHabitat candidateHabitat,
            EcologyHabitat neighborHabitat,
            EcosystemConfig cfg)
        {
            if (cfg != null && cfg.ApplyCrossHabitatSpacing)
            {
                return true;
            }

            return GetSpacingGroup(candidateHabitat) == GetSpacingGroup(neighborHabitat);
        }

        static int GetSpacingGroup(EcologyHabitat habitat)
        {
            switch (habitat)
            {
                case EcologyHabitat.ReedNearWater:
                case EcologyHabitat.WaterSurface:
                case EcologyHabitat.UnderwaterColumn:
                    return 1;
                case EcologyHabitat.TerrestrialTree:
                case EcologyHabitat.Ferntree:
                    return 2;
                default:
                    return 0;
            }
        }

        static int HorizontalChebyshev(BlockPos a, BlockPos b)
        {
            int dx = System.Math.Abs(a.X - b.X);
            int dz = System.Math.Abs(a.Z - b.Z);
            return System.Math.Max(dx, dz);
        }
    }
}
