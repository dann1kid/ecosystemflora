using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Ground-herb seasonal dormant/dieback phases with block sync. Covers tallgrass (vanilla height
    /// maturation) and shore sedge (vanilla tallplant). Spread is gated off in dormant and dieback.
    /// </summary>
    internal static class TallgrassPhenology
    {
        const float DormantSeasonThreshold = 0.12f;

        public static bool IsEnabled(EcosystemConfig cfg) =>
            cfg != null && cfg.EnableTallgrassPhenology && cfg.EcosystemEnabled;

        public static bool UsesPhenology(EcosystemConfig cfg, PlantRequirements requirements)
        {
            if (!IsEnabled(cfg) || requirements == null) return false;
            if (requirements.Species == "tallgrass") return true;
            return WildShoreSedgeEcology.IsSpecies(requirements.Species);
        }

        static bool IsShoreSedge(PlantRequirements requirements) =>
            WildShoreSedgeEcology.IsSpecies(requirements?.Species);

        public static bool IsRegisteredPlantBlock(ReproducerEntry entry, Block block)
        {
            if (entry == null || block == null) return false;

            if (IsShoreSedge(entry.Requirements))
            {
                if (SedgePhenologyBlocks.IsPhaseBlock(block)) return true;
                return SedgePhenologyBlocks.IsSyncableMatureBlock(block);
            }

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

        /// <summary>Worldgen / chunk-scan sedge without a registry entry yet.</summary>
        public static bool TrySyncWildBlock(ICoreAPI api, BlockPos pos, Block block, EcosystemConfig cfg)
        {
            if (!IsEnabled(cfg) || api?.World?.BlockAccessor == null || pos == null || block == null) return false;
            if (!LandClaimGuard.AllowsEcologyChange(api, pos)) return false;

            string species = PlantCodeHelper.ResolveEcologySpecies(block);
            if (!WildShoreSedgeEcology.IsSpecies(species)) return false;

            var requirements = new PlantRequirements
            {
                Species = species,
                Habitat = EcologyHabitat.Terrestrial,
            };

            TallgrassPhenologyPhase effective = EffectivePhase(null, ResolveSeasonalPhase(api, pos, requirements, cfg), cfg);
            if (BlockMatchesPhase(api, pos, requirements, block, effective)) return false;

            return SyncBlockToPhase(api, pos, requirements, effective);
        }

        public static void InitializeOnRegister(ICoreAPI api, ReproducerEntry entry, EcosystemConfig cfg)
        {
            if (!UsesPhenology(cfg, entry?.Requirements) || api?.World?.Calendar == null) return;

            double now = api.World.Calendar.TotalHours;
            TallgrassPhenologyPhase seasonal = ResolveSeasonalPhase(api, entry.Origin, entry.Requirements, cfg);
            TallgrassPhenologyPhase effective = EffectivePhase(entry, seasonal, cfg);
            entry.TallgrassPhenologyPhase = effective;
            entry.LastTallgrassPhenologyUpdateHours = now;
            ReconcileBlockToPhase(api, entry.Origin, entry.Requirements, effective);
        }

        public static void Advance(ICoreAPI api, ReproducerEntry entry, EcosystemConfig cfg, double nowHours)
        {
            if (!UsesPhenology(cfg, entry?.Requirements) || api?.World?.Calendar == null) return;

            TallgrassPhenologyPhase seasonal = ResolveSeasonalPhase(api, entry.Origin, entry.Requirements, cfg);
            TallgrassPhenologyPhase effective = EffectivePhase(entry, seasonal, cfg);

            if (entry.TallgrassPhenologyPhase == effective
                && nowHours - entry.LastTallgrassPhenologyUpdateHours < 6)
            {
                ReconcileBlockToPhase(api, entry.Origin, entry.Requirements, effective);
                return;
            }

            TallgrassPhenologyPhase previous = entry.TallgrassPhenologyPhase;
            entry.TallgrassPhenologyPhase = effective;
            entry.LastTallgrassPhenologyUpdateHours = nowHours;
            if (ReconcileBlockToPhase(api, entry.Origin, entry.Requirements, effective)
                && effective == TallgrassPhenologyPhase.Dieback
                && previous != TallgrassPhenologyPhase.Dieback)
            {
                EcologyHistoryRecorder.RecordStressDieback(
                    api,
                    entry.Origin,
                    entry.Requirements.Species ?? "tallgrass");
            }
        }

        internal static TallgrassPhenologyPhase InferPhaseForTests(float seasonActivity) =>
            seasonActivity <= DormantSeasonThreshold
                ? TallgrassPhenologyPhase.Dormant
                : TallgrassPhenologyPhase.Active;

        static bool ReconcileBlockToPhase(
            ICoreAPI api,
            BlockPos pos,
            PlantRequirements requirements,
            TallgrassPhenologyPhase phase)
        {
            Block current = api.World.BlockAccessor.GetBlock(pos);
            if (BlockMatchesPhase(api, pos, requirements, current, phase))
            {
                PlantSnowCoverSync.TrySyncCover(api, pos, current);
                return false;
            }

            bool changed = SyncBlockToPhase(api, pos, requirements, phase);
            PlantSnowCoverSync.TrySyncCover(api, pos);
            return changed;
        }

        internal static bool BlockMatchesPhase(
            ICoreAPI api,
            BlockPos pos,
            PlantRequirements requirements,
            Block block,
            TallgrassPhenologyPhase phase)
        {
            if (block == null || block.Id == 0) return false;

            if (IsShoreSedge(requirements))
            {
                return BlockMatchesSedgePhase(api, pos, block, phase);
            }

            return BlockMatchesTallgrassPhase(api, pos, block, phase);
        }

        static bool BlockMatchesSedgePhase(
            ICoreAPI api,
            BlockPos pos,
            Block block,
            TallgrassPhenologyPhase phase)
        {
            if (phase == TallgrassPhenologyPhase.Active)
            {
                return SedgePhenologyBlocks.IsSyncableMatureBlock(block);
            }

            if (!SedgePhenologyBlocks.IsPhaseBlock(block)) return false;
            if (!SedgePhenologyBlocks.TryGetPhase(block, out TallgrassPhenologyPhase blockPhase)) return false;
            if (blockPhase != phase) return false;

            bool wantSnow = PlantSnowCover.ResolveWantsSnowCover(api, pos);
            return PlantSnowCover.PathHasSnowCover(block.Code.Path) == wantSnow;
        }

        static bool BlockMatchesTallgrassPhase(
            ICoreAPI api,
            BlockPos pos,
            Block block,
            TallgrassPhenologyPhase phase)
        {
            if (phase == TallgrassPhenologyPhase.Active)
            {
                return PlantCodeHelper.ResolveEcologySpecies(block) == "tallgrass"
                    && !TallgrassPhenologyBlocks.IsPhaseBlock(block);
            }

            if (!TallgrassPhenologyBlocks.IsPhaseBlock(block)) return false;
            TallgrassPhenologyPhase? blockPhase = TallgrassPhenologyBlocks.PhaseFromBlock(block);
            if (blockPhase != phase) return false;

            bool wantSnow = PlantSnowCover.ResolveWantsSnowCover(api, pos);
            return PlantSnowCover.PathHasSnowCover(block.Code.Path) == wantSnow;
        }

        public static bool SyncBlockToPhase(
            ICoreAPI api,
            BlockPos pos,
            PlantRequirements requirements,
            TallgrassPhenologyPhase phase)
        {
            if (api == null || pos == null || requirements == null) return false;
            if (!LandClaimGuard.AllowsEcologyChange(api, pos)) return false;

            if (IsShoreSedge(requirements))
            {
                return SyncSedgeBlockToPhase(api, pos, phase);
            }

            return SyncTallgrassBlockToPhase(api, pos, phase);
        }

        static bool SyncSedgeBlockToPhase(ICoreAPI api, BlockPos pos, TallgrassPhenologyPhase phase)
        {
            Block current = api.World.BlockAccessor.GetBlock(pos);
            if (current == null || current.Id == 0) return false;
            if (!SedgePhenologyBlocks.IsSyncableMatureBlock(current)
                && !SedgePhenologyBlocks.IsPhaseBlock(current))
            {
                return false;
            }

            bool snow = PlantSnowCover.ResolveWantsSnowCover(api, pos);

            if (phase == TallgrassPhenologyPhase.Active)
            {
                if (!SedgePhenologyBlocks.IsPhaseBlock(current)) return false;

                AssetLocation matureCode = PlantSnowCover.CodeWithCover(
                    ShoreSedgeJuvenileBlocks.MatureVanillaCode(EcologyShoreSedgeSpecies.Brownsedge),
                    snow);
                Block mature = api.World.GetBlock(matureCode);
                if (mature == null || mature.Id == 0) return false;
                if (current.BlockId == mature.BlockId) return false;

                api.World.BlockAccessor.SetBlock(mature.BlockId, pos);
                api.World.BlockAccessor.MarkBlockDirty(pos);
                return true;
            }

            AssetLocation code = SedgePhenologyBlocks.CodeForPhase(phase, snow);
            if (code == null) return false;

            Block target = api.World.GetBlock(code);
            if (target == null || target.Id == 0) return false;
            if (current.BlockId == target.BlockId) return false;

            api.World.BlockAccessor.SetBlock(target.BlockId, pos);
            api.World.BlockAccessor.MarkBlockDirty(pos);
            return true;
        }

        static bool SyncTallgrassBlockToPhase(ICoreAPI api, BlockPos pos, TallgrassPhenologyPhase phase)
        {
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
                    bool snow = PlantSnowCover.ResolveWantsSnowCover(api, pos);
                    AssetLocation veryshortCode = PlantSnowCover.CodeWithCover(
                        new AssetLocation("game:tallgrass-veryshort-free"),
                        snow);
                    Block veryshort = api.World.GetBlock(veryshortCode);
                    if (veryshort == null || veryshort.Id == 0) return false;
                    if (current.BlockId == veryshort.BlockId) return false;
                    api.World.BlockAccessor.SetBlock(veryshort.BlockId, pos);
                    api.World.BlockAccessor.MarkBlockDirty(pos);
                    return true;
                }

                return false;
            }

            AssetLocation code = TallgrassPhenologyBlocks.CodeForPhase(
                phase,
                snow: PlantSnowCover.ResolveWantsSnowCover(api, pos));
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
