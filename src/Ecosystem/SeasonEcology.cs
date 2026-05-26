using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Monthly spread multipliers and seasonal stress (interpolated 12-month curves).</summary>
    internal static class SeasonEcology
    {
        public static float SpreadActivityMultiplier(ICoreAPI api, BlockPos pos, PlantRequirements requirements)
        {
            if (!EcosystemConfig.Loaded.UseSeasonalEcology) return 1f;
            if (api?.World?.Calendar == null || requirements == null || pos == null) return 1f;

            WildSpeciesSeason.Profile profile = WildSpeciesSeason.Resolve(requirements.Species);

            IGameCalendar cal = api.World.Calendar;
            float yearProgress = cal.DayOfYearf / cal.DaysPerYear;
            float mult = profile.SpreadMultiplierInterpolated(yearProgress);

            return Clamp(mult, 0f, 3f);
        }

        /// <summary>Monthly seasonal stress failure roll (replaces old winter/fall binary).</summary>
        public static bool RollSeasonalStressFailure(ICoreAPI api, BlockPos pos, PlantRequirements requirements)
        {
            if (!EcosystemConfig.Loaded.UseSeasonalEcology) return false;
            if (!EcosystemConfig.Loaded.SeasonalStressEnabled) return false;
            if (api?.World?.Calendar == null || requirements == null || pos == null) return false;

            if (requirements.Habitat != EcologyHabitat.Terrestrial) return false;

            WildSpeciesSeason.Profile profile = WildSpeciesSeason.Resolve(requirements.Species);

            int month = api.World.Calendar.Month - 1;
            float chance = profile.StressChance(month);
            if (chance <= 0f) return false;

            return api.World.Rand.NextDouble() < chance;
        }

        static float Clamp(float v, float min, float max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
    }
}
