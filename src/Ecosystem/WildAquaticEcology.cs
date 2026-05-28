using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Ecology for shallow aquatic plants (worldgen near-water / underwater patches).</summary>
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
            public readonly int MinWaterDepth;
            /// <summary>Blocks occupied upward from base (papyrus = 2).</summary>
            public readonly int VerticalBlocks;
            /// <summary>If &gt;= 0, require exactly this many water blocks above substrate when standing in water.</summary>
            public readonly int ExactWaterDepth;
            public readonly int SameSpeciesSpacing;
            public readonly int OtherSpeciesSpacing;

            public Profile(
                EcologyHabitat habitat,
                float minTemp, float maxTemp,
                float minRain, float maxRain,
                float spreadRate,
                int maxWaterDepth,
                int sameSpeciesSpacing,
                int otherSpeciesSpacing,
                int minWaterDepth = 0,
                int verticalBlocks = 1,
                int exactWaterDepth = -1)
            {
                Habitat = habitat;
                MinTemp = minTemp;
                MaxTemp = maxTemp;
                MinRain = minRain;
                MaxRain = maxRain;
                SpreadRate = spreadRate;
                MaxWaterDepth = maxWaterDepth;
                MinWaterDepth = minWaterDepth;
                VerticalBlocks = verticalBlocks;
                ExactWaterDepth = exactWaterDepth;
                SameSpeciesSpacing = sameSpeciesSpacing;
                OtherSpeciesSpacing = otherSpeciesSpacing;
            }
        }

        static readonly Dictionary<string, Profile> BySpecies = new Dictionary<string, Profile>
        {
            ["coopersreed"] = new Profile(EcologyHabitat.ReedNearWater, 3, 23, 0.4f, 1f, 1.0f, 1, 0, 1),
            ["tule"] = new Profile(EcologyHabitat.ReedNearWater, 5, 25, 0.4f, 1f, 0.85f, 1, 0, 1),
            // Papyrus: 2 blocks tall, vanilla maxWaterDepth 1 — one water block above muddy gravel
            ["papyrus"] = new Profile(EcologyHabitat.ReedNearWater, 24, 40, 0.33f, 1f, 0.75f, 1, 0, 1, exactWaterDepth: 1, verticalBlocks: 2),
            ["waterlily"] = new Profile(EcologyHabitat.WaterSurface, 10, 40, 0.5f, 1f, 2.2f, 2, 1, 1),
            // Water crowfoot (водяной лютик): section column + top/tip
            ["watercrowfoot"] = new Profile(EcologyHabitat.UnderwaterColumn, -10, 40, 0.5f, 1f, 2.0f, 8, 1, 1, minWaterDepth: 2),
        };

        public static bool TryGet(string species, out Profile profile)
        {
            profile = default;
            if (string.IsNullOrEmpty(species)) return false;
            return BySpecies.TryGetValue(species, out profile);
        }
    }
}
