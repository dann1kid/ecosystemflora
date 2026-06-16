using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Arid single-block cacti (not multi-block saguaro columns).</summary>
    public static class EcologyDesertSpecies
    {
        public const string Barrelcactus = "barrelcactus";
        public const string Silvertorchcactus = "silvertorchcactus";

        public static readonly IReadOnlyList<string> All = new[] { Barrelcactus, Silvertorchcactus };

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
