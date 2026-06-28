using System.Collections.Generic;

// Export-only C# tables: fallback when SpeciesEcologyRegistry is not loaded.
#pragma warning disable CS0618

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Meadow grass colonizers on vanilla <c>flower-*</c> blocks (e.g. redtopgrass) that invade tallgrass turf.
    /// </summary>
    [EcologyExportTable]
    [System.Obsolete("Export-only default table. Contract species runtime uses SpeciesEcologyRegistry.")]
    internal static class WildGrassColonizerEcology
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
            [EcologyGrassColonizerSpecies.Redtopgrass] = new EcologyEntry(
                0, 20,
                0.42f, 1f,
                0f, 0.2f,
                2.2f,
                0, 0,
                new WildPlantSoil.Profile(SoilKindSets.Meadow | SoilKind.LowFert, 100, 0),
                7),
        };

        public static bool IsSpecies(string species) => EcologyGrassColonizerSpecies.IsKnown(species);

        public static bool TryGet(string species, out EcologyEntry entry)
        {
            entry = default;
            if (string.IsNullOrEmpty(species)) return false;
            return BySpecies.TryGetValue(species, out entry);
        }

        public static void LogMissingSpecies(Vintagestory.API.Common.ICoreAPI api)
        {
            if (api == null) return;

            for (int i = 0; i < EcologyGrassColonizerSpecies.All.Count; i++)
            {
                string species = EcologyGrassColonizerSpecies.All[i];
                if (!BySpecies.ContainsKey(species))
                {
                    if (EcosystemConfig.Loaded.VerboseLogging)
                        api.Logger.Warning("[ecosystemflora] Grass colonizer species missing ecology data: {0}", species);
                }
            }
        }
    }
}
