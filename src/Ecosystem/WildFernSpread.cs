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

        internal static readonly SpreadMaturationPolicy Policy = new SpreadMaturationPolicy(
            maturationEnabled: cfg => cfg.EnableFernSpreadMaturation,
            cooldownEnabled: cfg => cfg.EnableFernSpreadAttemptCooldown,
            cooldownMultiplier: cfg => cfg.FernSpreadCooldownHoursMultiplier,
            isMember: IsFernSpecies,
            tryGetBaseHours: TryGetBaseHours,
            maturationFallbackHours: DefaultForest.MaturationHours,
            cooldownFallbackHours: DefaultForest.PostSpreadAttemptCooldownHours,
            maturationFloor: 8,
            cooldownFloor: 2,
            failedBaseHours: 4,
            failedFloor: 2,
            failedCap: 6,
            requiresTerrestrialForCooldown: false);

        public static bool IsFernSpecies(string species)
        {
            return EcologyFernSpecies.IsKnown(species);
        }

        public static bool UsesMaturation(EcosystemConfig cfg, string species)
        {
            return Policy.UsesMaturation(cfg, species);
        }

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

        public static double FailedSpreadAttemptCooldownHours(
            ICoreAPI api,
            BlockPos pos,
            PlantRequirements requirements,
            EcosystemConfig cfg)
        {
            return Policy.FailedSpreadAttemptCooldownHours(api, pos, requirements, cfg);
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
            return Policy.TryApplySpreadAttemptCooldown(parent, nowHours, api, pos, requirements, cfg, failedChanceRoll);
        }
    }
}
