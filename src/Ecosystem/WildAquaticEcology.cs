using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Ecology for shallow aquatic plants (worldgen near-water / waterlily patches).</summary>
    internal static class WildAquaticEcology
    {
        public readonly struct Profile
        {
            public readonly EcologyHabitat Habitat;
            public readonly float MinTemp;
            public readonly float MaxTemp;
            public readonly float MinRain;
            public readonly float MaxRain;
            public readonly float SpreadRate;
            public readonly int MaxWaterDepth;
            public readonly int SameSpeciesSpacing;
            public readonly int OtherSpeciesSpacing;

            public Profile(
                EcologyHabitat habitat,
                float minTemp, float maxTemp,
                float minRain, float maxRain,
                float spreadRate,
                int maxWaterDepth,
                int sameSpeciesSpacing,
                int otherSpeciesSpacing)
            {
                Habitat = habitat;
                MinTemp = minTemp;
                MaxTemp = maxTemp;
                MinRain = minRain;
                MaxRain = maxRain;
                SpreadRate = spreadRate;
                MaxWaterDepth = maxWaterDepth;
                SameSpeciesSpacing = sameSpeciesSpacing;
                OtherSpeciesSpacing = otherSpeciesSpacing;
            }
        }

        static readonly Dictionary<string, Profile> BySpecies = new Dictionary<string, Profile>
        {
            // Cooper's reed / cattail (рогоз) — NearWater, large patches
            ["coopersreed"] = new Profile(EcologyHabitat.ReedNearWater, 3, 23, 0.4f, 1f, 2.2f, 1, 0, 1),
            // Papyrus reed (камыш) — hot climate NearWater
            ["papyrus"] = new Profile(EcologyHabitat.ReedNearWater, 24, 40, 0.33f, 1f, 1.8f, 1, 0, 1),
            // Water lily (кувшинка)
            ["waterlily"] = new Profile(EcologyHabitat.WaterSurface, 10, 40, 0.5f, 1f, 1.5f, 2, 1, 1),
        };

        public static bool TryGet(string species, out Profile profile)
        {
            profile = default;
            if (string.IsNullOrEmpty(species)) return false;
            return BySpecies.TryGetValue(species, out profile);
        }
    }
}
