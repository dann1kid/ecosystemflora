using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Worldgen-aligned temps; VS does not resolve minTempByType on Block.Attributes at runtime.</summary>
    internal static class WildFlowerClimate
    {
        readonly struct Entry
        {
            public readonly float MinTemp;
            public readonly float MaxTemp;
            public readonly int GrowHours;

            public Entry(float minTemp, float maxTemp, int growHours)
            {
                MinTemp = minTemp;
                MaxTemp = maxTemp;
                GrowHours = growHours;
            }
        }

        static readonly Dictionary<string, Entry> BySpecies = new Dictionary<string, Entry>
        {
            ["catmint"] = new Entry(5, 19, 192),
            ["forgetmenot"] = new Entry(7, 20, 192),
            ["edelweiss"] = new Entry(-1, 12, 120),
            ["heather"] = new Entry(2, 15, 384),
            ["horsetail"] = new Entry(1, 15, 120),
            ["orangemallow"] = new Entry(20, 37, 192),
            ["wilddaisy"] = new Entry(7, 20, 192),
            ["westerngorse"] = new Entry(2, 15, 576),
            ["cowparsley"] = new Entry(2, 20, 192),
            ["goldenpoppy"] = new Entry(22, 25, 168),
            ["lilyofthevalley"] = new Entry(2, 13, 192),
            ["woad"] = new Entry(-2, 21, 192),
            ["cornflower"] = new Entry(3, 23, 192),
            ["daffodil"] = new Entry(11, 28, 192),
            ["mugwort"] = new Entry(11, 28, 192),
            ["bluebell"] = new Entry(2, 15, 192),
        };

        public static bool TryGet(string species, out float minTemp, out float maxTemp, out int growHours)
        {
            minTemp = 10f;
            maxTemp = 22f;
            growHours = 192;

            if (string.IsNullOrEmpty(species)) return false;
            if (!BySpecies.TryGetValue(species, out Entry entry)) return false;

            minTemp = entry.MinTemp;
            maxTemp = entry.MaxTemp;
            growHours = entry.GrowHours;
            return true;
        }
    }
}
