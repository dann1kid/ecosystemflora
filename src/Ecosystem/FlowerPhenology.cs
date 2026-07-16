using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Meadow flower life phases driven by season curves, local temperature, stored energy,
    /// deferred stress (dieback hysteresis), and a limited number of dieback life-cycles.
    /// Spread and harvest are gated to <see cref="FlowerPhenologyPhase.Bloom"/>; blocks mirror phase.
    /// </summary>
    internal static class FlowerPhenology
    {
        const float DormantSeasonThreshold = 0.08f;
        const float VegetativeSeasonThreshold = 0.15f;
        const float BloomSeasonThreshold = 0.35f;
        const float BloomExitSeasonThreshold = 0.22f;
        const float BloomDepletionPerDay = 0.12f;

        public static bool UsesPhenology(EcosystemConfig cfg, PlantRequirements requirements)
        {
            if (cfg == null || !cfg.EnableFlowerPhenology || requirements == null) return false;
            if (requirements.Habitat != EcologyHabitat.Terrestrial) return false;
            return IsFlowerSpecies(requirements.Species);
        }

        public static bool IsFlowerSpecies(string species)
        {
            if (string.IsNullOrEmpty(species)) return false;
            if (EcologyFlowerSpecies.IsKnownFlower(species)) return true;
            return EcologyGrassColonizerSpecies.IsKnown(species);
        }

        public static bool MatchesJuvenileBlock(Block block, PlantRequirements requirements)
        {
            if (block == null || requirements == null) return false;
            string species = FlowerJuvenileBlocks.SpeciesFromJuvenile(block);
            return species != null
                && string.Equals(species, requirements.Species, StringComparison.OrdinalIgnoreCase);
        }

        public static bool MatchesPhaseBlock(Block block, PlantRequirements requirements)
        {
            if (block == null || requirements == null) return false;
            string species = FlowerPhenologyBlocks.SpeciesFromPhaseBlock(block);
            return species != null
                && string.Equals(species, requirements.Species, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsRegisteredPlantBlock(ReproducerEntry entry, Block block)
        {
            if (entry == null || block == null) return false;
            if (entry.IsMatureBlock(block)) return true;
            if (MatchesPhaseBlock(block, entry.Requirements)) return true;
            return MatchesJuvenileBlock(block, entry.Requirements);
        }

        public static bool CanSpread(ReproducerEntry entry)
        {
            if (entry == null) return false;
            return entry.PhenologyPhase == FlowerPhenologyPhase.Bloom;
        }

        public static bool AllowsFlowerBlockHarvest(ReproducerEntry entry)
        {
            if (entry == null) return true;
            return entry.PhenologyPhase == FlowerPhenologyPhase.Bloom;
        }

        public static void InitializeOnRegister(
            ICoreAPI api,
            ReproducerEntry entry,
            EcosystemConfig cfg,
            bool spreadEstablished = false)
        {
            if (!UsesPhenology(cfg, entry?.Requirements) || api?.World?.Calendar == null) return;

            double now = api.World.Calendar.TotalHours;
            entry.LastPhenologyUpdateHours = now;

            FlowerPhenologyLifeStore store = EcosystemSystem.Instance?.FlowerPhenologyLife;
            bool restored = store != null && store.TryRestore(entry);

            if (!restored)
            {
                Block block = api.World.BlockAccessor.GetBlock(entry.Origin);
                entry.PhenologyPhase = ResolveRegisterPhase(api, entry, block, cfg, spreadEstablished);
                entry.PhenologyEnergy = entry.PhenologyPhase == FlowerPhenologyPhase.Bloom
                    ? cfg.FlowerBloomEnergyThreshold
                    : spreadEstablished ? 0.25f : 0.12f;
                entry.PhenologyStress = entry.PhenologyPhase == FlowerPhenologyPhase.Dieback
                    ? cfg.FlowerPhenologyStressEnterDieback
                    : 0f;
                entry.PhenologyLifeCycles = 0;
            }
            else if (entry.PhenologyPhase == FlowerPhenologyPhase.Bloom
                     && entry.PhenologyEnergy < cfg.FlowerBloomEnergyThreshold * 0.5f)
            {
                entry.PhenologyEnergy = cfg.FlowerBloomEnergyThreshold;
            }

            SyncBlock(api, entry, cfg);
            store?.Capture(entry);
            PlantSnowCoverSync.TrySyncCover(api, entry.Origin);
        }

        /// <summary>
        /// World block hints juvenile / phase assets; mature vanilla flowers follow season, not bloom appearance.
        /// </summary>
        internal static FlowerPhenologyPhase ResolveRegisterPhase(
            ICoreAPI api,
            ReproducerEntry entry,
            Block block,
            EcosystemConfig cfg,
            bool spreadEstablished)
        {
            if (spreadEstablished)
            {
                return FlowerPhenologyPhase.Vegetative;
            }

            if (FlowerPhenologyBlocks.TryGetPhase(block?.Code, out FlowerPhenologyPhase blockPhase))
            {
                return blockPhase;
            }

            if (MatchesJuvenileBlock(block, entry.Requirements))
            {
                return FlowerPhenologyPhase.Vegetative;
            }

            ResolveSeasonInputs(api, entry.Origin, entry.Requirements.Species, out float season, out float temp);
            return InferInitialPhase(season, temp, cfg);
        }

        static bool BlockMatchesEntryPhase(ReproducerEntry entry, Block block)
        {
            if (block == null || block.Id == 0) return false;

            switch (entry.PhenologyPhase)
            {
                case FlowerPhenologyPhase.Bloom:
                    return entry.IsMatureBlock(block);
                case FlowerPhenologyPhase.Dormant:
                case FlowerPhenologyPhase.Vegetative:
                case FlowerPhenologyPhase.Dieback:
                    if (!FlowerPhenologyBlocks.TryGetPhase(block.Code, out FlowerPhenologyPhase blockPhase))
                    {
                        return false;
                    }

                    return blockPhase == entry.PhenologyPhase;
                default:
                    return false;
            }
        }

        public static FlowerPhenologyPhase InferInitialPhase(float seasonActivity, float temp, EcosystemConfig cfg)
        {
            if (seasonActivity < DormantSeasonThreshold) return FlowerPhenologyPhase.Dormant;
            if (seasonActivity >= BloomSeasonThreshold && TemperatureSupportsBloom(temp, cfg))
            {
                return FlowerPhenologyPhase.Bloom;
            }

            // Deferred stress applies over time; do not snap to dieback on register for a hot tick.
            return FlowerPhenologyPhase.Vegetative;
        }

        public static void Advance(
            ICoreAPI api,
            ReproducerEntry entry,
            EcosystemConfig cfg,
            double nowHours)
        {
            if (!UsesPhenology(cfg, entry?.Requirements) || api?.World?.Calendar == null) return;

            double deltaHours = nowHours - entry.LastPhenologyUpdateHours;
            if (deltaHours < 0.25)
            {
                PlantSnowCoverSync.TrySyncCover(api, entry.Origin);
                return;
            }

            entry.LastPhenologyUpdateHours = nowHours;
            double deltaDays = deltaHours / Math.Max(1.0, api.World.Calendar.HoursPerDay);

            ResolveSeasonInputs(api, entry.Origin, entry.Requirements.Species, out float season, out float temp);
            FlowerPhenologyPhase previous = entry.PhenologyPhase;

            UpdateStress(entry, season, temp, cfg, deltaDays);

            if (TryApplyDiebackFromStress(api, entry, cfg))
            {
                // Senescence death removes the registry entry — stop immediately.
                if (EcosystemSystem.Instance == null
                    || !EcosystemSystem.Instance.TryGetReproducer(entry.Origin, out _))
                {
                    return;
                }
            }
            else
            {
                switch (entry.PhenologyPhase)
                {
                    case FlowerPhenologyPhase.Dormant:
                        AdvanceDormant(entry, season, temp, cfg, deltaDays);
                        break;
                    case FlowerPhenologyPhase.Vegetative:
                        AdvanceVegetative(entry, season, temp, cfg, deltaDays);
                        break;
                    case FlowerPhenologyPhase.Bloom:
                        AdvanceBloom(entry, season, temp, cfg, deltaDays);
                        break;
                    case FlowerPhenologyPhase.Dieback:
                        AdvanceDieback(entry, season, temp, cfg, deltaDays);
                        break;
                }
            }

            if (entry.PhenologyPhase != previous)
            {
                SyncBlock(api, entry, cfg);
            }
            else
            {
                Block current = api.World.BlockAccessor.GetBlock(entry.Origin);
                if (!BlockMatchesEntryPhase(entry, current))
                {
                    SyncBlock(api, entry, cfg);
                }
            }

            EcosystemSystem.Instance?.FlowerPhenologyLife?.Capture(entry);
            PlantSnowCoverSync.TrySyncCover(api, entry.Origin);
        }

        /// <summary>
        /// Accumulates cold/heat/season-exit stress; decays under good growing conditions.
        /// Frost and winter share the same gain rate so a hard freeze packs winter-class debt.
        /// </summary>
        internal static void UpdateStress(
            ReproducerEntry entry,
            float season,
            float temp,
            EcosystemConfig cfg,
            double deltaDays)
        {
            if (entry == null || cfg == null || deltaDays <= 0) return;

            float gain = SampleStressGainPerDay(entry.PhenologyPhase, season, temp, cfg, entry.PhenologyEnergy);
            float enter = Math.Max(0.2f, cfg.FlowerPhenologyStressEnterDieback);
            float stress = entry.PhenologyStress;

            if (gain > 0f)
            {
                stress += gain * (float)deltaDays;
            }
            else
            {
                float decay = Math.Max(0f, cfg.FlowerPhenologyStressDecayPerDay);
                stress -= decay * (float)deltaDays;
            }

            if (stress < 0f) stress = 0f;
            if (stress > enter * 1.5f) stress = enter * 1.5f;
            entry.PhenologyStress = stress;
        }

        internal static float SampleStressGainPerDay(
            FlowerPhenologyPhase phase,
            float season,
            float temp,
            EcosystemConfig cfg,
            float energy)
        {
            float gain = 0f;
            float coldGain = Math.Max(0f, cfg.FlowerPhenologyColdStressGainPerDay);
            float heatGain = Math.Max(0f, cfg.FlowerPhenologyHeatStressGainPerDay);
            float exitGain = Math.Max(0f, cfg.FlowerPhenologySeasonExitStressGainPerDay);

            bool winterSeason = season < DormantSeasonThreshold;
            bool frost = temp < cfg.FlowerBloomMinTemperature;
            bool heat = temp > cfg.FlowerBloomMaxTemperature;

            // Frost and winter: same damage class (one hard freeze ≈ early winter debt).
            if (winterSeason || frost)
            {
                gain = Math.Max(gain, coldGain);
            }

            if (heat)
            {
                gain = Math.Max(gain, heatGain);
            }

            if (phase == FlowerPhenologyPhase.Bloom
                && (season < BloomExitSeasonThreshold
                    || energy <= cfg.FlowerBloomEnergyThreshold * 0.2f))
            {
                gain = Math.Max(gain, exitGain);
            }

            // Soft recovery when neither pressing stress nor deep dormancy.
            if (gain <= 0f
                && phase != FlowerPhenologyPhase.Dieback
                && season >= VegetativeSeasonThreshold
                && TemperatureSupportsGrowth(temp, cfg))
            {
                return 0f;
            }

            return gain;
        }

        /// <returns>True when the entry entered dieback or was removed (senescence death).</returns>
        internal static bool TryApplyDiebackFromStress(ICoreAPI api, ReproducerEntry entry, EcosystemConfig cfg)
        {
            if (entry == null || cfg == null) return false;
            if (entry.PhenologyPhase == FlowerPhenologyPhase.Dieback
                || entry.PhenologyPhase == FlowerPhenologyPhase.Dormant)
            {
                return false;
            }

            float enter = Math.Max(0.2f, cfg.FlowerPhenologyStressEnterDieback);
            if (entry.PhenologyStress < enter) return false;

            return TryEnterDieback(api, entry, cfg);
        }

        /// <returns>True when dieback entered or plant removed.</returns>
        internal static bool TryEnterDieback(ICoreAPI api, ReproducerEntry entry, EcosystemConfig cfg)
        {
            if (entry == null || cfg == null) return false;

            int maxCycles = ResolveMaxLifeCycles(entry.Requirements?.Species, cfg);
            if (maxCycles > 0 && entry.PhenologyLifeCycles >= maxCycles)
            {
                KillFromSenescence(api, entry);
                return true;
            }

            entry.PhenologyLifeCycles++;
            entry.PhenologyPhase = FlowerPhenologyPhase.Dieback;
            entry.PhenologyEnergy = 0f;
            // Leave stress near enter so recovery needs real decay (hysteresis).
            float enter = Math.Max(0.2f, cfg.FlowerPhenologyStressEnterDieback);
            if (entry.PhenologyStress < enter) entry.PhenologyStress = enter;
            return true;
        }

        /// <summary>
        /// Per-species CSV <c>flower_phenology_life_cycles</c> when &gt; 0; else global config.
        /// </summary>
        public static int ResolveMaxLifeCycles(string species, EcosystemConfig cfg)
        {
            if (SpeciesEcology.SpeciesEcologyRegistry.TryGetFlowerPhenologyLifeCycles(species, out int cycles)
                && cycles > 0)
            {
                return cycles;
            }

            return cfg?.MaxFlowerPhenologyLifeCycles ?? 0;
        }

        static void KillFromSenescence(ICoreAPI api, ReproducerEntry entry)
        {
            if (api == null || entry?.Origin == null) return;
            string species = entry.Requirements?.Species ?? string.Empty;
            EcologyHistoryRecorder.RecordPhenologySenescence(api, entry.Origin, species);
            EcosystemSystem.Instance?.FlowerPhenologyLife?.Remove(entry.Origin);
            EcosystemSystem.Instance?.RemoveEcologyPlant(
                entry.Origin,
                cascadeSymbiosis: true,
                reason: "phenology-senescence",
                soilEvent: SoilSuccessionEvent.Death);
        }

        static void AdvanceDormant(
            ReproducerEntry entry,
            float season,
            float temp,
            EcosystemConfig cfg,
            double deltaDays)
        {
            entry.PhenologyEnergy = Math.Max(0f, entry.PhenologyEnergy - (float)deltaDays * 0.05f);
            float exit = Math.Max(0f, cfg.FlowerPhenologyStressExitDieback);
            if (season >= VegetativeSeasonThreshold
                && TemperatureSupportsGrowth(temp, cfg)
                && entry.PhenologyStress <= exit)
            {
                entry.PhenologyPhase = FlowerPhenologyPhase.Vegetative;
            }
        }

        static void AdvanceVegetative(
            ReproducerEntry entry,
            float season,
            float temp,
            EcosystemConfig cfg,
            double deltaDays)
        {
            // Winter / frost must go through deferred dieback (counts a life-cycle), then dieback→dormant.
            if (season < BloomSeasonThreshold || !TemperatureSupportsGrowth(temp, cfg))
            {
                return;
            }

            float gain = (float)deltaDays * cfg.FlowerPhenologyEnergyGainPerDay * season;
            if (TemperatureSupportsBloom(temp, cfg))
            {
                gain *= 1.15f;
            }
            else if (temp < cfg.FlowerBloomMinTemperature)
            {
                gain *= 0.35f;
            }

            entry.PhenologyEnergy = Math.Min(cfg.FlowerBloomEnergyThreshold * 1.25f, entry.PhenologyEnergy + gain);

            if (entry.PhenologyEnergy >= cfg.FlowerBloomEnergyThreshold
                && season >= BloomSeasonThreshold
                && TemperatureSupportsBloom(temp, cfg)
                && entry.PhenologyStress <= cfg.FlowerPhenologyStressExitDieback)
            {
                entry.PhenologyPhase = FlowerPhenologyPhase.Bloom;
                entry.PhenologyEnergy = cfg.FlowerBloomEnergyThreshold;
            }
        }

        static void AdvanceBloom(
            ReproducerEntry entry,
            float season,
            float temp,
            EcosystemConfig cfg,
            double deltaDays)
        {
            entry.PhenologyEnergy -= (float)deltaDays * BloomDepletionPerDay;
            // Exit to dieback is deferred via UpdateStress + TryApplyDiebackFromStress.
        }

        static void AdvanceDieback(
            ReproducerEntry entry,
            float season,
            float temp,
            EcosystemConfig cfg,
            double deltaDays)
        {
            float exit = Math.Max(0f, cfg.FlowerPhenologyStressExitDieback);

            if (season < DormantSeasonThreshold)
            {
                entry.PhenologyPhase = FlowerPhenologyPhase.Dormant;
                entry.PhenologyEnergy = 0f;
                return;
            }

            if (season >= VegetativeSeasonThreshold
                && TemperatureSupportsGrowth(temp, cfg)
                && entry.PhenologyStress <= exit)
            {
                entry.PhenologyPhase = FlowerPhenologyPhase.Vegetative;
                entry.PhenologyEnergy = 0.08f;
            }
        }

        public static void SyncBlock(ICoreAPI api, ReproducerEntry entry, EcosystemConfig cfg)
        {
            if (!UsesPhenology(cfg, entry?.Requirements) || api?.World?.BlockAccessor == null) return;
            if (!LandClaimGuard.AllowsEcologyChange(api, entry.Origin)) return;

            Block current = api.World.BlockAccessor.GetBlock(entry.Origin);
            Block target = ResolveBlockForPhase(api, entry, current);
            if (target == null || target.Id == 0) return;

            if (current == null || current.Id == target.Id) return;

            api.World.BlockAccessor.SetBlock(target.Id, entry.Origin);
            api.World.BlockAccessor.MarkBlockDirty(entry.Origin);
        }

        static Block ResolveBlockForPhase(ICoreAPI api, ReproducerEntry entry, Block current)
        {
            bool snow = PlantSnowCover.ClimateWantsSnowCover(api, entry.Origin);

            if (entry.PhenologyPhase == FlowerPhenologyPhase.Bloom)
            {
                AssetLocation matureCode = PlantSnowCover.CodeWithCover(entry.MatureBlockCode, snow);
                Block mature = api.World.GetBlock(matureCode);
                if (mature != null && mature.Id != 0) return mature;
            }

            AssetLocation phaseCode = FlowerPhenologyBlocks.CodeForPhase(
                entry.Requirements.Species,
                entry.PhenologyPhase,
                snow);
            if (phaseCode != null)
            {
                Block phaseBlock = api.World.GetBlock(phaseCode);
                if (phaseBlock != null && phaseBlock.Id != 0) return phaseBlock;
            }

            AssetLocation juvenileCode = FlowerJuvenileBlocks.CodeForSpecies(entry.Requirements.Species, snow);
            if (juvenileCode == null) return null;
            return api.World.GetBlock(juvenileCode);
        }

        static void ResolveSeasonInputs(
            ICoreAPI api,
            BlockPos pos,
            string species,
            out float seasonActivity,
            out float temperature)
        {
            seasonActivity = 0f;
            temperature = 0f;
            if (api?.World?.Calendar == null || pos == null) return;

            WildSpeciesSeason.Profile profile = WildSpeciesSeason.Resolve(species);
            float yearProgress = api.World.Calendar.DayOfYearf / api.World.Calendar.DaysPerYear;
            seasonActivity = profile.SpreadMultiplierInterpolated(yearProgress);

            EnvironmentalContext ctx = EnvironmentalContext.SampleForSurvival(api, pos);
            temperature = ctx.Temperature;
        }

        static bool TemperatureSupportsBloom(float temp, EcosystemConfig cfg) =>
            temp >= cfg.FlowerBloomMinTemperature && temp <= cfg.FlowerBloomMaxTemperature;

        static bool TemperatureSupportsGrowth(float temp, EcosystemConfig cfg) =>
            temp >= cfg.FlowerBloomMinTemperature - 4f && temp <= cfg.FlowerBloomMaxTemperature + 2f;

        internal static void AdvanceVegetativeForTests(
            ReproducerEntry entry,
            float season,
            float temp,
            EcosystemConfig cfg,
            double deltaDays) =>
            AdvanceVegetative(entry, season, temp, cfg, deltaDays);

        internal static void AdvanceBloomForTests(
            ReproducerEntry entry,
            float season,
            float temp,
            EcosystemConfig cfg,
            double deltaDays) =>
            AdvanceBloom(entry, season, temp, cfg, deltaDays);

        internal static void UpdateStressForTests(
            ReproducerEntry entry,
            float season,
            float temp,
            EcosystemConfig cfg,
            double deltaDays) =>
            UpdateStress(entry, season, temp, cfg, deltaDays);
    }
}
