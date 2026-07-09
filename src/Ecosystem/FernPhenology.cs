using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Fern seasonal phases (dormant / sporulating / dieback) with block sync and spread gates.
    /// Orphan symbionts under stress sync to dieback before removal.
    /// </summary>
    internal static class FernPhenology
    {
        const float SporulationSeasonThreshold = 0.35f;

        public static bool IsEnabled(EcosystemConfig cfg) =>
            cfg != null && cfg.EnableFernPhenology && cfg.EcosystemEnabled;

        public static bool UsesPhenology(EcosystemConfig cfg, PlantRequirements requirements)
        {
            if (!IsEnabled(cfg) || requirements == null) return false;
            return WildFernSpread.IsFernSpecies(requirements.Species);
        }

        public static bool MatchesPhaseBlock(Block block, PlantRequirements requirements)
        {
            if (block == null || requirements == null) return false;
            string species = FernPhenologyBlocks.SpeciesFromPhaseCode(block.Code);
            return species != null
                && string.Equals(species, requirements.Species, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsRegisteredPlantBlock(ReproducerEntry entry, Block block)
        {
            if (entry == null || block == null) return false;
            if (entry.IsMatureBlock(block)) return true;
            if (MatchesPhaseBlock(block, entry.Requirements)) return true;
            return FernJuvenileBlocks.MatchesJuvenileBlock(block, entry.Requirements);
        }

        public static FernPhenologyPhase ResolveSeasonalPhase(
            ICoreAPI api,
            BlockPos pos,
            PlantRequirements requirements,
            EcosystemConfig cfg)
        {
            if (api == null || pos == null || requirements == null) return FernPhenologyPhase.Sporulating;
            if (!cfg.EnableFernSporulationGate) return FernPhenologyPhase.Sporulating;

            float season = SeasonEcology.SpreadActivityMultiplier(api, pos, requirements);
            return season >= SporulationSeasonThreshold
                ? FernPhenologyPhase.Sporulating
                : FernPhenologyPhase.Dormant;
        }

        public static bool ShouldUseDieback(IBlockAccessor acc, ReproducerEntry entry, EcosystemConfig cfg)
        {
            if (entry == null || cfg == null || !cfg.EnableSymbiosis) return false;
            if (entry.FailedSurvivalChecks <= 0) return false;
            if (entry.Requirements == null || !FloraSymbiosis.TryGetRule(entry.Requirements.Species, out _)) return false;
            return !FloraSymbiosis.HasRequiredHost(acc, entry.Origin, entry.Requirements.Species);
        }

        public static FernPhenologyPhase EffectivePhase(
            IBlockAccessor acc,
            ReproducerEntry entry,
            FernPhenologyPhase seasonal,
            EcosystemConfig cfg)
        {
            if (ShouldUseDieback(acc, entry, cfg)) return FernPhenologyPhase.Dieback;
            return seasonal;
        }

        public static bool AllowsSpread(FernPhenologyPhase phase) => phase == FernPhenologyPhase.Sporulating;

        public static bool CanSpread(ICoreAPI api, ReproducerEntry entry, EcosystemConfig cfg)
        {
            if (entry == null) return false;
            if (UsesPhenology(cfg, entry.Requirements) && !AllowsSpread(entry.FernPhenologyPhase)) return false;
            return WildFernSpread.CanSpread(api, entry, cfg);
        }

        public static void InitializeOnRegister(ICoreAPI api, ReproducerEntry entry, EcosystemConfig cfg)
        {
            if (!UsesPhenology(cfg, entry?.Requirements) || api?.World?.Calendar == null) return;

            double now = api.World.Calendar.TotalHours;
            IBlockAccessor acc = api.World.BlockAccessor;
            FernPhenologyPhase seasonal = ResolveSeasonalPhase(api, entry.Origin, entry.Requirements, cfg);
            FernPhenologyPhase effective = EffectivePhase(acc, entry, seasonal, cfg);
            entry.FernPhenologyPhase = effective;
            entry.LastFernPhenologyUpdateHours = now;
            ReconcileBlockToPhase(api, entry.Origin, entry.Requirements.Species, effective);
        }

        public static void Advance(ICoreAPI api, ReproducerEntry entry, EcosystemConfig cfg, double nowHours)
        {
            if (!UsesPhenology(cfg, entry?.Requirements) || api?.World?.Calendar == null) return;

            IBlockAccessor acc = api.World.BlockAccessor;
            FernPhenologyPhase seasonal = ResolveSeasonalPhase(api, entry.Origin, entry.Requirements, cfg);
            FernPhenologyPhase effective = EffectivePhase(acc, entry, seasonal, cfg);

            if (entry.FernPhenologyPhase == effective
                && nowHours - entry.LastFernPhenologyUpdateHours < 6)
            {
                ReconcileBlockToPhase(api, entry.Origin, entry.Requirements.Species, effective);
                return;
            }

            FernPhenologyPhase previous = entry.FernPhenologyPhase;
            entry.FernPhenologyPhase = effective;
            entry.LastFernPhenologyUpdateHours = nowHours;
            if (ReconcileBlockToPhase(api, entry.Origin, entry.Requirements.Species, effective)
                && effective == FernPhenologyPhase.Dieback
                && previous != FernPhenologyPhase.Dieback)
            {
                EcologyHistoryRecorder.RecordOrphanDieback(api, entry.Origin, entry.Requirements.Species);
            }
        }

        internal static FernPhenologyPhase InferPhaseForTests(float seasonActivity) =>
            seasonActivity >= SporulationSeasonThreshold
                ? FernPhenologyPhase.Sporulating
                : FernPhenologyPhase.Dormant;

        static bool ReconcileBlockToPhase(
            ICoreAPI api,
            BlockPos pos,
            string species,
            FernPhenologyPhase phase)
        {
            Block current = api.World.BlockAccessor.GetBlock(pos);
            if (BlockMatchesPhase(api, pos, species, current, phase))
            {
                PlantSnowCoverSync.TrySyncCover(api, pos, current);
                return false;
            }

            bool changed = SyncBlockToPhase(api, pos, species, phase);
            PlantSnowCoverSync.TrySyncCover(api, pos);
            return changed;
        }

        internal static bool BlockMatchesPhase(
            ICoreAPI api,
            BlockPos pos,
            string species,
            Block block,
            FernPhenologyPhase phase)
        {
            if (block == null || block.Id == 0) return false;
            if (!string.Equals(FernPhenologyBlocks.SpeciesFromPhaseCode(block.Code), species, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (FernPhenologyBlocks.PhaseFromCode(block.Code) != phase) return false;
            return true;
        }

        public static bool SyncBlockToPhase(
            ICoreAPI api,
            BlockPos pos,
            string species,
            FernPhenologyPhase phase)
        {
            if (api == null || pos == null || string.IsNullOrEmpty(species)) return false;
            if (!LandClaimGuard.AllowsEcologyChange(api, pos)) return false;

            Block current = api.World.BlockAccessor.GetBlock(pos);
            AssetLocation code = FernPhenologyBlocks.CodeForPhase(species, phase, snow: false);
            if (code == null) return false;

            Block target = api.World.GetBlock(code);
            if (target == null || target.Id == 0) return false;

            if (current?.BlockId == target.BlockId) return false;

            api.World.BlockAccessor.SetBlock(target.BlockId, pos);
            api.World.BlockAccessor.MarkBlockDirty(pos);
            return true;
        }
    }
}
