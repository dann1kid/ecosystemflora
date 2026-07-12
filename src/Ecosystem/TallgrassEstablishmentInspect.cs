using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Inspect (I) snapshot for meadow tallgrass establishment vs registration queue.</summary>
    internal static class TallgrassEstablishmentInspect
    {
        internal enum Phase
        {
            None,
            WaitingForScan,
            Growing,
            RegistrationPending,
        }

        internal readonly struct Snapshot
        {
            public readonly Phase Phase;
            public readonly int CurrentStageIndex;
            public readonly int RegisterStageIndex;
            public readonly int TargetStageIndex;
            public readonly double HoursUntilNextStage;

            public Snapshot(
                Phase phase,
                int currentStageIndex,
                int registerStageIndex,
                int targetStageIndex,
                double hoursUntilNextStage)
            {
                Phase = phase;
                CurrentStageIndex = currentStageIndex;
                RegisterStageIndex = registerStageIndex;
                TargetStageIndex = targetStageIndex;
                HoursUntilNextStage = hoursUntilNextStage;
            }
        }

        public static bool TryBuild(
            ICoreAPI api,
            BlockPos pos,
            Block block,
            EcosystemSystem eco,
            out Snapshot snapshot)
        {
            snapshot = default;
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (api == null || pos == null || block == null || eco == null) return false;
            if (!TallgrassEstablishment.UsesEstablishment(cfg)) return false;
            if (PlantCodeHelper.ResolveEcologySpecies(block) != "tallgrass") return false;
            if (!TallgrassSpreadHeight.TryParsePath(block.Code.Path, out TallgrassSpreadHeight.TallgrassPathParts parts))
            {
                return false;
            }

            if (string.IsNullOrEmpty(parts.Height)) return false;

            int currentIdx = TallgrassSpreadHeight.GetHeightStageIndex(parts.Height);
            if (currentIdx < 0) return false;

            PlantRequirements requirements = PlantRequirements.FromBlock(block);
            int targetIdx = TallgrassSpreadHeight.PickTargetStageIndex(api, pos, requirements);
            int registerIdx = TallgrassSpreadHeight.MinSpreadStageIndex(targetIdx);

            double nowHours = api.World?.Calendar?.TotalHours ?? 0;
            bool inPromotionQueue = eco.TryGetTallgrassPromotionState(
                pos,
                out int queuedTarget,
                out double nextAdvanceAtHours);

            if (queuedTarget >= 0)
            {
                targetIdx = queuedTarget;
                registerIdx = TallgrassSpreadHeight.MinSpreadStageIndex(targetIdx);
            }

            if (inPromotionQueue)
            {
                double hoursLeft = nextAdvanceAtHours - nowHours;
                if (hoursLeft < 0) hoursLeft = 0;

                snapshot = new Snapshot(
                    Phase.Growing,
                    currentIdx,
                    registerIdx,
                    targetIdx,
                    hoursLeft);
                return true;
            }

            if (TallgrassEstablishment.NeedsEstablishment(api, pos, block, out int liveTarget, requirements))
            {
                targetIdx = liveTarget;
                registerIdx = TallgrassSpreadHeight.MinSpreadStageIndex(targetIdx);

                snapshot = new Snapshot(
                    Phase.WaitingForScan,
                    currentIdx,
                    registerIdx,
                    targetIdx,
                    hoursUntilNextStage: -1);
                return true;
            }

            if (currentIdx >= registerIdx)
            {
                snapshot = new Snapshot(
                    Phase.RegistrationPending,
                    currentIdx,
                    registerIdx,
                    targetIdx,
                    hoursUntilNextStage: -1);
                return true;
            }

            return false;
        }

        public static string StageLabel(int stageIndex)
        {
            if (stageIndex < 0 || stageIndex >= TallgrassSpreadHeight.HeightStages.Length)
            {
                return "?";
            }

            return Lang.Get("ecosystemflora:inspect-tallgrass-stage-" + TallgrassSpreadHeight.HeightStages[stageIndex]);
        }
    }
}
