using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Meadow flower life phases driven by season curves, local temperature, and stored energy.
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

            Block block = api.World.BlockAccessor.GetBlock(entry.Origin);
            entry.PhenologyPhase = ResolveRegisterPhase(api, entry, block, cfg, spreadEstablished);
            entry.PhenologyEnergy = entry.PhenologyPhase == FlowerPhenologyPhase.Bloom
                ? cfg.FlowerBloomEnergyThreshold
                : spreadEstablished ? 0.25f : 0.12f;

            SyncBlock(api, entry, cfg);
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

            if (temp > cfg.FlowerBloomMaxTemperature) return FlowerPhenologyPhase.Dieback;
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

            PlantSnowCoverSync.TrySyncCover(api, entry.Origin);
        }

        static void AdvanceDormant(
            ReproducerEntry entry,
            float season,
            float temp,
            EcosystemConfig cfg,
            double deltaDays)
        {
            entry.PhenologyEnergy = Math.Max(0f, entry.PhenologyEnergy - (float)deltaDays * 0.05f);
            if (season >= VegetativeSeasonThreshold && TemperatureSupportsGrowth(temp, cfg))
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
            if (season < DormantSeasonThreshold)
            {
                entry.PhenologyPhase = FlowerPhenologyPhase.Dormant;
                entry.PhenologyEnergy = 0f;
                return;
            }

            if (temp > cfg.FlowerBloomMaxTemperature)
            {
                entry.PhenologyPhase = FlowerPhenologyPhase.Dieback;
                entry.PhenologyEnergy = 0f;
                return;
            }

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
                && TemperatureSupportsBloom(temp, cfg))
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

            if (temp > cfg.FlowerBloomMaxTemperature
                || season < BloomExitSeasonThreshold
                || entry.PhenologyEnergy <= cfg.FlowerBloomEnergyThreshold * 0.2f)
            {
                entry.PhenologyPhase = FlowerPhenologyPhase.Dieback;
                entry.PhenologyEnergy = 0f;
            }
        }

        static void AdvanceDieback(
            ReproducerEntry entry,
            float season,
            float temp,
            EcosystemConfig cfg,
            double deltaDays)
        {
            if (season < DormantSeasonThreshold)
            {
                entry.PhenologyPhase = FlowerPhenologyPhase.Dormant;
                entry.PhenologyEnergy = 0f;
                return;
            }

            if (season >= VegetativeSeasonThreshold && TemperatureSupportsGrowth(temp, cfg))
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
            bool snow = PlantSnowCover.ResolveWantsSnowCover(api, entry.Origin);

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

            // Fallback when phase assets are missing (dev / third-party species).
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
    }
}
