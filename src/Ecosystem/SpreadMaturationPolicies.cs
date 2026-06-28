using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Registry and facade for spread-maturation policies. Order matters for cooldown application:
    /// first matching policy wins. Every reproducing species that needs post-spread cooldown must
    /// belong to one policy's cooldown membership — otherwise wake can outpace calendar cadence.
    /// </summary>
    internal static class SpreadMaturationPolicies
    {
        public static readonly SpreadMaturationPolicy[] All =
        {
            WildFernSpread.Policy,
            WildFlowerMaturation.Policy,
        };

        public static bool UsesMaturation(EcosystemConfig cfg, string species)
        {
            if (cfg == null || string.IsNullOrEmpty(species)) return false;

            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].UsesMaturation(cfg, species)) return true;
            }

            return false;
        }

        public static bool UsesPostSpreadAttemptCooldown(EcosystemConfig cfg, string species)
        {
            if (cfg == null || string.IsNullOrEmpty(species)) return false;

            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].UsesPostSpreadAttemptCooldown(cfg, species)) return true;
            }

            return false;
        }

        public static bool TryApplySpreadAttemptCooldown(
            ReproducerEntry parent,
            double nowHours,
            ICoreAPI api,
            BlockPos pos,
            PlantRequirements requirements,
            EcosystemConfig cfg,
            bool failedChanceRoll)
        {
            if (parent == null || requirements == null || cfg == null) return false;

            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].TryApplySpreadAttemptCooldown(
                        parent, nowHours, api, pos, requirements, cfg, failedChanceRoll))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
