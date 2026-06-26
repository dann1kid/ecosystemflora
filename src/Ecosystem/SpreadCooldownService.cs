using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Applies post-spread / failed-roll cooldown to a parent reproducer, deduped per reproduce tick.
    /// Iterates the registered maturation policies so each species' cooldown math lives in its policy
    /// rather than as a fern-then-flower branch ladder inside the ecosystem tick.
    /// </summary>
    internal sealed class SpreadCooldownService
    {
        readonly ICoreAPI api;
        readonly ReproducerRegistry registry;
        readonly HashSet<BlockPos> appliedThisTick = new HashSet<BlockPos>();

        public SpreadCooldownService(ICoreAPI api, ReproducerRegistry registry)
        {
            this.api = api;
            this.registry = registry;
        }

        /// <summary>Clear the per-tick dedupe set; call once at the start of each reproduce tick.</summary>
        public void ResetTick()
        {
            appliedThisTick.Clear();
        }

        public void ApplyOnCommit(BlockPos spreadOrigin, PlantRequirements requirements)
        {
            if (spreadOrigin == null || requirements == null) return;
            ApplyPostSpreadAttemptOnce(spreadOrigin, requirements);
        }

        public void ApplyPostSpreadAttemptOnce(BlockPos spreadOrigin, PlantRequirements requirements)
        {
            if (spreadOrigin == null || !appliedThisTick.Add(spreadOrigin)) return;
            Apply(spreadOrigin, requirements, failedChanceRoll: false);
        }

        public void ApplyFailedChanceRollOnce(BlockPos spreadOrigin, PlantRequirements requirements)
        {
            if (spreadOrigin == null || !appliedThisTick.Add(spreadOrigin)) return;
            Apply(spreadOrigin, requirements, failedChanceRoll: true);
        }

        void Apply(BlockPos spreadOrigin, PlantRequirements requirements, bool failedChanceRoll)
        {
            if (spreadOrigin == null || requirements == null || api == null) return;
            if (!registry.TryGetEntry(spreadOrigin, out ReproducerEntry parent)) return;

            double nowHours = api.World.Calendar.TotalHours;
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            SpreadMaturationPolicy[] policies = SpreadMaturationPolicies.All;

            for (int i = 0; i < policies.Length; i++)
            {
                if (policies[i].TryApplySpreadAttemptCooldown(
                        parent, nowHours, api, parent.Origin, requirements, cfg, failedChanceRoll))
                {
                    return;
                }
            }
        }
    }
}
