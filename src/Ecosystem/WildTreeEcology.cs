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

            public Profile(
                float minTemp, float maxTemp,
                float minRain, float maxRain,
                float minForest, float maxForest,
                float spreadRate,
                int spreadRadius,
                int sameSpeciesSpacing,
                int otherSpeciesSpacing)
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
            ["birch"] = new Profile(-7, 39, 0.35f, 1f, 0.2f, 1f, 0.55f, 8, 7, 4),
            ["oak"] = new Profile(-5, 40, 0.35f, 1f, 0.25f, 1f, 0.5f, 8, 7, 4),
            ["maple"] = new Profile(-7, 40, 0.35f, 1f, 0.25f, 1f, 0.5f, 8, 7, 4),
            ["pine"] = new Profile(-18, 30, 0.3f, 1f, 0.15f, 0.95f, 0.45f, 9, 8, 4),
            ["larch"] = new Profile(-24, 15, 0.35f, 1f, 0.1f, 0.9f, 0.4f, 9, 8, 4),
            ["acacia"] = new Profile(21, 60, 0.15f, 0.6f, 0f, 0.5f, 0.35f, 10, 9, 5),
            ["kapok"] = new Profile(20, 50, 0.35f, 1f, 0.2f, 0.85f, 0.4f, 9, 8, 4),
            ["crimsonkingmaple"] = new Profile(1, 26, 0.4f, 0.9f, 0.2f, 0.8f, 0.45f, 8, 7, 4),
            ["redwood"] = new Profile(7, 35, 0.4f, 1f, 0.35f, 1f, 0.35f, 10, 9, 5),
            ["baldcypress"] = new Profile(8, 41, 0.45f, 1f, 0.3f, 1f, 0.45f, 9, 8, 4),
            ["greenspirecypress"] = new Profile(1, 39, 0.4f, 1f, 0.2f, 0.9f, 0.45f, 8, 7, 4),
            ["ebony"] = new Profile(21, 50, 0.25f, 0.75f, 0.15f, 0.7f, 0.3f, 10, 9, 5),
            ["purpleheart"] = new Profile(20, 50, 0.3f, 0.85f, 0.2f, 0.85f, 0.32f, 10, 9, 5),
            ["walnut"] = new Profile(-1, 39, 0.35f, 0.9f, 0.2f, 0.85f, 0.42f, 8, 7, 4),
        };

        public static bool TryGet(string wood, out Profile profile)
        {
            profile = default;
            if (string.IsNullOrEmpty(wood)) return false;
            return ByWood.TryGetValue(wood, out profile);
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
