using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    public static class EcologyFernSpecies
    {
        public static readonly IReadOnlyList<string> All = new[]
        {
            "eaglefern", "cinnamonfern", "deerfern", "hartstongue", "tallfern",
        };

        public static bool IsKnown(string species)
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
