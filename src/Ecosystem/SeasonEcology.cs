using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Calendar season multipliers for spread attempts and stress survival.</summary>
    internal static class SeasonEcology
    {
        public static float SpreadActivityMultiplier(ICoreAPI api, BlockPos pos, PlantRequirements requirements)
        {
            if (!EcosystemConfig.Loaded.UseSeasonalEcology) return 1f;
            if (api?.World?.Calendar == null || requirements == null || pos == null) return 1f;

            WildSpeciesSeason.Profile profile = WildSpeciesSeason.Resolve(requirements.Species);
            EnumSeason season = api.World.Calendar.GetSeason(pos);
            float mult = profile.SpreadMultiplier(season);

            if (season == EnumSeason.Spring)
            {
                float rel = api.World.Calendar.GetSeasonRel(pos);
                mult *= SpringRamp(rel);
            }

            return Clamp(mult, 0f, 3f);
        }

        /// <summary>Extra failed survival tick from season (winter kill, fall die-off).</summary>
        public static bool RollSeasonalStressFailure(ICoreAPI api, BlockPos pos, PlantRequirements requirements)
        {
            if (!EcosystemConfig.Loaded.UseSeasonalEcology) return false;
            if (!EcosystemConfig.Loaded.SeasonalStressEnabled) return false;
            if (api?.World?.Calendar == null || requirements == null || pos == null) return false;

            if (requirements.Habitat != EcologyHabitat.Terrestrial) return false;

            WildSpeciesSeason.Profile profile = WildSpeciesSeason.Resolve(requirements.Species);
            EnumSeason season = api.World.Calendar.GetSeason(pos);

            if (season == EnumSeason.Winter)
            {
                if (profile.WinterSurvival >= 1f) return false;
                if (profile.WinterSurvival <= 0f) return true;
                return api.World.Rand.NextDouble() > profile.WinterSurvival;
            }

            if (season == EnumSeason.Fall && profile.FallDieoffChance > 0f)
            {
                return api.World.Rand.NextDouble() < profile.FallDieoffChance;
            }

            return false;
        }

        static float SpringRamp(float seasonRel)
        {
            if (seasonRel <= 0.15f) return 1.35f;
            if (seasonRel <= 0.4f) return 1.15f;
            return 1f;
        }

        static float Clamp(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
    }
}
