using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Wet-shore sedges on <c>tallplant-brownsedge-land-*</c> — slow clumping wetland herbs
    /// (Carex-like: edge rhizome steps only, extreme moisture, no long seed jumps).
    /// </summary>
    internal static class WildShoreSedgeEcology
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
            public readonly float SeedDispersalChance;
            public readonly int SeedDispersalRadius;
            public readonly int MatSpreadRadius;

            public EcologyEntry(
                float minTemp, float maxTemp,
                float minRain, float maxRain,
                float minForest, float maxForest,
                float spreadRate,
                int sameSpacing, int otherSpacing,
                WildPlantSoil.Profile soil,
                int minSunlight = 7,
                float seedDispersalChance = 0f,
                int seedDispersalRadius = 0,
                int matSpreadRadius = 1)
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
                SeedDispersalChance = seedDispersalChance;
                SeedDispersalRadius = seedDispersalRadius;
                MatSpreadRadius = matSpreadRadius;
            }
        }

        static readonly Dictionary<string, EcologyEntry> BySpecies = new Dictionary<string, EcologyEntry>
        {
            [EcologyShoreSedgeSpecies.Brownsedge] = new EcologyEntry(
                8, 26,
                0.78f, 1f,
                0f, 0.25f,
                0.35f,
                1, 1,
                new WildPlantSoil.Profile(SoilKind.Peat | SoilKind.MediumFert, 85, 0),
                minSunlight: 7,
                seedDispersalChance: 0f,
                seedDispersalRadius: 0,
                matSpreadRadius: 1),
        };

        public static bool IsSpecies(string species) => EcologyShoreSedgeSpecies.IsKnown(species);

        public static bool TryGet(string species, out EcologyEntry entry)
        {
            entry = default;
            if (string.IsNullOrEmpty(species)) return false;
            return BySpecies.TryGetValue(species, out entry);
        }

        public static void LogMissingSpecies(Vintagestory.API.Common.ICoreAPI api)
        {
            if (api == null) return;

            for (int i = 0; i < EcologyShoreSedgeSpecies.All.Count; i++)
            {
                string species = EcologyShoreSedgeSpecies.All[i];
                if (!BySpecies.ContainsKey(species))
                {
                    if (EcosystemConfig.Loaded.VerboseLogging)
                        api.Logger.Warning("[ecosystemflora] Shore sedge species missing ecology data: {0}", species);
                }
            }
        }
    }
}