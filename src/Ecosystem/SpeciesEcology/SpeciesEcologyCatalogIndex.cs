using System;
using System.Collections.Generic;

namespace WildFarming.Ecosystem.SpeciesEcology
{
    /// <summary>Contract species keys for CSV validation warnings.</summary>
    internal static class SpeciesEcologyCatalogIndex
    {
        static HashSet<string> contractSpecies;

        public static bool IsContractSpecies(string species)
        {
            if (string.IsNullOrEmpty(species)) return false;
            contractSpecies ??= BuildContractSet();
            return contractSpecies.Contains(species);
        }

        public static void ResetForTests()
        {
            contractSpecies = null;
        }

        static HashSet<string> BuildContractSet()
        {
            IReadOnlyList<SpeciesEcologyCatalog.Entry> catalog = SpeciesEcologyCatalog.All();
            var set = new HashSet<string>(catalog.Count, StringComparer.Ordinal);
            for (int i = 0; i < catalog.Count; i++)
            {
                set.Add(catalog[i].Species);
            }

            return set;
        }
    }
}
