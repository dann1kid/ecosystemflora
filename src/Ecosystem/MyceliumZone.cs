using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Soft niche around vanilla <see cref="BlockEntityMycelium"/> anchors (growRange 7).
    /// Hard spread block on the anchor cell stays in <see cref="WildSoilGroundRules"/>,
    /// except meadow flora over meadow mycelium (<see cref="MyceliumCoexistence"/>).
    /// </summary>
    internal static class MyceliumZone
    {
        /// <summary>Vanilla <c>BlockEntityMycelium.growRange</c>.</summary>
        public const int VanillaGrowRange = 7;

        public static float ApplySpreadFitness(
            ICoreAPI api,
            PlantRequirements req,
            BlockPos plantPos,
            float baseFitness)
        {
            if (req == null || plantPos == null || baseFitness <= 0f) return baseFitness;
            if (req.Habitat != EcologyHabitat.Terrestrial) return baseFitness;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EnableMyceliumNiche || api?.World?.BlockAccessor == null) return baseFitness;
            if (string.IsNullOrEmpty(req.Species)) return baseFitness;
            if (!WildSpeciesSoilSuccession.TryGetRole(req.Species, out PlantSoilRole role)) return baseFitness;

            int radius = cfg.MyceliumZoneRadius > 0 ? cfg.MyceliumZoneRadius : VanillaGrowRange;
            BlockPos groundPos = plantPos.DownCopy();
            if (!TryGetNearestAnchorNiche(api.World.BlockAccessor, groundPos, radius, out int distance, out MyceliumNiche nearestNiche))
            {
                return baseFitness;
            }

            float mult = SpreadMultiplierForRole(
                role,
                distance,
                radius,
                nearestNiche,
                cfg.MyceliumMeadowSpreadPenalty,
                cfg.MyceliumForestSpreadBonus);

            return baseFitness * mult;
        }

        public static bool TryGetNearestActiveDistance(
            IBlockAccessor acc,
            BlockPos groundPos,
            int maxRadius,
            out int distance)
        {
            return TryGetNearestAnchorNiche(acc, groundPos, maxRadius, out distance, out _);
        }

        public static bool TryGetNearestAnchorNiche(
            IBlockAccessor acc,
            BlockPos groundPos,
            int maxRadius,
            out int distance,
            out MyceliumNiche niche)
        {
            distance = -1;
            niche = MyceliumNiche.ForestAnyTree;
            if (acc == null || groundPos == null || maxRadius < 0) return false;

            if (MyceliumCoexistence.TryGetAnchorNiche(acc, groundPos, out niche))
            {
                distance = 0;
                return true;
            }

            int best = maxRadius + 1;
            MyceliumNiche bestNiche = niche;
            BlockPos probe = new BlockPos(groundPos.dimension);
            int y = groundPos.Y;

            for (int dx = -maxRadius; dx <= maxRadius; dx++)
            {
                for (int dz = -maxRadius; dz <= maxRadius; dz++)
                {
                    if (dx == 0 && dz == 0) continue;

                    int dist = Math.Max(Math.Abs(dx), Math.Abs(dz));
                    if (dist > maxRadius || dist >= best) continue;

                    probe.Set(groundPos.X + dx, y, groundPos.Z + dz);
                    if (MyceliumCoexistence.TryGetAnchorNiche(acc, probe, out MyceliumNiche probeNiche))
                    {
                        best = dist;
                        bestNiche = probeNiche;
                    }
                }
            }

            if (best <= maxRadius)
            {
                distance = best;
                niche = bestNiche;
                return true;
            }

            return false;
        }

        internal static float SpreadMultiplierForRole(
            PlantSoilRole role,
            int distance,
            int zoneRadius,
            MyceliumNiche nearestNiche,
            float meadowPenaltyAtZero,
            float forestBonusAtZero)
        {
            if (distance < 0 || zoneRadius <= 0 || distance > zoneRadius) return 1f;

            if (nearestNiche == MyceliumNiche.MeadowOpen && role.IsMeadowRole())
            {
                return 1f;
            }

            float t = distance / (float)zoneRadius;

            if (role.IsMeadowRole() && MyceliumCoexistence.IsForestMyceliumNiche(nearestNiche))
            {
                float floor = ClampMultiplier(meadowPenaltyAtZero, 0.05f, 1f);
                return floor + (1f - floor) * t;
            }

            if (role.IsForestRole() && MyceliumCoexistence.IsForestMyceliumNiche(nearestNiche))
            {
                float peak = ClampMultiplier(forestBonusAtZero, 1f, 2f);
                return 1f + (peak - 1f) * (1f - t);
            }

            return 1f;
        }

        static float ClampMultiplier(float value, float min, float max) =>
            value < min ? min : value > max ? max : value;
    }
}
