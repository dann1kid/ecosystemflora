using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class EcologySpreadFitness
    {
        public static float ApplyContext(
            ICoreAPI api,
            PlantRequirements req,
            BlockPos targetPos,
            float baseFitness)
        {
            if (req == null || baseFitness <= 0f) return baseFitness;
            if (req.Habitat != EcologyHabitat.Terrestrial
                && req.Habitat != EcologyHabitat.TerrestrialTree) return baseFitness;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.UseFloraContext || api == null) return baseFitness;

            FloraContextSampler sampler = EcosystemSystem.Instance?.FloraContext;
            if (sampler == null) return baseFitness;

            FloraContext local = sampler.GetContext(api, targetPos);
            return baseFitness * ContextMultiplierFor(req, local);
        }

        public static float ApplyNiche(
            ICoreAPI api,
            PlantRequirements req,
            BlockPos targetPos,
            float baseFitness)
        {
            if (req == null || baseFitness <= 0f || !req.HasNicheProfile) return baseFitness;
            if (req.Habitat != EcologyHabitat.Terrestrial) return baseFitness;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.UseNicheContext || api == null) return baseFitness;

            NicheSampler sampler = EcosystemSystem.Instance?.Niche;
            if (sampler == null) return baseFitness;

            LocalNiche local = sampler.GetNiche(api, targetPos);
            return baseFitness * NicheMultiplierFor(req, local);
        }

        /// <summary>Penalty from persisted foot-traffic pressure (ecological trails).</summary>
        public static float ApplyTraffic(ICoreAPI api, BlockPos targetPos, float baseFitness)
        {
            if (baseFitness <= 0f || targetPos == null) return baseFitness;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EnableTrampling) return baseFitness;

            ColumnTrafficStore store = EcosystemSystem.Instance?.ColumnTraffic;
            if (store == null || store.Count == 0) return baseFitness;

            IGameCalendar cal = api?.World?.Calendar;
            double now = cal?.TotalHours ?? 0;
            float hoursPerDay = cal != null && cal.HoursPerDay > 0 ? cal.HoursPerDay : 24f;
            float pressure01 = store.GetPressure01(targetPos, now, hoursPerDay, cfg.FootTrafficDecayPerDay);
            return baseFitness * TrafficMultiplierFor(pressure01, cfg.FootTrafficMinSpreadMultiplier);
        }

        public static float TrafficMultiplierFor(float pressure01, float minMultiplier)
        {
            if (pressure01 <= 0f) return 1f;
            if (pressure01 > 1f) pressure01 = 1f;
            if (minMultiplier < 0f) minMultiplier = 0f;
            if (minMultiplier > 1f) minMultiplier = 1f;
            return 1f - pressure01 * (1f - minMultiplier);
        }

        public static float NicheMultiplierFor(PlantRequirements req, LocalNiche local)
        {
            if (req == null || !req.HasNicheProfile) return 1f;

            float bonus = req.NicheBonus > 0f ? req.NicheBonus : 1f;
            float moisture = AxisMultiplier((int)local.Moisture, (int)req.PreferredMoisture, bonus);
            float light = AxisMultiplier((int)local.Light, (int)req.PreferredLight, bonus);
            return System.Math.Min(moisture, light);
        }

        static float AxisMultiplier(int local, int preferred, float bonus)
        {
            int dist = System.Math.Abs(local - preferred);
            switch (dist)
            {
                case 0: return bonus;
                case 1: return 0.88f;
                case 2: return 0.68f;
                default: return 0.45f;
            }
        }

        internal static float ContextMultiplierFor(PlantRequirements req, FloraContext local)
        {
            float bonus = req.ContextBonus > 0f ? req.ContextBonus : 1f;
            float openPenalty = req.ForestInteriorPenalty > 0f
                ? req.ForestInteriorPenalty
                : EcosystemConfig.Loaded.FloraOpenInteriorPenalty;

            switch (local)
            {
                case FloraContext.Open:
                    if (req.ContextAffinity == FloraContextAffinity.Open) return bonus;
                    if (req.ContextAffinity == FloraContextAffinity.Edge) return 0.85f;
                    return 0.55f;

                case FloraContext.ForestEdge:
                    if (req.ContextAffinity == FloraContextAffinity.Edge) return bonus;
                    if (req.ContextAffinity == FloraContextAffinity.Open) return 0.92f;
                    if (req.ContextAffinity == FloraContextAffinity.Forest) return 1.15f;
                    return 1f;

                case FloraContext.ForestInterior:
                    if (req.ContextAffinity == FloraContextAffinity.Forest) return bonus;
                    if (req.ContextAffinity == FloraContextAffinity.Edge) return 0.72f;
                    if (req.ContextAffinity == FloraContextAffinity.Open) return openPenalty;
                    return 0.5f;

                default:
                    return 1f;
            }
        }
    }
}
