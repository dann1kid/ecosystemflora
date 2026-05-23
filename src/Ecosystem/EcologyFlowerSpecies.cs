using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>All species from game worldproperty block/flower — used for climate table completeness checks.</summary>
    public static class EcologyFlowerSpecies
    {
        public static readonly IReadOnlyList<string> All = new[]
        {
            "catmint", "cornflower", "forgetmenot", "edelweiss", "heather", "horsetail",
            "orangemallow", "wilddaisy", "westerngorse", "cowparsley", "goldenpoppy",
            "lilyofthevalley", "woad", "redtopgrass", "bluebell",
            "ghostpipewhite", "ghostpipepink", "ghostpipered",
            "daffodil", "mugwort", "lupine",
        };

        public static bool IsKnownFlower(string species)
        {
            if (string.IsNullOrEmpty(species)) return false;
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i] == species) return true;
            }

            return false;
        }
    }
}
