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
            public readonly SpreadMode SpreadMode;
            public readonly MatConnectivity MatConnectivity;
            public readonly float SeedDispersalChance;
            public readonly int SeedDispersalRadius;
            public readonly int MatSpreadRadius;
            public readonly int IndependentSpreadRadius;

            public Profile(
                float minTemp, float maxTemp,
                float minRain, float maxRain,
                float minForest, float maxForest,
                float spreadRate,
                int sameSpacing, int otherSpacing,
                WildPlantSoil.Profile soil,
                SpreadMode spreadMode,
                MatConnectivity matConnectivity = MatConnectivity.Orthogonal4,
                float seedDispersalChance = 0f,
                int seedDispersalRadius = 0,
                int matSpreadRadius = 1,
                int independentSpreadRadius = 0,
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
                SpreadMode = spreadMode;
                MatConnectivity = matConnectivity;
                SeedDispersalChance = seedDispersalChance;
                SeedDispersalRadius = seedDispersalRadius;
                MatSpreadRadius = matSpreadRadius;
                IndependentSpreadRadius = independentSpreadRadius;
            }
        }

        public static readonly IReadOnlyList<string> AllTypes = new[]
        {
            "blackcurrant", "redcurrant", "whitecurrant", "blueberry", "cranberry",
            "strawberry", "beautyberry", "cloudberry", "blackberry", "raspberry",
        };

        static readonly Dictionary<string, Profile> ByType = new Dictionary<string, Profile>
        {
            // Currant clumps at woodland edge: rhizome curtain + bird seed.
            ["blackcurrant"] = ColonyClump(-2, 23, 0.3f, 0.7f, 0.15f, 0.65f, 0.58f, 2,
                SoilKind.LowFert | SoilKind.MediumFert, 100, 200, seedChance: 0.14f, seedRadius: 6),
            ["redcurrant"] = ColonyClump(-3, 22, 0.3f, 0.7f, 0.15f, 0.6f, 0.56f, 2,
                SoilKind.LowFert | SoilKind.MediumFert, 100, 200, seedChance: 0.14f, seedRadius: 6),
            ["whitecurrant"] = ColonyClump(0, 24, 0.3f, 0.7f, 0.1f, 0.55f, 0.52f, 2,
                SoilKind.LowFert | SoilKind.MediumFert, 100, 200, seedChance: 0.12f, seedRadius: 5),

            // Blueberry: dense rhizome colonies under sparse-canopy forest/heath.
            ["blueberry"] = RhizomeColony(-2, 18, 0.35f, 0.85f, 0.35f, 1f, 1.05f,
                SoilKindSets.ForestUnderstory | SoilKind.LowFert, 80, 180, seedChance: 0.04f, seedRadius: 3),

            // Raspberry: root-sucker thickets along forest edge.
            ["raspberry"] = SuckerThicket(-15, 10, 0.3f, 1f, 0.2f, 0.6f, 1.12f,
                SoilKind.MediumFert | SoilKind.LowFert | SoilKind.HighFert, 180, 0, seedChance: 0.06f, seedRadius: 4),

            // Blackberry: tip-rooting + suckers — eight-connected mat edge.
            ["blackberry"] = RunnerThicket(-2, 23, 0.35f, 1f, 0.35f, 1f, 1.22f,
                SoilKindSets.ForestUnderstory | SoilKind.MediumFert, 80, 260, seedChance: 0.1f, seedRadius: 5),

            // Strawberry: stolon runners + seed into forest clearings.
            ["strawberry"] = RunnerThicket(-2, 18, 0.45f, 1f, 0.25f, 0.85f, 1.08f,
                SoilKindSets.ForestUnderstory | SoilKind.MediumFert, 80, 0, seedChance: 0.18f, seedRadius: 7),

            // Cranberry: low peat mat.
            ["cranberry"] = RhizomeColony(-2, 18, 0.45f, 1f, 0f, 0.55f, 0.92f,
                SoilKindSets.Meadow | SoilKind.Peat, 80, 0, seedChance: 0.03f, seedRadius: 2),

            // Cloudberry: bog/tundra rhizome + seed colonies.
            ["cloudberry"] = RhizomeColony(-20, -3, 0.5f, 1f, 0f, 0.55f, 0.78f,
                SoilKind.Peat | SoilKind.LowFert | SoilKind.ForestFloor, 50, 180, seedChance: 0.12f, seedRadius: 8),

            // Beautyberry: upright seed shrub at forest edge (radius search, no mat).
            ["beautyberry"] = SeedShrub(10, 22, 0.45f, 1f, 0f, 0.45f, 0.42f, 3,
                SoilKindSets.Meadow | SoilKind.ForestFloor, 80, 0, spreadRadius: 5),
        };

        static Profile ColonyClump(
            float minTemp, float maxTemp, float minRain, float maxRain,
            float minForest, float maxForest, float spreadRate, int sameSpacing,
            SoilKind soils, int minFert, int maxFert,
            float seedChance, int seedRadius)
        {
            return new Profile(
                minTemp, maxTemp, minRain, maxRain, minForest, maxForest,
                spreadRate, sameSpacing, 2,
                new WildPlantSoil.Profile(soils, minFert, maxFert),
                SpreadMode.BerryColonyMat,
                MatConnectivity.Orthogonal4,
                seedChance, seedRadius);
        }

        static Profile RhizomeColony(
            float minTemp, float maxTemp, float minRain, float maxRain,
            float minForest, float maxForest, float spreadRate,
            SoilKind soils, int minFert, int maxFert,
            float seedChance, int seedRadius)
        {
            return new Profile(
                minTemp, maxTemp, minRain, maxRain, minForest, maxForest,
                spreadRate, 0, 2,
                new WildPlantSoil.Profile(soils, minFert, maxFert),
                SpreadMode.BerryColonyMat,
                MatConnectivity.Orthogonal4,
                seedChance, seedRadius);
        }

        static Profile SuckerThicket(
            float minTemp, float maxTemp, float minRain, float maxRain,
            float minForest, float maxForest, float spreadRate,
            SoilKind soils, int minFert, int maxFert,
            float seedChance, int seedRadius)
        {
            return new Profile(
                minTemp, maxTemp, minRain, maxRain, minForest, maxForest,
                spreadRate, 0, 2,
                new WildPlantSoil.Profile(soils, minFert, maxFert),
                SpreadMode.BerryColonyMat,
                MatConnectivity.Orthogonal4,
                seedChance, seedRadius);
        }

        static Profile RunnerThicket(
            float minTemp, float maxTemp, float minRain, float maxRain,
            float minForest, float maxForest, float spreadRate,
            SoilKind soils, int minFert, int maxFert,
            float seedChance, int seedRadius)
        {
            return new Profile(
                minTemp, maxTemp, minRain, maxRain, minForest, maxForest,
                spreadRate, 0, 2,
                new WildPlantSoil.Profile(soils, minFert, maxFert),
                SpreadMode.BerryColonyMat,
                MatConnectivity.Chebyshev8,
                seedChance, seedRadius);
        }

        static Profile SeedShrub(
            float minTemp, float maxTemp, float minRain, float maxRain,
            float minForest, float maxForest, float spreadRate, int sameSpacing,
            SoilKind soils, int minFert, int maxFert, int spreadRadius)
        {
            return new Profile(
                minTemp, maxTemp, minRain, maxRain, minForest, maxForest,
                spreadRate, sameSpacing, 2,
                new WildPlantSoil.Profile(soils, minFert, maxFert),
                SpreadMode.Independent,
                independentSpreadRadius: spreadRadius);
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
