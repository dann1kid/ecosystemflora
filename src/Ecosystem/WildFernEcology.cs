using System.Collections.Generic;

// Export-only C# tables: fallback when SpeciesEcologyRegistry is not loaded.
#pragma warning disable CS0618

namespace WildFarming.Ecosystem
{
    /// <summary>Worldgen-aligned ecology for game:fern-* and tallfern (survival/worldgen/blockpatches/fern.json).</summary>
    [EcologyExportTable]
    [System.Obsolete("Export-only default table. Contract species runtime uses SpeciesEcologyRegistry.")]
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
            // Boreal/cool, tolerates drier shade — short warm window
            ["eaglefern"] = new EcologyEntry(-14, 8, 0.35f, 1f, 0.5f, 1f, 1.0f, 1, 2,
                new WildPlantSoil.Profile(SoilKindSets.ForestUnderstory | SoilKind.LowFert, 80, 200)),

            // Cold wet peat/forest — needs rain, early-season colonizer
            ["cinnamonfern"] = new EcologyEntry(-10, 14, 0.82f, 1f, 0.5f, 1f, 0.92f, 1, 2,
                new WildPlantSoil.Profile(SoilKindSets.ForestUnderstory | SoilKind.Peat | SoilKind.LowFert, 80, 180)),

            // Cool–warm wet forest — broader temp, summer spread
            ["deerfern"] = new EcologyEntry(5, 24, 0.62f, 1f, 0.5f, 1f, 1.18f, 1, 1,
                new WildPlantSoil.Profile(SoilKindSets.ForestUnderstory | SoilKind.MediumFert | SoilKind.LowFert, 80, 0)),

            // Warm shady meadow/rock — open-wet edge, no tree symbiosis
            ["hartstongue"] = new EcologyEntry(12, 38, 0.72f, 1f, 0f, 0.5f, 0.82f, 2, 1,
                new WildPlantSoil.Profile(SoilKindSets.Meadow | SoilKind.ForestFloor | SoilKind.MediumFert, 80, 0)),

            // Warm forest edge — partial sun, not deep interior
            ["tallfern"] = new EcologyEntry(18, 48, 0.68f, 1f, 0.45f, 0.88f, 1.28f, 1, 1,
                new WildPlantSoil.Profile(SoilKindSets.ForestUnderstory | SoilKind.MediumFert, 80, 0)),
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
