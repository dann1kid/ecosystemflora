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
            if (req.Habitat != EcologyHabitat.Terrestrial) return baseFitness;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.UseFloraContext || api == null) return baseFitness;

            FloraContextSampler sampler = EcosystemSystem.Instance?.FloraContext;
            if (sampler == null) return baseFitness;

            FloraContext local = sampler.GetContext(api, targetPos);
            return baseFitness * ContextMultiplierFor(req, local);
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
