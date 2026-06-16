using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Desert colonizers on arid soils (barrel + silver torch cactus).</summary>
    internal static class WildDesertEcology
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
                int minSunlight = 9)
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
            [EcologyDesertSpecies.Barrelcactus] = new EcologyEntry(
                18, 45,
                0.05f, 0.35f,
                0f, 0.25f,
                0.45f,
                2, 2,
                new WildPlantSoil.Profile(SoilKind.Sand | SoilKind.LowFert | SoilKind.Gravel, 60, 180),
                10),
            [EcologyDesertSpecies.Silvertorchcactus] = new EcologyEntry(
                20, 48,
                0.08f, 0.4f,
                0f, 0.2f,
                0.4f,
                3, 2,
                new WildPlantSoil.Profile(SoilKind.Sand | SoilKind.LowFert | SoilKind.Gravel, 60, 170),
                10),
        };

        public static bool IsSpecies(string species) => EcologyDesertSpecies.IsKnown(species);

        public static bool TryGet(string species, out EcologyEntry entry)
        {
            entry = default;
            if (string.IsNullOrEmpty(species)) return false;
            return BySpecies.TryGetValue(species, out entry);
        }

        public static void LogMissingSpecies(Vintagestory.API.Common.ICoreAPI api)
        {
            if (api == null) return;

            for (int i = 0; i < EcologyDesertSpecies.All.Count; i++)
            {
                string species = EcologyDesertSpecies.All[i];
                if (!BySpecies.ContainsKey(species))
                {
                    if (EcosystemConfig.Loaded.VerboseLogging)
                        api.Logger.Warning("[ecosystemflora] Desert species missing ecology data: {0}", species);
                }
            }
        }
    }
}
