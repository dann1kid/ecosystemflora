using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Worldgen-aligned temps for game:flower-* (from survival worldgen/blockpatches/flower.json).</summary>
    internal static class WildFlowerClimate
    {
        readonly struct Entry
        {
            public readonly float MinTemp;
            public readonly float MaxTemp;

            public Entry(float minTemp, float maxTemp)
            {
                MinTemp = minTemp;
                MaxTemp = maxTemp;
            }
        }

        static readonly Dictionary<string, Entry> BySpecies = new Dictionary<string, Entry>
        {
            ["catmint"] = new Entry(5, 19),
            ["cornflower"] = new Entry(3, 23),
            ["forgetmenot"] = new Entry(7, 20),
            ["edelweiss"] = new Entry(-1, 12),
            ["heather"] = new Entry(2, 15),
            ["horsetail"] = new Entry(1, 15),
            ["orangemallow"] = new Entry(20, 37),
            ["wilddaisy"] = new Entry(7, 20),
            ["westerngorse"] = new Entry(2, 15),
            ["cowparsley"] = new Entry(2, 20),
            ["goldenpoppy"] = new Entry(22, 25),
            ["lilyofthevalley"] = new Entry(2, 13),
            ["woad"] = new Entry(-2, 21),
            ["redtopgrass"] = new Entry(0, 20),
            ["bluebell"] = new Entry(-5, 15),
            ["ghostpipewhite"] = new Entry(-12, 12),
            ["ghostpipepink"] = new Entry(-12, 12),
            ["ghostpipered"] = new Entry(-12, 12),
            ["daffodil"] = new Entry(11, 28),
            ["mugwort"] = new Entry(11, 28),
        };

        public static bool TryGet(string species, out float minTemp, out float maxTemp)
        {
            minTemp = 10f;
            maxTemp = 22f;

            if (string.IsNullOrEmpty(species)) return false;
            if (!BySpecies.TryGetValue(species, out Entry entry)) return false;

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
                    api.Logger.Warning("[wildfarming] Flower species missing climate data: {0}", species);
                }
            }
        }
    }
}
