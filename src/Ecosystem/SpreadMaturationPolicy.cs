using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Shared spread-maturation + post-spread-cooldown engine. The hours math (growth multiplier,
    /// seasonal scaling, floors/clamps) is identical across plant families; only the per-family data
    /// differs (profile tables, config flags, floors, clamps, terrestrial guard). Each family builds
    /// one instance and delegates its public API here, so behavior is preserved while the math lives once.
    /// </summary>
    internal sealed class SpreadMaturationPolicy
    {
        /// <summary>Resolve a species' base maturation/cooldown hours; false routes to fallbacks.</summary>
        public delegate bool TryGetBaseHoursDelegate(string species, out double maturationHours, out double cooldownHours);

        readonly System.Func<EcosystemConfig, bool> maturationEnabled;
        readonly System.Func<EcosystemConfig, bool> cooldownEnabled;
        readonly System.Func<EcosystemConfig, float> cooldownMultiplier;
        readonly System.Func<string, bool> isMember;
        readonly TryGetBaseHoursDelegate tryGetBaseHours;
        readonly double maturationFallbackHours;
        readonly double cooldownFallbackHours;
        readonly double maturationFloor;
        readonly double cooldownFloor;
        readonly double failedBaseHours;
        readonly double failedFloor;
        readonly double failedCap;
        readonly bool requiresTerrestrialForCooldown;

        public SpreadMaturationPolicy(
            System.Func<EcosystemConfig, bool> maturationEnabled,
            System.Func<EcosystemConfig, bool> cooldownEnabled,
            System.Func<EcosystemConfig, float> cooldownMultiplier,
            System.Func<string, bool> isMember,
            TryGetBaseHoursDelegate tryGetBaseHours,
            double maturationFallbackHours,
            double cooldownFallbackHours,
            double maturationFloor,
            double cooldownFloor,
            double failedBaseHours,
            double failedFloor,
            double failedCap,
            bool requiresTerrestrialForCooldown)
        {
            this.maturationEnabled = maturationEnabled;
            this.cooldownEnabled = cooldownEnabled;
            this.cooldownMultiplier = cooldownMultiplier;
            this.isMember = isMember;
            this.tryGetBaseHours = tryGetBaseHours;
            this.maturationFallbackHours = maturationFallbackHours;
            this.cooldownFallbackHours = cooldownFallbackHours;
            this.maturationFloor = maturationFloor;
            this.cooldownFloor = cooldownFloor;
            this.failedBaseHours = failedBaseHours;
            this.failedFloor = failedFloor;
            this.failedCap = failedCap;
            this.requiresTerrestrialForCooldown = requiresTerrestrialForCooldown;
        }

        public bool IsMember(string species) => isMember(species);

        public bool UsesMaturation(EcosystemConfig cfg, string species)
        {
            if (cfg == null || !maturationEnabled(cfg)) return false;
            return isMember(species);
        }

        public bool UsesPostSpreadAttemptCooldown(EcosystemConfig cfg, string species)
        {
            if (cfg == null || !cooldownEnabled(cfg)) return false;
            return isMember(species);
        }

        public double MaturationHours(ICoreAPI api, BlockPos pos, string species, EcosystemConfig cfg)
        {
            double hours = tryGetBaseHours(species, out double m, out _) ? m : maturationFallbackHours;

            if (cfg != null && cfg.GrowthHoursMultiplier > 0.05f)
            {
                hours /= cfg.GrowthHoursMultiplier;
            }

            if (cfg != null && cfg.UseSeasonalEcology && api != null && pos != null)
            {
                var req = new PlantRequirements { Species = species };
                float season = SeasonEcology.SpreadActivityMultiplier(api, pos, req);
                if (season > 0.05f)
                {
                    hours /= System.Math.Min(season, 2f);
                }
            }

            if (hours < maturationFloor) hours = maturationFloor;
            return hours;
        }

        public double PostSpreadAttemptCooldownHours(ICoreAPI api, BlockPos pos, PlantRequirements requirements, EcosystemConfig cfg)
        {
            string species = requirements?.Species;
            double hours = tryGetBaseHours(species, out _, out double c) ? c : cooldownFallbackHours;

            float mult = cfg != null ? cooldownMultiplier(cfg) : 0f;
            if (cfg != null && mult > 0.05f)
            {
                hours /= mult;
            }

            if (cfg != null && cfg.UseSeasonalEcology && api != null && pos != null && requirements != null)
            {
                float season = SeasonEcology.SpreadActivityMultiplier(api, pos, requirements);
                if (season > 1.05f)
                {
                    hours /= System.Math.Min(season, 2f);
                }
            }

            if (hours < cooldownFloor) hours = cooldownFloor;
            return hours;
        }

        public double FailedSpreadAttemptCooldownHours(ICoreAPI api, BlockPos pos, PlantRequirements requirements, EcosystemConfig cfg)
        {
            double hours = failedBaseHours;

            float mult = cfg != null ? cooldownMultiplier(cfg) : 0f;
            if (cfg != null && mult > 0.05f)
            {
                hours /= mult;
            }

            if (cfg != null && cfg.UseSeasonalEcology && api != null && pos != null && requirements != null)
            {
                float season = SeasonEcology.SpreadActivityMultiplier(api, pos, requirements);
                if (season > 1.05f)
                {
                    hours /= System.Math.Min(season, 2f);
                }
            }

            if (hours < failedFloor) hours = failedFloor;
            if (hours > failedCap) hours = failedCap;
            return hours;
        }

        public bool TryApplySpreadAttemptCooldown(
            ReproducerEntry parent,
            double nowHours,
            ICoreAPI api,
            BlockPos pos,
            PlantRequirements requirements,
            EcosystemConfig cfg,
            bool failedChanceRoll)
        {
            if (parent == null || requirements == null || cfg == null) return false;
            if (requiresTerrestrialForCooldown && requirements.Habitat != EcologyHabitat.Terrestrial) return false;
            if (!UsesPostSpreadAttemptCooldown(cfg, requirements.Species)) return false;

            double cooldown = failedChanceRoll
                ? FailedSpreadAttemptCooldownHours(api, pos, requirements, cfg)
                : PostSpreadAttemptCooldownHours(api, pos, requirements, cfg);
            parent.NextSpawnAllowedAtHours = nowHours + cooldown;
            return true;
        }
    }
}
