using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class MyceliumSpreadFitness
    {
        public static float Score(ICoreAPI api, BlockPos groundPos, PlantRequirements req)
        {
            if (api == null || groundPos == null || req == null) return 0f;
            if (!MyceliumStressEvaluator.MeetsSurvival(api, groundPos, req)) return 0f;

            float score = 1f;
            EcosystemConfig cfg = EcosystemConfig.Loaded;

            if (cfg.UseFloraContext)
            {
                FloraContextSampler flora = EcosystemSystem.Instance?.FloraContext;
                if (flora != null)
                {
                    score *= EcologySpreadFitness.ContextMultiplierFor(req, flora.GetContext(api, groundPos));
                }
            }

            if (req.SpreadRate > 0f)
            {
                score *= req.SpreadRate;
            }

            return score;
        }
    }
}
