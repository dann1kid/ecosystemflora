using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Wood codes from vanilla sapling / log-grown (bamboo excluded).</summary>
    public static class EcologyTreeSpecies
    {
        public static readonly IReadOnlyList<string> AllWoods = new[]
        {
            "birch", "oak", "maple", "pine", "acacia", "kapok", "larch",
            "crimsonkingmaple", "redwood", "baldcypress", "greenspirecypress",
            "ebony", "purpleheart", "walnut",
        };

        public static bool IsKnown(string species)
        {
            if (string.IsNullOrEmpty(species)) return false;
            for (int i = 0; i < AllWoods.Count; i++)
            {
                if (AllWoods[i] == species) return true;
            }

            return false;
        }
    }
}
