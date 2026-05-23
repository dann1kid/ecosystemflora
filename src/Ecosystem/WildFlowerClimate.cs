using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Worldgen-aligned ecology for game:flower-* (survival/worldgen/blockpatches/flower.json).</summary>
    internal static class WildFlowerClimate
    {
        public readonly struct EcologyEntry
        {
            public readonly float MinTemp;
            public readonly float MaxTemp;
            public readonly float MinRain;
            public readonly float MaxRain;
            public readonly float MinForest;
            public readonly float MaxForest;
            /// <summary>Relative spread vigor: 1 = config baseline, &gt;1 colonizer, &lt;1 rare/slow.</summary>
            public readonly float SpreadRate;

            public EcologyEntry(
                float minTemp, float maxTemp,
                float minRain, float maxRain,
                float minForest, float maxForest,
                float spreadRate)
            {
                MinTemp = minTemp;
                MaxTemp = maxTemp;
                MinRain = minRain;
                MaxRain = maxRain;
                MinForest = minForest;
                MaxForest = maxForest;
                SpreadRate = spreadRate;
            }
        }

        /// <summary>Envelope of all worldgen patches per species (widest viable range).</summary>
        static readonly Dictionary<string, EcologyEntry> BySpecies = new Dictionary<string, EcologyEntry>
        {
            // Colonizers — large worldgen patches, high quantity
            ["horsetail"] = new EcologyEntry(1, 15, 0.4f, 1f, 0f, 1f, 2.8f),
            ["heather"] = new EcologyEntry(2, 15, 0.4f, 1f, 0f, 0.35f, 2.6f),
            ["westerngorse"] = new EcologyEntry(2, 15, 0.4f, 1f, 0f, 0.35f, 2.6f),
            ["redtopgrass"] = new EcologyEntry(0, 20, 0.42f, 1f, 0f, 0.2f, 2.2f),
            ["mugwort"] = new EcologyEntry(11, 28, 0.5f, 1f, 0f, 0.3f, 2.0f),
            ["cowparsley"] = new EcologyEntry(2, 20, 0.4f, 1f, 0f, 0.35f, 1.7f),
            ["catmint"] = new EcologyEntry(5, 19, 0.39f, 0.86f, 0.1f, 0.5f, 1.5f),
            ["bluebell"] = new EcologyEntry(-5, 15, 0.45f, 0.88f, 0.8f, 1f, 1.5f),
            ["cornflower"] = new EcologyEntry(3, 23, 0.35f, 0.75f, 0.1f, 0.45f, 1.4f),
            ["wilddaisy"] = new EcologyEntry(7, 20, 0.4f, 0.75f, 0f, 0.31f, 1.3f),
            // Steady meadow / understory
            ["forgetmenot"] = new EcologyEntry(7, 20, 0.4f, 0.9f, 0f, 0.4f, 1.1f),
            ["woad"] = new EcologyEntry(-2, 21, 0.25f, 0.7f, 0f, 0.35f, 1.0f),
            ["lilyofthevalley"] = new EcologyEntry(2, 13, 0.45f, 1f, 0.55f, 1f, 0.95f),
            // Slow / localized — rare or small worldgen patches
            ["daffodil"] = new EcologyEntry(11, 28, 0.5f, 1f, 0f, 0.3f, 0.65f),
            ["ghostpipewhite"] = new EcologyEntry(-12, 12, 0.5f, 1f, 0.3f, 1f, 0.5f),
            ["ghostpipepink"] = new EcologyEntry(-12, 12, 0.5f, 1f, 0.3f, 1f, 0.5f),
            ["ghostpipered"] = new EcologyEntry(-12, 12, 0.5f, 1f, 0.3f, 1f, 0.5f),
            ["orangemallow"] = new EcologyEntry(20, 37, 0.1f, 0.35f, 0f, 1f, 0.55f),
            ["edelweiss"] = new EcologyEntry(-1, 12, 0.3f, 0.8f, 0f, 0.7f, 0.4f),
            ["goldenpoppy"] = new EcologyEntry(22, 25, 0.5f, 1f, 0f, 1f, 0.35f),
        };

        public static bool TryGet(string species, out EcologyEntry entry)
        {
            entry = default;
            if (string.IsNullOrEmpty(species)) return false;
            return BySpecies.TryGetValue(species, out entry);
        }

        public static bool TryGet(string species, out float minTemp, out float maxTemp)
        {
            minTemp = 10f;
            maxTemp = 22f;
            if (!TryGet(species, out EcologyEntry entry)) return false;
            minTemp = entry.MinTemp;
            maxTemp = entry.MaxTemp;
            return true;
        }

        public static void LogMissingSpecies(Vintagestory.API.Common.ICoreAPI api)
        {
            if (api == null) return;

            for (int i = 0; i < EcologyFlowerSpecies.All.Count; i++)
            {
                string species = EcologyFlowerSpecies.All[i];
                if (!BySpecies.ContainsKey(species))
                {
                    api.Logger.Warning("[wildfarming] Flower species missing ecology data: {0}", species);
                }
            }
        }
    }
}
