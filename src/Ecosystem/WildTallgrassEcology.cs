using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Meadow matrix for game:tallgrass-* (survival/worldgen/blockpatches/grass.json).</summary>
    internal static class WildTallgrassEcology
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

        /// <summary>Single species key for all growth stages; spread uses veryshort when maturation enabled.</summary>
        static readonly Dictionary<string, EcologyEntry> BySpecies = new Dictionary<string, EcologyEntry>
        {
            ["tallgrass"] = new EcologyEntry(
                -12, 40,
                0.1f, 1f,
                0f, 1f,
                1.35f,
                0, 0,
                new WildPlantSoil.Profile(SoilKindSets.Meadow | SoilKind.LowFert | SoilKind.ForestFloor, 80, 0),
                7),
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

            for (int i = 0; i < EcologyTallgrassSpecies.All.Count; i++)
            {
                string species = EcologyTallgrassSpecies.All[i];
                if (!BySpecies.ContainsKey(species))
                {
                    if (EcosystemConfig.Loaded.VerboseLogging)
                        api.Logger.Warning("[ecosystemflora] Tallgrass species missing ecology data: {0}", species);
                }
            }
        }
    }
}
