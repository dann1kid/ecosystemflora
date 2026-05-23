using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    /// <summary>Per-species spread timing from config baseline, SpreadRate, and world calendar.</summary>
    public static class SpeciesSpread
    {
        public static double EffectiveIntervalHours(ICoreAPI api, EcosystemConfig cfg, PlantRequirements requirements)
        {
            if (cfg == null || requirements == null) return 24;

            if (cfg.UseCalendarScaledSpread && api?.World?.Calendar != null)
            {
                return EffectiveIntervalHoursCalendar(api.World.Calendar, cfg, requirements);
            }

            return EffectiveIntervalHoursLegacy(cfg, requirements);
        }

        static double EffectiveIntervalHoursCalendar(IGameCalendar cal, EcosystemConfig cfg, PlantRequirements requirements)
        {
            double attemptsPerYear = cfg.ReproduceAttemptsPerYear;
            if (attemptsPerYear <= 0)
            {
                double legacyHours = cfg.ReproduceIntervalHours > 0 ? cfg.ReproduceIntervalHours : 24;
                double hoursPerYear = cal.DaysPerYear * cal.HoursPerDay;
                attemptsPerYear = hoursPerYear / legacyHours;
            }

            if (attemptsPerYear < 0.01) attemptsPerYear = 0.01;

            float spreadRate = SpreadMultiplier(cfg, requirements);
            double intervalDays = (cal.DaysPerYear / attemptsPerYear) / spreadRate;
            double intervalHours = intervalDays * cal.HoursPerDay;

            if (cfg.MinSpeciesReproduceIntervalDays > 0)
            {
                double minHours = cfg.MinSpeciesReproduceIntervalDays * cal.HoursPerDay;
                if (intervalHours < minHours) intervalHours = minHours;
            }

            return intervalHours;
        }

        static double EffectiveIntervalHoursLegacy(EcosystemConfig cfg, PlantRequirements requirements)
        {
            double interval = cfg.ReproduceIntervalHours;
            if (cfg.UseSpeciesSpreadRates && requirements.SpreadRate > 0f)
            {
                interval = cfg.ReproduceIntervalHours / requirements.SpreadRate;
            }

            if (cfg.MinSpeciesReproduceIntervalHours > 0)
            {
                interval = System.Math.Max(interval, cfg.MinSpeciesReproduceIntervalHours);
            }

            return interval;
        }

        public static float EffectiveChance(EcosystemConfig cfg, PlantRequirements requirements)
        {
            if (cfg == null || requirements == null) return 0.25f;
            if (!cfg.UseSpeciesSpreadRates || requirements.SpreadRate <= 0f)
            {
                return cfg.ReproduceChance;
            }

            return System.Math.Min(1f, cfg.ReproduceChance * requirements.SpreadRate);
        }

        static float SpreadMultiplier(EcosystemConfig cfg, PlantRequirements requirements)
        {
            if (!cfg.UseSpeciesSpreadRates || requirements.SpreadRate <= 0f) return 1f;
            return requirements.SpreadRate;
        }
    }
}
