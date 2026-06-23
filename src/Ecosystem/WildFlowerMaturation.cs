using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Per-species spread maturation and post-spread-attempt cooldown for meadow flowers.</summary>
    internal static class WildFlowerMaturation
    {
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

        static readonly Profile DefaultColonizer = new Profile(42, 18);
        static readonly Profile DefaultBiennial = new Profile(72, 24);
        static readonly Profile DefaultSteady = new Profile(48, 24);
        static readonly Profile DefaultSlow = new Profile(72, 36);
        static readonly Profile DefaultForest = new Profile(64, 28);
        static readonly Profile DefaultRare = new Profile(84, 42);

        static readonly System.Collections.Generic.Dictionary<string, Profile> BySpecies =
            new System.Collections.Generic.Dictionary<string, Profile>
            {
                ["cowparsley"] = new Profile(42, 18),
                ["horsetail"] = new Profile(36, 16),
                ["mugwort"] = new Profile(36, 16),
                ["lupine"] = new Profile(40, 18),
                ["woad"] = DefaultBiennial,
                ["redtopgrass"] = new Profile(36, 16),
                ["heather"] = new Profile(40, 18),
                ["westerngorse"] = new Profile(40, 18),
                ["catmint"] = DefaultSteady,
                ["cornflower"] = DefaultSteady,
                ["wilddaisy"] = DefaultSteady,
                ["forgetmenot"] = DefaultSteady,
            };

        public static bool UsesMaturation(EcosystemConfig cfg, string species)
        {
            if (cfg == null || !cfg.EnableFlowerSpreadMaturation) return false;
            return IsFlowerSpreadSpecies(species);
        }

        /// <summary>Post-spread cooldown applies even when juvenile maturation is disabled.</summary>
        public static bool UsesPostSpreadAttemptCooldown(EcosystemConfig cfg, string species)
        {
            if (cfg == null || !cfg.EnableFlowerSpreadAttemptCooldown) return false;
            return IsFlowerSpreadSpecies(species);
        }

        static bool IsFlowerSpreadSpecies(string species)
        {
            if (string.IsNullOrEmpty(species)) return false;
            if (EcologyFlowerSpecies.IsKnownFlower(species)) return true;
            return EcologyGrassColonizerSpecies.IsKnown(species);
        }

        public static bool TryGetProfile(string species, out Profile profile)
        {
            if (species != null && BySpecies.TryGetValue(species, out profile))
            {
                return true;
            }

            if (!EcologyFlowerSpecies.IsKnownFlower(species))
            {
                profile = default;
                return false;
            }

            profile = ResolveDefaultProfile(species);
            return true;
        }

        static Profile ResolveDefaultProfile(string species)
        {
            switch (species)
            {
                case "daffodil":
                case "edelweiss":
                case "goldenpoppy":
                case "orangemallow":
                case "ghostpipewhite":
                case "ghostpipepink":
                case "ghostpipered":
                    return DefaultSlow;
                case "bluebell":
                case "lilyofthevalley":
                    return DefaultForest;
                case "croton":
                case "rafflesiabrown":
                case "rafflesiared":
                    return DefaultRare;
                default:
                    return DefaultSteady;
            }
        }

        public static double MaturationHours(ICoreAPI api, BlockPos pos, string species, EcosystemConfig cfg)
        {
            if (!TryGetProfile(species, out Profile profile))
            {
                profile = DefaultColonizer;
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

            if (hours < 6) hours = 6;
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
                profile = DefaultSteady;
            }

            double hours = profile.PostSpreadAttemptCooldownHours;
            if (cfg != null && cfg.FlowerSpreadCooldownHoursMultiplier > 0.05f)
            {
                hours /= cfg.FlowerSpreadCooldownHoursMultiplier;
            }

            if (cfg != null && cfg.UseSeasonalEcology && api != null && pos != null && requirements != null)
            {
                float season = SeasonEcology.SpreadActivityMultiplier(api, pos, requirements);
                if (season > 1.05f)
                {
                    hours /= System.Math.Min(season, 2f);
                }
            }

            if (hours < 1) hours = 1;
            return hours;
        }

        /// <summary>Short anti-spam pause when the spread chance roll fails (no placement attempt).</summary>
        public static double FailedSpreadAttemptCooldownHours(
            ICoreAPI api,
            BlockPos pos,
            PlantRequirements requirements,
            EcosystemConfig cfg)
        {
            const double baseHours = 3;
            double hours = baseHours;
            if (cfg != null && cfg.FlowerSpreadCooldownHoursMultiplier > 0.05f)
            {
                hours /= cfg.FlowerSpreadCooldownHoursMultiplier;
            }

            if (cfg != null && cfg.UseSeasonalEcology && api != null && pos != null && requirements != null)
            {
                float season = SeasonEcology.SpreadActivityMultiplier(api, pos, requirements);
                if (season > 1.05f)
                {
                    hours /= System.Math.Min(season, 2f);
                }
            }

            if (hours < 1) hours = 1;
            if (hours > 4) hours = 4;
            return hours;
        }

        /// <returns>True when cooldown was applied to the parent entry.</returns>
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
            if (requirements.Habitat != EcologyHabitat.Terrestrial) return false;
            if (!UsesPostSpreadAttemptCooldown(cfg, requirements.Species)) return false;

            double cooldown = failedChanceRoll
                ? FailedSpreadAttemptCooldownHours(api, pos, requirements, cfg)
                : PostSpreadAttemptCooldownHours(api, pos, requirements, cfg);
            parent.NextSpawnAllowedAtHours = nowHours + cooldown;
            return true;
        }
    }
}
