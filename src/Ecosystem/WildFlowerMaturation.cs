using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem.SpeciesEcology;

// Export-only C# tables: fallback when SpeciesEcologyRegistry is not loaded.
#pragma warning disable CS0618

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Meadow flower / grass-colonizer spread maturation and post-spread cooldown.
    /// Cooldown-only members (no juvenile maturation) use the same config flags and profile table.
    /// </summary>
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
                [EcologyShoreSedgeSpecies.Brownsedge] = new Profile(120, 48),
                ["heather"] = new Profile(40, 18),
                ["westerngorse"] = new Profile(40, 18),
                ["catmint"] = DefaultSteady,
                ["cornflower"] = DefaultSteady,
                ["wilddaisy"] = DefaultSteady,
                ["forgetmenot"] = DefaultSteady,
            };

        internal static readonly SpreadMaturationPolicy Policy = new SpreadMaturationPolicy(
            maturationEnabled: cfg => cfg.EnableFlowerSpreadMaturation,
            cooldownEnabled: cfg => cfg.EnableFlowerSpreadAttemptCooldown,
            cooldownMultiplier: cfg => cfg.FlowerSpreadCooldownHoursMultiplier,
            isMember: UsesSpreadCooldown,
            tryGetBaseHours: TryGetBaseHours,
            clamps: new SpreadMaturationPolicy.Clamps(
                maturationFallbackHours: DefaultColonizer.MaturationHours,
                cooldownFallbackHours: DefaultSteady.PostSpreadAttemptCooldownHours,
                maturationFloor: 6,
                cooldownFloor: 1,
                failedBaseHours: 3,
                failedFloor: 1,
                failedCap: 4),
            requiresTerrestrialForCooldown: true,
            isMaturationMember: UsesJuvenileSpreadMaturation,
            isCooldownMember: UsesSpreadCooldown);

        public static bool UsesMaturation(EcosystemConfig cfg, string species)
        {
            return Policy.UsesMaturation(cfg, species);
        }

        /// <summary>Post-spread cooldown applies even when juvenile maturation is disabled.</summary>
        public static bool UsesPostSpreadAttemptCooldown(EcosystemConfig cfg, string species)
        {
            return Policy.UsesPostSpreadAttemptCooldown(cfg, species);
        }

        static bool TryGetBaseHours(string species, out double maturationHours, out double cooldownHours)
        {
            if (TryGetProfile(species, out Profile profile))
            {
                maturationHours = profile.MaturationHours;
                cooldownHours = profile.PostSpreadAttemptCooldownHours;
                return true;
            }

            maturationHours = 0;
            cooldownHours = 0;
            return false;
        }

        static bool UsesJuvenileSpreadMaturation(string species)
        {
            if (string.IsNullOrEmpty(species)) return false;
            if (WildShoreSedgeEcology.IsSpecies(species)) return true;
            if (EcologyFlowerSpecies.IsKnownFlower(species)) return true;
            return EcologyGrassColonizerSpecies.IsKnown(species);
        }

        static bool UsesSpreadCooldown(string species)
        {
            if (UsesJuvenileSpreadMaturation(species)) return true;
            return WildShoreSedgeEcology.IsSpecies(species);
        }

        public static bool TryGetProfile(string species, out Profile profile)
        {
            if (SpeciesEcologyRegistry.IsLoaded
                && SpeciesEcologyRegistry.TryGetFlowerMaturation(species, out double maturationHours, out double cooldownHours))
            {
                profile = new Profile(
                    maturationHours > 0 ? maturationHours : DefaultSteady.MaturationHours,
                    cooldownHours > 0 ? cooldownHours : DefaultSteady.PostSpreadAttemptCooldownHours);
                return true;
            }

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
            return Policy.MaturationHours(api, pos, species, cfg);
        }

        public static double PostSpreadAttemptCooldownHours(
            ICoreAPI api,
            BlockPos pos,
            PlantRequirements requirements,
            EcosystemConfig cfg)
        {
            return Policy.PostSpreadAttemptCooldownHours(api, pos, requirements, cfg);
        }

        /// <summary>Short anti-spam pause when the spread chance roll fails (no placement attempt).</summary>
        public static double FailedSpreadAttemptCooldownHours(
            ICoreAPI api,
            BlockPos pos,
            PlantRequirements requirements,
            EcosystemConfig cfg)
        {
            return Policy.FailedSpreadAttemptCooldownHours(api, pos, requirements, cfg);
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
            return Policy.TryApplySpreadAttemptCooldown(parent, nowHours, api, pos, requirements, cfg, failedChanceRoll);
        }
    }
}
