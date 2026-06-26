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

        /// <summary>
        /// Pure numeric tuning for a plant family: the fallback hours used when a species has no profile,
        /// the floors applied to the successful maturation/cooldown values, and the base/floor/cap clamps
        /// for the failed-roll cooldown. Grouping these here keeps the policy constructor about behavior
        /// (the enable flags, membership test, and hours resolver) rather than a long list of magic numbers.
        /// </summary>
        public readonly struct Clamps
        {
            public readonly double MaturationFallbackHours;
            public readonly double CooldownFallbackHours;
            public readonly double MaturationFloor;
            public readonly double CooldownFloor;
            public readonly double FailedBaseHours;
            public readonly double FailedFloor;
            public readonly double FailedCap;

            public Clamps(
                double maturationFallbackHours,
                double cooldownFallbackHours,
                double maturationFloor,
                double cooldownFloor,
                double failedBaseHours,
                double failedFloor,
                double failedCap)
            {
                MaturationFallbackHours = maturationFallbackHours;
                CooldownFallbackHours = cooldownFallbackHours;
                MaturationFloor = maturationFloor;
                CooldownFloor = cooldownFloor;
                FailedBaseHours = failedBaseHours;
                FailedFloor = failedFloor;
                FailedCap = failedCap;
            }
        }

        readonly System.Func<EcosystemConfig, bool> maturationEnabled;
        readonly System.Func<EcosystemConfig, bool> cooldownEnabled;
        readonly System.Func<EcosystemConfig, float> cooldownMultiplier;
        readonly System.Func<string, bool> isMember;
        readonly TryGetBaseHoursDelegate tryGetBaseHours;
        readonly Clamps clamps;
        readonly bool requiresTerrestrialForCooldown;

        public SpreadMaturationPolicy(
            System.Func<EcosystemConfig, bool> maturationEnabled,
            System.Func<EcosystemConfig, bool> cooldownEnabled,
            System.Func<EcosystemConfig, float> cooldownMultiplier,
            System.Func<string, bool> isMember,
            TryGetBaseHoursDelegate tryGetBaseHours,
            Clamps clamps,
            bool requiresTerrestrialForCooldown)
        {
            this.maturationEnabled = maturationEnabled;
            this.cooldownEnabled = cooldownEnabled;
            this.cooldownMultiplier = cooldownMultiplier;
            this.isMember = isMember;
            this.tryGetBaseHours = tryGetBaseHours;
            this.clamps = clamps;
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
            double hours = tryGetBaseHours(species, out double m, out _) ? m : clamps.MaturationFallbackHours;

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

            if (hours < clamps.MaturationFloor) hours = clamps.MaturationFloor;
            return hours;
        }

        public double PostSpreadAttemptCooldownHours(ICoreAPI api, BlockPos pos, PlantRequirements requirements, EcosystemConfig cfg)
        {
            string species = requirements?.Species;
            double hours = tryGetBaseHours(species, out _, out double c) ? c : clamps.CooldownFallbackHours;

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

            if (hours < clamps.CooldownFloor) hours = clamps.CooldownFloor;
            return hours;
        }

        public double FailedSpreadAttemptCooldownHours(ICoreAPI api, BlockPos pos, PlantRequirements requirements, EcosystemConfig cfg)
        {
            double hours = clamps.FailedBaseHours;

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

            if (hours < clamps.FailedFloor) hours = clamps.FailedFloor;
            if (hours > clamps.FailedCap) hours = clamps.FailedCap;
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
