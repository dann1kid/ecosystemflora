using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Calendar spread timing for <see cref="EcologyHabitat.MyceliumAnchor"/> network steps.</summary>
    internal static class MyceliumSpreadTiming
    {
        public static double EffectiveIntervalHours(ICoreAPI api, EcosystemConfig cfg, PlantRequirements requirements)
        {
            if (cfg == null || requirements == null) return 24;

            if (cfg.UseCalendarScaledSpread && api?.World?.Calendar != null)
            {
                IGameCalendar cal = api.World.Calendar;
                double attemptsPerYear = cfg.MyceliumSpreadAttemptsPerYear;
                if (attemptsPerYear <= 0) attemptsPerYear = 4;

                float spreadRate = cfg.MyceliumSpreadRate > 0f ? cfg.MyceliumSpreadRate : 0.12f;
                if (requirements.SpreadRate > 0f) spreadRate = requirements.SpreadRate;

                double intervalDays = (cal.DaysPerYear / attemptsPerYear) / spreadRate;
                double intervalHours = intervalDays * cal.HoursPerDay;

                if (cfg.MinSpeciesReproduceIntervalDays > 0)
                {
                    double minHours = cfg.MinSpeciesReproduceIntervalDays * cal.HoursPerDay;
                    if (intervalHours < minHours) intervalHours = minHours;
                }

                return CalendarSpeedHelper.ScaleCalendarHours(intervalHours, api?.World?.Calendar);
            }

            double baseHours = cfg.ReproduceIntervalHours > 0 ? cfg.ReproduceIntervalHours : 24;
            float rate = cfg.MyceliumSpreadRate > 0f ? cfg.MyceliumSpreadRate : 0.12f;
            return CalendarSpeedHelper.ScaleCalendarHours(baseHours / rate, api?.World?.Calendar);
        }

        public static float EffectiveChance(EcosystemConfig cfg, PlantRequirements requirements)
        {
            if (cfg == null || requirements == null) return 0.25f;

            float chance = cfg.ReproduceChance;
            if (chance <= 0f) chance = 0.5f;

            float rate = cfg.MyceliumSpreadRate > 0f ? cfg.MyceliumSpreadRate : 0.12f;
            if (requirements.SpreadRate > 0f) rate = requirements.SpreadRate;

            return System.Math.Min(1f, chance * rate);
        }
    }
}
