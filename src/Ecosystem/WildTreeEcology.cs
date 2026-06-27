using System;
using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Wild spread of vanilla saplings from mature log-grown trunks (no mod trunk blocks).</summary>
    internal static class WildTreeEcology
    {
        public readonly struct Profile
        {
            public readonly float MinTemp;
            public readonly float MaxTemp;
            public readonly float MinRain;
            public readonly float MaxRain;
            public readonly float MinForest;
            public readonly float MaxForest;
            /// <summary>Relative spread vigor (1 = config baseline).</summary>
            public readonly float SpreadRate;
            /// <summary>Horizontal search radius for sapling placement.</summary>
            public readonly int SpreadRadius;
            public readonly int SameSpeciesSpacing;
            public readonly int OtherSpeciesSpacing;
            public readonly TreeSeralRole SeralRole;
            /// <summary>Minimum sunlight at sapling cell (open pioneers need more light).</summary>
            public readonly int SaplingMinSunlight;

            public Profile(
                float minTemp, float maxTemp,
                float minRain, float maxRain,
                float minForest, float maxForest,
                float spreadRate,
                int spreadRadius,
                int sameSpeciesSpacing,
                int otherSpeciesSpacing,
                TreeSeralRole seralRole,
                int saplingMinSunlight = 0)
            {
                MinTemp = minTemp;
                MaxTemp = maxTemp;
                MinRain = minRain;
                MaxRain = maxRain;
                MinForest = minForest;
                MaxForest = maxForest;
                SpreadRate = spreadRate;
                SpreadRadius = spreadRadius;
                SameSpeciesSpacing = sameSpeciesSpacing;
                OtherSpeciesSpacing = otherSpeciesSpacing;
                SeralRole = seralRole;
                SaplingMinSunlight = saplingMinSunlight > 0
                    ? saplingMinSunlight
                    : DefaultSaplingMinSunlight(seralRole);
            }
        }

        /// <summary>Wood codes from game sapling / log-grown (bamboo excluded — vanilla shoots).</summary>
        public static readonly IReadOnlyList<string> AllWoods = new[]
        {
            "birch", "oak", "maple", "pine", "acacia", "kapok", "larch",
            "crimsonkingmaple", "redwood", "baldcypress", "greenspirecypress",
            "ebony", "purpleheart", "walnut",
        };

        static readonly Dictionary<string, Profile> ByWood = new Dictionary<string, Profile>
        {
            // Pioneers — open ground, low forest cover, fast turnover
            ["birch"] = P(-7, 39, 0.35f, 1f, 0f, 0.42f, 0.62f, 8, 7, 4, TreeSeralRole.Pioneer),
            ["acacia"] = P(21, 60, 0.15f, 0.6f, 0f, 0.40f, 0.44f, 10, 9, 5, TreeSeralRole.Pioneer),

            // Mid-seral — edges and young stands
            ["maple"] = P(-7, 40, 0.35f, 1f, 0.15f, 0.82f, 0.48f, 8, 7, 4, TreeSeralRole.Mid),
            ["crimsonkingmaple"] = P(1, 26, 0.4f, 0.9f, 0.12f, 0.75f, 0.46f, 8, 7, 4, TreeSeralRole.Mid),
            ["walnut"] = P(-1, 39, 0.35f, 0.9f, 0.18f, 0.88f, 0.40f, 8, 7, 4, TreeSeralRole.Mid),
            ["greenspirecypress"] = P(1, 39, 0.4f, 1f, 0.14f, 0.82f, 0.42f, 8, 7, 4, TreeSeralRole.Mid),
            ["baldcypress"] = P(8, 41, 0.45f, 1f, 0.16f, 0.90f, 0.43f, 9, 8, 4, TreeSeralRole.Mid),
            ["purpleheart"] = P(20, 50, 0.3f, 0.85f, 0.20f, 0.88f, 0.34f, 10, 9, 5, TreeSeralRole.Mid),

            // Climax — mature forest, slower spread, longer lifespan
            ["oak"] = P(-5, 40, 0.35f, 1f, 0.28f, 1f, 0.44f, 8, 7, 4, TreeSeralRole.Climax),
            ["pine"] = P(-18, 30, 0.3f, 1f, 0.20f, 1f, 0.40f, 9, 8, 4, TreeSeralRole.Climax),
            ["larch"] = P(-24, 15, 0.35f, 1f, 0.16f, 0.95f, 0.36f, 9, 8, 4, TreeSeralRole.Climax),
            ["kapok"] = P(20, 50, 0.35f, 1f, 0.26f, 1f, 0.34f, 9, 8, 4, TreeSeralRole.Climax),
            ["redwood"] = P(7, 35, 0.4f, 1f, 0.32f, 1f, 0.30f, 10, 9, 5, TreeSeralRole.Climax, saplingMinSunlight: 10),
            ["ebony"] = P(21, 50, 0.25f, 0.75f, 0.30f, 1f, 0.26f, 10, 9, 5, TreeSeralRole.Climax, saplingMinSunlight: 9),
        };

        static Profile P(
            float minTemp, float maxTemp,
            float minRain, float maxRain,
            float minForest, float maxForest,
            float spreadRate,
            int spreadRadius,
            int sameSpeciesSpacing,
            int otherSpeciesSpacing,
            TreeSeralRole seralRole,
            int saplingMinSunlight = 0)
        {
            return new Profile(
                minTemp, maxTemp, minRain, maxRain,
                minForest, maxForest, spreadRate,
                spreadRadius, sameSpeciesSpacing, otherSpeciesSpacing,
                seralRole, saplingMinSunlight);
        }

        public static bool TryGet(string wood, out Profile profile)
        {
            profile = default;
            if (string.IsNullOrEmpty(wood)) return false;
            return ByWood.TryGetValue(wood, out profile);
        }

        public static int DefaultSaplingMinSunlight(TreeSeralRole role)
        {
            switch (role)
            {
                case TreeSeralRole.Pioneer: return 11;
                case TreeSeralRole.Mid: return 10;
                default: return 10;
            }
        }

        /// <summary>Soft multiplier from local forest cover at the sapling cell (0–1).</summary>
        public static float SeralSpreadMultiplier(string wood, float localForestCover)
        {
            if (!EcosystemConfig.Loaded.EnableTreeSeralSuccession) return 1f;
            if (!TryGet(wood, out Profile profile)) return 1f;
            return SeralSpreadMultiplier(profile.SeralRole, localForestCover);
        }

        internal static float SeralSpreadMultiplier(TreeSeralRole role, float localForestCover)
        {
            float cover = localForestCover;
            if (cover < 0f) cover = 0f;
            if (cover > 1f) cover = 1f;

            switch (role)
            {
                case TreeSeralRole.Pioneer:
                    return Bell(cover, peak: 0.10f, halfWidth: 0.20f, floor: 0.12f);
                case TreeSeralRole.Mid:
                    return Bell(cover, peak: 0.32f, halfWidth: 0.26f, floor: 0.18f);
                default:
                    if (cover < 0.06f) return 0.14f;
                    return Bell(cover, peak: 0.58f, halfWidth: 0.38f, floor: 0.22f);
            }
        }

        static float Bell(float x, float peak, float halfWidth, float floor)
        {
            if (halfWidth <= 0.001f) return 1f;
            float dist = Math.Abs(x - peak) / halfWidth;
            if (dist >= 1f) return floor;
            return floor + (1f - floor) * (1f - dist);
        }

        public static bool TryGetModifierProfile(string wood, out WildSpeciesModifiers.Profile profile)
        {
            profile = default;
            if (!TryGet(wood, out Profile tree)) return false;
            profile = ModifierFor(tree.SeralRole);
            return true;
        }

        static WildSpeciesModifiers.Profile ModifierFor(TreeSeralRole role)
        {
            switch (role)
            {
                case TreeSeralRole.Pioneer:
                    return new WildSpeciesModifiers.Profile(
                        FloraContextAffinity.Open, 1.18f, 0.28f, 0.78f);
                case TreeSeralRole.Mid:
                    return new WildSpeciesModifiers.Profile(
                        FloraContextAffinity.Edge, 1.28f, 0.42f, 0.96f);
                default:
                    return new WildSpeciesModifiers.Profile(
                        FloraContextAffinity.Forest, 1.42f, 0.55f, 1.18f);
            }
        }

        public static void LogMissingWoods(Vintagestory.API.Common.ICoreAPI api)
        {
            if (api == null) return;

            for (int i = 0; i < AllWoods.Count; i++)
            {
                string wood = AllWoods[i];
                if (!ByWood.ContainsKey(wood))
                {
                    if (EcosystemConfig.Loaded.VerboseLogging)
                        api.Logger.Warning("[ecosystemflora] Tree wood missing ecology data: {0}", wood);
                }
            }
        }
    }
}
