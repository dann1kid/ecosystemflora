using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Per-species spread timing from config baseline, SpreadRate, and world calendar.</summary>
    public static class SpeciesSpread
    {
        public static double EffectiveIntervalHours(ICoreAPI api, BlockPos pos, EcosystemConfig cfg, PlantRequirements requirements)
        {
            if (cfg == null || requirements == null) return 24;

            double interval;
            if (cfg.UseCalendarScaledSpread && api?.World?.Calendar != null)
            {
                interval = EffectiveIntervalHoursCalendar(api.World.Calendar, cfg, requirements);
            }
            else
            {
                interval = EffectiveIntervalHoursLegacy(cfg, requirements);
            }

            if (cfg.UseSeasonalEcology && api != null && pos != null)
            {
                float mult = SeasonEcology.SpreadActivityMultiplier(api, pos, requirements);
                if (mult > 0.05f) interval /= mult;
                else interval *= 20;
            }

            return CalendarSpeedHelper.ScaleCalendarHours(interval, api?.World?.Calendar);
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
            double baseHours = cfg.ReproduceIntervalHours > 0 ? cfg.ReproduceIntervalHours : 24;

            double interval = baseHours;
            float spreadRate = EffectiveSpreadRate(cfg, requirements);
            if (cfg.UseSpeciesSpreadRates && spreadRate > 0f)
            {
                interval = baseHours / spreadRate;
            }

            if (cfg.MinSpeciesReproduceIntervalHours > 0)
            {
                interval = System.Math.Max(interval, cfg.MinSpeciesReproduceIntervalHours);
            }

            if (interval < 0.25) interval = 0.25;

            return interval;
        }

        public static float EffectiveChance(ICoreAPI api, BlockPos pos, EcosystemConfig cfg, PlantRequirements requirements)
        {
            if (cfg == null || requirements == null) return 0.25f;

            float chance;
            float spreadRate = EffectiveSpreadRate(cfg, requirements);
            if (!cfg.UseSpeciesSpreadRates || spreadRate <= 0f)
            {
                chance = cfg.ReproduceChance;
            }
            else
            {
                chance = cfg.ReproduceChance * spreadRate;
            }

            if (api != null && pos != null)
            {
                chance *= SeasonEcology.SpreadActivityMultiplier(api, pos, requirements);
            }

            return System.Math.Min(1f, chance);
        }

        static float EffectiveSpreadRate(EcosystemConfig cfg, PlantRequirements requirements)
        {
            if (requirements == null || requirements.SpreadRate <= 0f) return requirements?.SpreadRate ?? 0f;
            return WildSpreadBalance.ScaleSpeciesSpreadRate(requirements.Species, requirements.SpreadRate, cfg);
        }

        static float SpreadMultiplier(EcosystemConfig cfg, PlantRequirements requirements)
        {
            if (!cfg.UseSpeciesSpreadRates || requirements.SpreadRate <= 0f) return 1f;
            return EffectiveSpreadRate(cfg, requirements);
        }
    }
}
