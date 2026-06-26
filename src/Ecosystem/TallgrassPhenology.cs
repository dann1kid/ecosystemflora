using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Tallgrass seasonal dormant/dieback phases with block sync. Active phase keeps vanilla height
    /// maturation; spread is gated off in dormant and dieback.
    /// </summary>
    internal static class TallgrassPhenology
    {
        const float DormantSeasonThreshold = 0.12f;

        public static bool IsEnabled(EcosystemConfig cfg) =>
            cfg != null && cfg.EnableTallgrassPhenology && cfg.EcosystemEnabled;

        public static bool UsesPhenology(EcosystemConfig cfg, PlantRequirements requirements)
        {
            if (!IsEnabled(cfg) || requirements == null) return false;
            return requirements.Species == "tallgrass";
        }

        public static bool IsRegisteredPlantBlock(ReproducerEntry entry, Block block)
        {
            if (entry == null || block == null) return false;
            if (PlantCodeHelper.ResolveEcologySpecies(block) == "tallgrass") return true;
            return TallgrassPhenologyBlocks.IsPhaseBlock(block);
        }

        public static bool ShouldUseDieback(ReproducerEntry entry, EcosystemConfig cfg)
        {
            if (entry == null || cfg == null) return false;
            return entry.FailedSurvivalChecks > 0;
        }

        public static TallgrassPhenologyPhase ResolveSeasonalPhase(
            ICoreAPI api,
            BlockPos pos,
            PlantRequirements requirements,
            EcosystemConfig cfg)
        {
            if (api == null || pos == null || requirements == null) return TallgrassPhenologyPhase.Active;
            if (!cfg.UseSeasonalEcology) return TallgrassPhenologyPhase.Active;

            float season = SeasonEcology.SpreadActivityMultiplier(api, pos, requirements);
            return season <= DormantSeasonThreshold
                ? TallgrassPhenologyPhase.Dormant
                : TallgrassPhenologyPhase.Active;
        }

        public static TallgrassPhenologyPhase EffectivePhase(
            ReproducerEntry entry,
            TallgrassPhenologyPhase seasonal,
            EcosystemConfig cfg)
        {
            if (ShouldUseDieback(entry, cfg)) return TallgrassPhenologyPhase.Dieback;
            return seasonal;
        }

        public static bool AllowsSpread(TallgrassPhenologyPhase phase) => phase == TallgrassPhenologyPhase.Active;

        public static bool CanSpread(ReproducerEntry entry, EcosystemConfig cfg)
        {
            if (entry == null) return false;
            if (!UsesPhenology(cfg, entry.Requirements)) return true;
            return AllowsSpread(entry.TallgrassPhenologyPhase);
        }

        public static void InitializeOnRegister(ICoreAPI api, ReproducerEntry entry, EcosystemConfig cfg)
        {
            if (!UsesPhenology(cfg, entry?.Requirements) || api?.World?.Calendar == null) return;

            double now = api.World.Calendar.TotalHours;
            TallgrassPhenologyPhase seasonal = ResolveSeasonalPhase(api, entry.Origin, entry.Requirements, cfg);
            TallgrassPhenologyPhase effective = EffectivePhase(entry, seasonal, cfg);
            entry.TallgrassPhenologyPhase = effective;
            entry.LastTallgrassPhenologyUpdateHours = now;
            SyncBlockToPhase(api, entry.Origin, effective);
        }

        public static void Advance(ICoreAPI api, ReproducerEntry entry, EcosystemConfig cfg, double nowHours)
        {
            if (!UsesPhenology(cfg, entry?.Requirements) || api?.World?.Calendar == null) return;

            TallgrassPhenologyPhase seasonal = ResolveSeasonalPhase(api, entry.Origin, entry.Requirements, cfg);
            TallgrassPhenologyPhase effective = EffectivePhase(entry, seasonal, cfg);
            if (entry.TallgrassPhenologyPhase == effective
                && nowHours - entry.LastTallgrassPhenologyUpdateHours < 6) return;

            TallgrassPhenologyPhase previous = entry.TallgrassPhenologyPhase;
            entry.TallgrassPhenologyPhase = effective;
            entry.LastTallgrassPhenologyUpdateHours = nowHours;
            if (SyncBlockToPhase(api, entry.Origin, effective)
                && effective == TallgrassPhenologyPhase.Dieback
                && previous != TallgrassPhenologyPhase.Dieback)
            {
                EcologyHistoryRecorder.RecordStressDieback(api, entry.Origin, "tallgrass");
            }
        }

        internal static TallgrassPhenologyPhase InferPhaseForTests(float seasonActivity) =>
            seasonActivity <= DormantSeasonThreshold
                ? TallgrassPhenologyPhase.Dormant
                : TallgrassPhenologyPhase.Active;

        public static bool SyncBlockToPhase(ICoreAPI api, BlockPos pos, TallgrassPhenologyPhase phase)
        {
            if (api == null || pos == null) return false;

            Block current = api.World.BlockAccessor.GetBlock(pos);
            if (current == null || current.Id == 0) return false;
            if (PlantCodeHelper.ResolveEcologySpecies(current) != "tallgrass"
                && !TallgrassPhenologyBlocks.IsPhaseBlock(current))
            {
                return false;
            }

            if (phase == TallgrassPhenologyPhase.Active)
            {
                if (TallgrassPhenologyBlocks.IsPhaseBlock(current))
                {
                    Block veryshort = api.World.GetBlock(new AssetLocation("game:tallgrass-veryshort-free"));
                    if (veryshort == null || veryshort.Id == 0) return false;
                    if (current.BlockId == veryshort.BlockId) return false;
                    api.World.BlockAccessor.SetBlock(veryshort.BlockId, pos);
                    api.World.BlockAccessor.MarkBlockDirty(pos);
                    return true;
                }

                return false;
            }

            AssetLocation code = TallgrassPhenologyBlocks.CodeForPhase(phase, current);
            if (code == null) return false;

            Block target = api.World.GetBlock(code);
            if (target == null || target.Id == 0) return false;
            if (current.BlockId == target.BlockId) return false;

            api.World.BlockAccessor.SetBlock(target.BlockId, pos);
            api.World.BlockAccessor.MarkBlockDirty(pos);
            return true;
        }
    }
}
