using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Wild fruiting bushes (survival/worldgen/blockpatches/berrybush.json).</summary>
    internal static class WildBerryEcology
    {
        public readonly struct Profile
        {
            public readonly float MinTemp;
            public readonly float MaxTemp;
            public readonly float MinRain;
            public readonly float MaxRain;
            public readonly float MinForest;
            public readonly float MaxForest;
            public readonly float SpreadRate;
            public readonly int SameSpeciesSpacing;
            public readonly int OtherSpeciesSpacing;
            public readonly WildPlantSoil.Profile Soil;
            public readonly int MinSunlight;

            public Profile(
                float minTemp, float maxTemp,
                float minRain, float maxRain,
                float minForest, float maxForest,
                float spreadRate,
                int sameSpacing, int otherSpacing,
                WildPlantSoil.Profile soil,
                int minSunlight = 7)
            {
                MinTemp = minTemp;
                MaxTemp = maxTemp;
                MinRain = minRain;
                MaxRain = maxRain;
                MinForest = minForest;
                MaxForest = maxForest;
                SpreadRate = spreadRate;
                SameSpeciesSpacing = sameSpacing;
                OtherSpeciesSpacing = otherSpacing;
                Soil = soil;
                MinSunlight = minSunlight;
            }
        }

        public static readonly IReadOnlyList<string> AllTypes = new[]
        {
            "blackcurrant", "redcurrant", "whitecurrant", "blueberry", "cranberry",
            "strawberry", "beautyberry", "cloudberry", "blackberry", "raspberry",
        };

        static readonly Dictionary<string, Profile> ByType = new Dictionary<string, Profile>
        {
            ["blackcurrant"] = Entry(-2, 23, 0.3f, 0.7f, 0f, 0.5f, 0.55f, 3, 2,
                new WildPlantSoil.Profile(SoilKind.LowFert | SoilKind.MediumFert, 100, 200)),
            ["redcurrant"] = Entry(-3, 22, 0.3f, 0.7f, 0f, 0.4f, 0.55f, 3, 2,
                new WildPlantSoil.Profile(SoilKind.LowFert | SoilKind.MediumFert, 100, 200)),
            ["whitecurrant"] = Entry(0, 24, 0.3f, 0.7f, 0f, 0.4f, 0.45f, 3, 2,
                new WildPlantSoil.Profile(SoilKind.LowFert | SoilKind.MediumFert, 100, 200)),
            ["blueberry"] = Entry(-2, 18, 0.3f, 0.7f, 0.5f, 1f, 0.65f, 2, 2,
                new WildPlantSoil.Profile(SoilKindSets.ForestUnderstory | SoilKind.LowFert, 80, 180)),
            ["cranberry"] = Entry(-2, 18, 0.45f, 1f, 0f, 0.7f, 0.6f, 2, 2,
                new WildPlantSoil.Profile(SoilKindSets.Meadow | SoilKind.Peat, 80, 0)),
            ["strawberry"] = Entry(-2, 18, 0.45f, 1f, 0.6f, 1f, 0.7f, 2, 2,
                new WildPlantSoil.Profile(SoilKindSets.ForestUnderstory | SoilKind.MediumFert, 80, 0)),
            ["beautyberry"] = Entry(10, 22, 0.45f, 1f, 0f, 0.4f, 0.5f, 3, 2,
                new WildPlantSoil.Profile(SoilKindSets.Meadow, 80, 0)),
            ["cloudberry"] = Entry(-20, -3, 0.5f, 1f, 0f, 0.7f, 0.35f, 3, 2,
                new WildPlantSoil.Profile(SoilKind.Peat | SoilKind.LowFert | SoilKind.ForestFloor, 50, 180)),
            ["blackberry"] = Entry(-2, 23, 0.35f, 1f, 0.5f, 1f, 0.65f, 2, 2,
                new WildPlantSoil.Profile(SoilKindSets.ForestUnderstory | SoilKind.MediumFert, 80, 260)),
            ["raspberry"] = Entry(-15, 10, 0.3f, 1f, 0f, 0.4f, 0.6f, 3, 2,
                new WildPlantSoil.Profile(SoilKind.MediumFert | SoilKind.LowFert | SoilKind.HighFert, 180, 0)),
        };

        static Profile Entry(
            float minTemp, float maxTemp,
            float minRain, float maxRain,
            float minForest, float maxForest,
            float spreadRate,
            int sameSpacing, int otherSpacing,
            WildPlantSoil.Profile soil,
            int minSunlight = 7)
        {
            return new Profile(minTemp, maxTemp, minRain, maxRain, minForest, maxForest,
                spreadRate, sameSpacing, otherSpacing, soil, minSunlight);
        }

        public static bool TryGet(string berryType, out Profile profile)
        {
            profile = default;
            if (string.IsNullOrEmpty(berryType)) return false;
            return ByType.TryGetValue(berryType, out profile);
        }

        public static void LogMissingTypes(Vintagestory.API.Common.ICoreAPI api)
        {
            if (api == null) return;

            for (int i = 0; i < AllTypes.Count; i++)
            {
                string type = AllTypes[i];
                if (!ByType.ContainsKey(type))
                {
                    if (EcosystemConfig.Loaded.VerboseLogging)
                        api.Logger.Warning("[ecosystemflora] Berry type missing ecology data: {0}", type);
                }
            }
        }
    }
}
