using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Worldgen-aligned ecology for game:fern-* and tallfern (survival/worldgen/blockpatches/fern.json).</summary>
    internal static class WildFernEcology
    {
        public readonly struct EcologyEntry
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

            public EcologyEntry(
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

        static readonly Dictionary<string, EcologyEntry> BySpecies = new Dictionary<string, EcologyEntry>
        {
            ["tallfern"] = new EcologyEntry(22, 50, 0.7f, 1f, 0.5f, 1f, 1.8f, 0, 1,
                new WildPlantSoil.Profile(SoilKindSets.ForestUnderstory | SoilKind.MediumFert, 80, 0)),
            ["eaglefern"] = new EcologyEntry(-12, 10, 0.4f, 1f, 0.5f, 1f, 1.4f, 0, 1,
                new WildPlantSoil.Profile(SoilKindSets.ForestUnderstory | SoilKind.LowFert, 80, 220)),
            ["cinnamonfern"] = new EcologyEntry(-3, 15, 0.75f, 1f, 0.5f, 1f, 1.3f, 0, 1,
                new WildPlantSoil.Profile(SoilKindSets.ForestUnderstory | SoilKind.Peat | SoilKind.LowFert, 80, 0)),
            ["deerfern"] = new EcologyEntry(2, 20, 0.75f, 1f, 0.5f, 1f, 1.3f, 0, 1,
                new WildPlantSoil.Profile(SoilKindSets.ForestUnderstory | SoilKind.Peat | SoilKind.LowFert, 80, 0)),
            ["hartstongue"] = new EcologyEntry(15, 40, 0.75f, 1f, 0f, 0.6f, 1.1f, 1, 1,
                new WildPlantSoil.Profile(SoilKindSets.Meadow | SoilKind.ForestFloor | SoilKind.MediumFert, 80, 0)),
        };

        public static bool TryGet(string species, out EcologyEntry entry)
        {
            entry = default;
            if (string.IsNullOrEmpty(species)) return false;
            return BySpecies.TryGetValue(species, out entry);
        }

        public static void LogMissingSpecies(Vintagestory.API.Common.ICoreAPI api)
        {
            if (api == null) return;

            for (int i = 0; i < EcologyFernSpecies.All.Count; i++)
            {
                string species = EcologyFernSpecies.All[i];
                if (!BySpecies.ContainsKey(species))
                {
                    if (EcosystemConfig.Loaded.VerboseLogging)
                        api.Logger.Warning("[ecosystemflora] Fern species missing ecology data: {0}", species);
                }
            }
        }
    }
}
