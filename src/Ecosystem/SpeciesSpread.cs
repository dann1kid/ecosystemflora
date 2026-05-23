namespace WildFarming.Ecosystem
{
    /// <summary>Per-species spread timing derived from config baseline and SpreadRate.</summary>
    public static class SpeciesSpread
    {
        public static double EffectiveIntervalHours(EcosystemConfig cfg, PlantRequirements requirements)
        {
            if (cfg == null || requirements == null) return 24;
            if (!cfg.UseSpeciesSpreadRates || requirements.SpreadRate <= 0f)
            {
                return cfg.ReproduceIntervalHours;
            }

            double interval = cfg.ReproduceIntervalHours / requirements.SpreadRate;
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
    }
}
