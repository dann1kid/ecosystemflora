using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Per-species spread maturation and post-spawn cooldown for colonizer meadow flowers.</summary>
    internal static class WildFlowerMaturation
    {
        public readonly struct Profile
        {
            public readonly double MaturationHours;
            public readonly double PostSpawnCooldownHours;

            public Profile(double maturationHours, double postSpawnCooldownHours)
            {
                MaturationHours = maturationHours;
                PostSpawnCooldownHours = postSpawnCooldownHours;
            }
        }

        static readonly Profile DefaultColonizer = new Profile(42, 18);
        static readonly Profile DefaultBiennial = new Profile(72, 24);

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
            };

        public static bool UsesMaturation(EcosystemConfig cfg, string species)
        {
            if (cfg == null || !cfg.EnableFlowerSpreadMaturation) return false;
            if (string.IsNullOrEmpty(species)) return false;
            return BySpecies.ContainsKey(species);
        }

        public static bool TryGetProfile(string species, out Profile profile)
        {
            if (species != null && BySpecies.TryGetValue(species, out profile))
            {
                return true;
            }

            profile = default;
            return false;
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

        public static double PostSpawnCooldownHours(
            ICoreAPI api,
            BlockPos pos,
            PlantRequirements requirements,
            EcosystemConfig cfg)
        {
            string species = requirements?.Species;
            if (!TryGetProfile(species, out Profile profile))
            {
                profile = DefaultColonizer;
            }

            double hours = profile.PostSpawnCooldownHours;
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
    }
}
