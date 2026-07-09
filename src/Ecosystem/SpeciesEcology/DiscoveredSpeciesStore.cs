using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;

namespace WildFarming.Ecosystem.SpeciesEcology
{
    /// <summary>
    /// Persisted list of non-contract species ids discovered at runtime (third-party participants, modded tree woods, etc.).
    /// This allows user CSV validation to accept those rows across restarts.
    /// </summary>
    internal static class DiscoveredSpeciesStore
    {
        const string FileName = "discovered.csv";

        static readonly HashSet<string> discovered =
            new HashSet<string>(StringComparer.Ordinal);

        public static IReadOnlyCollection<string> All() => discovered;

        public static string GetPath(ICoreAPI api) =>
            Path.Combine(SpeciesEcologyUserConfig.GetSpeciesFolder(api), FileName);

        public static void Load(ICoreAPI api)
        {
            discovered.Clear();
            if (api == null) return;

            string path = GetPath(api);
            if (!File.Exists(path)) return;

            try
            {
                foreach (string line in File.ReadAllLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.TrimStart().StartsWith("#", StringComparison.Ordinal)) continue;
                    if (line.StartsWith("species", StringComparison.OrdinalIgnoreCase)) continue;

                    string[] cells = line.Split(',');
                    if (cells.Length == 0) continue;
                    string species = cells[0]?.Trim();
                    if (string.IsNullOrEmpty(species)) continue;
                    discovered.Add(species);
                }
            }
            catch
            {
                // Ignore parse errors; worst case we just don't extend validation this boot.
            }
        }

        public static bool AddAndPersist(ICoreAPI api, IEnumerable<string> species)
        {
            if (api == null || species == null) return false;

            bool changed = false;
            foreach (string s in species)
            {
                if (string.IsNullOrWhiteSpace(s)) continue;
                if (SpeciesEcologyCatalogIndex.IsContractSpecies(s)) continue;
                if (discovered.Add(s)) changed = true;
            }

            if (!changed) return false;

            string dir = SpeciesEcologyUserConfig.GetSpeciesFolder(api);
            Directory.CreateDirectory(dir);
            string path = GetPath(api);

            // Rewrite whole file for determinism.
            using (var writer = new StreamWriter(path, append: false))
            {
                writer.WriteLine("species");
                foreach (string s in Sorted())
                {
                    writer.WriteLine(s);
                }
            }

            return true;
        }

        static IEnumerable<string> Sorted()
        {
            var list = new List<string>(discovered);
            list.Sort(StringComparer.Ordinal);
            return list;
        }
    }
}

