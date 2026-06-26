using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Fern spread maturation, post-attempt cooldown, and seasonal sporulation gate.</summary>
    internal static class WildFernSpread
    {
        const float SporulationSeasonThreshold = 0.35f;

        public readonly struct Profile
        {
            public readonly double MaturationHours;
            public readonly double PostSpreadAttemptCooldownHours;

            public Profile(double maturationHours, double postSpreadAttemptCooldownHours)
            {
                MaturationHours = maturationHours;
                PostSpreadAttemptCooldownHours = postSpreadAttemptCooldownHours;
            }
        }

        static readonly Profile DefaultForest = new Profile(56, 32);
        static readonly Profile ColdWetForest = new Profile(62, 36);
        static readonly Profile TemperateWetForest = new Profile(50, 28);
        static readonly Profile BorealForest = new Profile(58, 34);
        static readonly Profile DefaultEdge = new Profile(46, 26);
        static readonly Profile Hartstongue = new Profile(44, 24);

        static readonly Dictionary<string, Profile> BySpecies = new Dictionary<string, Profile>
        {
            ["eaglefern"] = BorealForest,
            ["cinnamonfern"] = ColdWetForest,
            ["deerfern"] = TemperateWetForest,
            ["tallfern"] = DefaultEdge,
            ["hartstongue"] = Hartstongue,
        };

        public static bool IsFernSpecies(string species)
        {
            return EcologyFernSpecies.IsKnown(species);
        }

        public static bool UsesMaturation(EcosystemConfig cfg, string species)
        {
            if (cfg == null || !cfg.EnableFernSpreadMaturation) return false;
            return IsFernSpecies(species);
        }

        public static bool UsesPostSpreadAttemptCooldown(EcosystemConfig cfg, string species)
        {
            if (cfg == null || !cfg.EnableFernSpreadAttemptCooldown) return false;
            return IsFernSpecies(species);
        }

        public static bool UsesSporulationGate(EcosystemConfig cfg, PlantRequirements requirements)
        {
            if (cfg == null || !cfg.EnableFernSporulationGate || requirements == null) return false;
            return IsFernSpecies(requirements.Species);
        }

        public static bool CanSpread(ICoreAPI api, ReproducerEntry entry, EcosystemConfig cfg)
        {
            if (!UsesSporulationGate(cfg, entry?.Requirements)) return true;
            if (api == null || entry?.Origin == null || entry.Requirements == null) return false;

            float season = SeasonEcology.SpreadActivityMultiplier(api, entry.Origin, entry.Requirements);
            return IsSporulationSeasonActive(season);
        }

        internal static bool IsSporulationSeasonActive(float spreadActivityMultiplier)
        {
            return spreadActivityMultiplier >= SporulationSeasonThreshold;
        }

        public static bool TryGetProfile(string species, out Profile profile)
        {
            if (species != null && BySpecies.TryGetValue(species, out profile))
            {
                return true;
            }

            profile = DefaultForest;
            return IsFernSpecies(species);
        }

        public static double MaturationHours(ICoreAPI api, BlockPos pos, string species, EcosystemConfig cfg)
        {
            if (!TryGetProfile(species, out Profile profile))
            {
                profile = DefaultForest;
            }

            double hours = profile.MaturationHours;
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

            if (hours < 8) hours = 8;
            return hours;
        }

        public static double PostSpreadAttemptCooldownHours(
            ICoreAPI api,
            BlockPos pos,
            PlantRequirements requirements,
            EcosystemConfig cfg)
        {
            string species = requirements?.Species;
            if (!TryGetProfile(species, out Profile profile))
            {
                profile = DefaultForest;
            }

            double hours = profile.PostSpreadAttemptCooldownHours;
            if (cfg != null && cfg.FernSpreadCooldownHoursMultiplier > 0.05f)
            {
                hours /= cfg.FernSpreadCooldownHoursMultiplier;
            }

            if (cfg != null && cfg.UseSeasonalEcology && api != null && pos != null && requirements != null)
            {
                float season = SeasonEcology.SpreadActivityMultiplier(api, pos, requirements);
                if (season > 1.05f)
                {
                    hours /= System.Math.Min(season, 2f);
                }
            }

            if (hours < 2) hours = 2;
            return hours;
        }

        public static double FailedSpreadAttemptCooldownHours(
            ICoreAPI api,
            BlockPos pos,
            PlantRequirements requirements,
            EcosystemConfig cfg)
        {
            const double baseHours = 4;
            double hours = baseHours;
            if (cfg != null && cfg.FernSpreadCooldownHoursMultiplier > 0.05f)
            {
                hours /= cfg.FernSpreadCooldownHoursMultiplier;
            }

            if (cfg != null && cfg.UseSeasonalEcology && api != null && pos != null && requirements != null)
            {
                float season = SeasonEcology.SpreadActivityMultiplier(api, pos, requirements);
                if (season > 1.05f)
                {
                    hours /= System.Math.Min(season, 2f);
                }
            }

            if (hours < 2) hours = 2;
            if (hours > 6) hours = 6;
            return hours;
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
            if (!UsesPostSpreadAttemptCooldown(cfg, requirements.Species)) return false;

            double cooldown = failedChanceRoll
                ? FailedSpreadAttemptCooldownHours(api, pos, requirements, cfg)
                : PostSpreadAttemptCooldownHours(api, pos, requirements, cfg);
            parent.NextSpawnAllowedAtHours = nowHours + cooldown;
            return true;
        }
    }
}
