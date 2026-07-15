using System;
using System.Collections.Generic;

// Export-only C# tables: fallback when SpeciesEcologyRegistry is not loaded.
#pragma warning disable CS0618

namespace WildFarming.Ecosystem
{
    /// <summary>Wild spread of vanilla saplings from mature log-grown trunks (no mod trunk blocks).</summary>
    [EcologyExportTable]
    [System.Obsolete("Export-only default table. Contract species runtime uses SpeciesEcologyRegistry.")]
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
            /// <summary>Species seral peak on local forest cover (0 = use role default).</summary>
            public readonly float SeralPeakForest;
            public readonly float SeralHalfWidth;
            public readonly float SeralFloor;

            public Profile(
                float minTemp, float maxTemp,
                float minRain, float maxRain,
                float minForest, float maxForest,
                float spreadRate,
                int spreadRadius,
                int sameSpeciesSpacing,
                int otherSpeciesSpacing,
                TreeSeralRole seralRole,
                int saplingMinSunlight = 0,
                float seralPeakForest = 0f,
                float seralHalfWidth = 0f,
                float seralFloor = 0f)
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
                SeralPeakForest = seralPeakForest;
                SeralHalfWidth = seralHalfWidth;
                SeralFloor = seralFloor;
            }
        }

        public static readonly IReadOnlyList<string> AllWoods = EcologyTreeSpecies.AllWoods;

        static readonly Dictionary<string, Profile> ByWood = new Dictionary<string, Profile>
        {
            // Pioneers — disturbed open ground; spacing ~ crown + open bias (see TreeSpacingDefaults).
            ["birch"] = P(-12, 28, 0.35f, 1f, 0f, 0.38f, 0.70f, 9, 5, 4,
                TreeSeralRole.Pioneer, 11, seralPeak: 0.08f, seralHalf: 0.18f, seralFloor: 0.14f),
            ["acacia"] = P(18, 44, 0.12f, 0.55f, 0f, 0.35f, 0.46f, 11, 7, 5,
                TreeSeralRole.Pioneer, 11, seralPeak: 0.06f, seralHalf: 0.16f, seralFloor: 0.12f),

            // Mid-seral — edges, riparian, semi-open woodland; spacing ~ ReferenceCrownRadius + 1.
            ["oak"] = P(-2, 30, 0.35f, 0.78f, 0f, 0.75f, 0.50f, 9, 6, 4,
                TreeSeralRole.Mid, 11, seralPeak: 0.35f, seralHalf: 0.28f, seralFloor: 0.16f),
            ["maple"] = P(-7, 28, 0.35f, 0.95f, 0.08f, 0.82f, 0.52f, 9, 6, 4,
                TreeSeralRole.Mid, 10, seralPeak: 0.30f, seralHalf: 0.26f, seralFloor: 0.18f),
            ["crimsonkingmaple"] = P(0, 24, 0.38f, 0.72f, 0.10f, 0.68f, 0.44f, 8, 6, 4,
                TreeSeralRole.Mid, 10, seralPeak: 0.26f, seralHalf: 0.22f, seralFloor: 0.16f),
            ["walnut"] = P(-1, 32, 0.35f, 0.88f, 0.12f, 0.85f, 0.42f, 9, 6, 4,
                TreeSeralRole.Mid, 10, seralPeak: 0.28f, seralHalf: 0.24f, seralFloor: 0.17f),
            ["greenspirecypress"] = P(0, 34, 0.38f, 1f, 0.10f, 0.80f, 0.40f, 8, 5, 4,
                TreeSeralRole.Mid, 11, seralPeak: 0.22f, seralHalf: 0.22f, seralFloor: 0.15f),
            ["baldcypress"] = P(8, 36, 0.45f, 1f, 0.14f, 0.92f, 0.45f, 10, 4, 4,
                TreeSeralRole.Mid, 10, seralPeak: 0.22f, seralHalf: 0.22f, seralFloor: 0.14f),
            ["purpleheart"] = P(22, 48, 0.28f, 0.88f, 0.22f, 0.88f, 0.36f, 10, 7, 5,
                TreeSeralRole.Mid, 9, seralPeak: 0.34f, seralHalf: 0.26f, seralFloor: 0.18f),

            // Climax — mature forest; dense conifers keep tighter trunks (pine 3 ≈ crown 3).
            ["pine"] = P(-20, 22, 0.38f, 1f, 0.12f, 1f, 0.48f, 10, 3, 3,
                TreeSeralRole.Climax, 9, seralPeak: 0.48f, seralHalf: 0.34f, seralFloor: 0.20f),
            ["larch"] = P(-26, 12, 0.32f, 1f, 0.14f, 0.92f, 0.38f, 10, 4, 4,
                TreeSeralRole.Climax, 10, seralPeak: 0.38f, seralHalf: 0.30f, seralFloor: 0.18f),
            ["kapok"] = P(22, 48, 0.32f, 1f, 0.30f, 1f, 0.36f, 11, 7, 5,
                TreeSeralRole.Climax, 10, seralPeak: 0.55f, seralHalf: 0.32f, seralFloor: 0.22f),
            ["redwood"] = P(5, 28, 0.42f, 1f, 0.35f, 1f, 0.32f, 12, 8, 5,
                TreeSeralRole.Climax, 10, seralPeak: 0.62f, seralHalf: 0.30f, seralFloor: 0.24f),
            ["ebony"] = P(22, 48, 0.22f, 0.72f, 0.32f, 0.95f, 0.28f, 10, 6, 5,
                TreeSeralRole.Climax, 9, seralPeak: 0.50f, seralHalf: 0.30f, seralFloor: 0.20f),
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
            int saplingMinSunlight = 0,
            float seralPeak = 0f,
            float seralHalf = 0f,
            float seralFloor = 0f)
        {
            return new Profile(
                minTemp, maxTemp, minRain, maxRain,
                minForest, maxForest, spreadRate,
                spreadRadius, sameSpeciesSpacing, otherSpeciesSpacing,
                seralRole, saplingMinSunlight, seralPeak, seralHalf, seralFloor);
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
                default: return 9;
            }
        }

        /// <summary>Soft multiplier from local forest cover at the sapling cell (0–1).</summary>
        public static float SeralSpreadMultiplier(string wood, float localForestCover)
        {
            if (!EcosystemConfig.Loaded.EnableTreeSeralSuccession) return 1f;
            if (!TryGet(wood, out Profile profile)) return 1f;
            return SeralSpreadMultiplier(profile, localForestCover);
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

        static float SeralSpreadMultiplier(Profile profile, float localForestCover)
        {
            if (profile.SeralPeakForest > 0f)
            {
                float half = profile.SeralHalfWidth > 0f ? profile.SeralHalfWidth : 0.24f;
                float floor = profile.SeralFloor > 0f ? profile.SeralFloor : 0.15f;
                if (profile.SeralRole == TreeSeralRole.Climax && localForestCover < 0.06f)
                {
                    return floor;
                }

                return Bell(localForestCover, profile.SeralPeakForest, half, floor);
            }

            return SeralSpreadMultiplier(profile.SeralRole, localForestCover);
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
            profile = ModifierFor(tree.SeralRole, wood);
            return true;
        }

        static WildSpeciesModifiers.Profile ModifierFor(TreeSeralRole role, string wood)
        {
            if (wood == "baldcypress")
            {
                return new WildSpeciesModifiers.Profile(
                    FloraContextAffinity.Edge, 1.22f, 0.38f, 0.92f);
            }

            if (wood == "pine" || wood == "larch")
            {
                return new WildSpeciesModifiers.Profile(
                    FloraContextAffinity.Forest, 1.38f, 0.58f, 1.22f);
            }

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
