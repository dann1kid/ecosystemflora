using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;

namespace WildFarming.Ecosystem.SpeciesEcology
{
    /// <summary>C# season defaults → mod assets season.csv → ModConfig user season CSV.</summary>
    internal static class SpeciesSeasonRegistry
    {
        public const string UserFileName = SpeciesEcologyUserConfig.SeasonFileName;
        public const string AssetRelativePath = "assets/ecosystemflora/species/season.csv";

        static Dictionary<string, WildSpeciesSeason.Profile> bySpecies =
            new Dictionary<string, WildSpeciesSeason.Profile>(StringComparer.Ordinal);

        public static bool IsLoaded { get; private set; }

        public static bool TryGet(string species, out WildSpeciesSeason.Profile profile)
        {
            if (string.IsNullOrEmpty(species))
            {
                profile = default;
                return false;
            }

            return bySpecies.TryGetValue(species, out profile);
        }

        public static void TryLoadFromDisk(ICoreAPI api, string modRoot, bool syncUserFiles)
        {
            if (api == null || string.IsNullOrEmpty(modRoot)) return;

            string userPath = SpeciesEcologyUserConfig.GetSeasonCsvPath(api);
            if (syncUserFiles)
            {
                Directory.CreateDirectory(SpeciesEcologyUserConfig.GetSpeciesFolder(api));
                EnsureUserCsvFile(modRoot, userPath);
            }

            LoadFromPaths(modRoot, File.Exists(userPath) ? userPath : null, appendMissingUserRows: syncUserFiles, api);
        }

        internal static void EnsureUserCsvFile(string modRoot, string userCsvPath)
        {
            if (string.IsNullOrEmpty(modRoot) || string.IsNullOrEmpty(userCsvPath)) return;

            Dictionary<string, SpeciesSeasonCsvRow> seed = BuildShippedDefaults(modRoot);
            if (!File.Exists(userCsvPath))
            {
                WriteUserCsv(userCsvPath, seed);
                return;
            }

            AppendMissingUserRows(userCsvPath, seed);
        }

        internal static void LoadFromPaths(string modRoot, string userCsvPath, bool appendMissingUserRows, ICoreAPI api = null)
        {
            var merged = BuildShippedDefaults(modRoot, api);

            if (!string.IsNullOrEmpty(userCsvPath) && File.Exists(userCsvPath))
            {
                MergeCsvFile(userCsvPath, merged, userFile: true, api);
                if (appendMissingUserRows)
                {
                    AppendMissingUserRows(userCsvPath, merged);
                }
            }

            bySpecies = new Dictionary<string, WildSpeciesSeason.Profile>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, SpeciesSeasonCsvRow> entry in merged)
            {
                bySpecies[entry.Key] = SpeciesSeasonCsvMerge.ToProfile(entry.Value);
            }

            IsLoaded = true;
        }

        internal static void ResetForTests()
        {
            bySpecies = new Dictionary<string, WildSpeciesSeason.Profile>(StringComparer.Ordinal);
            IsLoaded = false;
        }

        static Dictionary<string, SpeciesSeasonCsvRow> BuildShippedDefaults(string modRoot, ICoreAPI api = null)
        {
            var merged = BuildCodeDefaults();
            string assetPath = Path.Combine(modRoot, AssetRelativePath);
            if (File.Exists(assetPath))
            {
                MergeCsvFile(assetPath, merged, userFile: false, api);
            }

            return merged;
        }

        static void WriteUserCsv(string userCsvPath, Dictionary<string, SpeciesSeasonCsvRow> seed)
        {
            IReadOnlyList<SpeciesEcologyCatalog.Entry> catalog = SpeciesEcologyCatalog.All();
            var rows = new List<SpeciesSeasonCsvRow>(catalog.Count);
            for (int i = 0; i < catalog.Count; i++)
            {
                string species = catalog[i].Species;
                if (!seed.TryGetValue(species, out SpeciesSeasonCsvRow row)) continue;
                rows.Add(SpeciesSeasonCsvMerge.Clone(row));
            }

            SpeciesSeasonCsvWriter.WriteFile(userCsvPath, rows);
        }

        static Dictionary<string, SpeciesSeasonCsvRow> BuildCodeDefaults()
        {
            var dict = new Dictionary<string, SpeciesSeasonCsvRow>(StringComparer.Ordinal);
            IReadOnlyList<SpeciesSeasonCsvRow> rows = SpeciesSeasonExporter.ExportAll();
            for (int i = 0; i < rows.Count; i++)
            {
                SpeciesSeasonCsvRow row = SpeciesSeasonCsvMerge.Clone(rows[i]);
                dict[row.Species] = row;
            }

            return dict;
        }

        static void MergeCsvFile(
            string path,
            Dictionary<string, SpeciesSeasonCsvRow> target,
            bool userFile,
            ICoreAPI api = null)
        {
            var issues = new List<CsvRowIssue>();
            foreach (KeyValuePair<string, Dictionary<string, string>> entry in SpeciesEcologyCsvReader.ReadRows(
                path,
                issues,
                validateContractSpecies: true))
            {
                if (userFile && !SpeciesEcologyCatalogIndex.IsContractSpecies(entry.Key))
                {
                    continue;
                }

                if (!target.TryGetValue(entry.Key, out SpeciesSeasonCsvRow row))
                {
                    row = new SpeciesSeasonCsvRow { Species = entry.Key };
                    target[entry.Key] = row;
                }

                SpeciesSeasonCsvMerge.ApplyFields(row, entry.Value);
            }

            SpeciesCsvLoadWarnings.LogIssues(api, path, issues, userFile);
        }

        static void AppendMissingUserRows(string userCsvPath, Dictionary<string, SpeciesSeasonCsvRow> merged)
        {
            HashSet<string> present = SpeciesEcologyCsvReader.ReadSpeciesKeys(userCsvPath);
            IReadOnlyList<SpeciesEcologyCatalog.Entry> catalog = SpeciesEcologyCatalog.All();
            var missing = new List<SpeciesSeasonCsvRow>();

            for (int i = 0; i < catalog.Count; i++)
            {
                string species = catalog[i].Species;
                if (present.Contains(species)) continue;
                if (!merged.TryGetValue(species, out SpeciesSeasonCsvRow row)) continue;
                missing.Add(row);
            }

            if (missing.Count == 0) return;

            bool writeHeader = new FileInfo(userCsvPath).Length == 0;
            using (var writer = new StreamWriter(userCsvPath, append: true))
            {
                if (writeHeader)
                {
                    writer.WriteLine(string.Join(",", SpeciesSeasonCsvSchema.Columns));
                }

                for (int i = 0; i < missing.Count; i++)
                {
                    writer.WriteLine(SpeciesSeasonCsvWriter.FormatRowLine(missing[i]));
                }
            }
        }
    }
}

