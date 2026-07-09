using System;
using System.Collections.Generic;

namespace WildFarming.Ecosystem.SpeciesEcology
{
    /// <summary>Contract species keys for CSV validation warnings.</summary>
    internal static class SpeciesEcologyCatalogIndex
    {
        static HashSet<string> contractSpecies;
        static HashSet<string> discoveredSpecies;

        public static bool IsContractSpecies(string species)
        {
            if (string.IsNullOrEmpty(species)) return false;
            contractSpecies ??= BuildContractSet();
            return contractSpecies.Contains(species);
        }

        public static bool IsKnownSpecies(string species)
        {
            if (string.IsNullOrEmpty(species)) return false;
            if (IsContractSpecies(species)) return true;
            discoveredSpecies ??= BuildDiscoveredSet();
            return discoveredSpecies.Contains(species);
        }

        internal static void SeedDiscoveredFromStore(System.Collections.Generic.IReadOnlyCollection<string> discovered)
        {
            if (discovered == null) return;
            discoveredSpecies ??= new HashSet<string>(StringComparer.Ordinal);
            foreach (string s in discovered)
            {
                if (string.IsNullOrEmpty(s)) continue;
                discoveredSpecies.Add(s);
            }
        }

        public static void ResetForTests()
        {
            contractSpecies = null;
            discoveredSpecies = null;
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

        static HashSet<string> BuildDiscoveredSet()
        {
            // Runtime store is loaded on server start; on client/singleplayer this stays empty unless loaded.
            return new HashSet<string>(StringComparer.Ordinal);
        }
    }
}
