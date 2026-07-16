using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;

namespace WildFarming.Ecosystem.SpeciesEcology
{
    /// <summary>
    /// Runtime species ecology table: C# defaults → mod assets CSV → ModConfig user CSV.
    /// </summary>
    internal static class SpeciesEcologyRegistry
    {
        public const string UserFileName = SpeciesEcologyUserConfig.EcologyFileName;
        public const string UserFolderSubpath = SpeciesEcologyUserConfig.ConfigSubfolder;
        public const string AssetRelativePath = "assets/ecosystemflora/species/ecology.csv";

        static Dictionary<string, SpeciesEcologyCsvRow> bySpecies =
            new Dictionary<string, SpeciesEcologyCsvRow>(StringComparer.Ordinal);

        public static bool IsLoaded { get; private set; }

        public static bool TryGet(string species, out SpeciesEcologyCsvRow row)
        {
            if (string.IsNullOrEmpty(species))
            {
                row = null;
                return false;
            }

            return bySpecies.TryGetValue(species, out row);
        }

        public static void TryLoadFromDisk(ICoreAPI api, string modRoot, bool syncUserFiles)
        {
            if (api == null || string.IsNullOrEmpty(modRoot)) return;

            SpeciesEcologyUserConfig.MigrateLegacyFiles(api);

            string userPath = SpeciesEcologyUserConfig.GetEcologyCsvPath(api);
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

            Dictionary<string, SpeciesEcologyCsvRow> seed = BuildShippedDefaults(modRoot);
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

            bySpecies = merged;
            IsLoaded = true;
        }

        internal static void ResetForTests()
        {
            bySpecies = new Dictionary<string, SpeciesEcologyCsvRow>(StringComparer.Ordinal);
            IsLoaded = false;
        }

        static Dictionary<string, SpeciesEcologyCsvRow> BuildShippedDefaults(string modRoot, ICoreAPI api = null)
        {
            var merged = BuildCodeDefaults();
            string assetPath = Path.Combine(modRoot, AssetRelativePath);
            if (File.Exists(assetPath))
            {
                MergeCsvFile(assetPath, merged, userFile: false, api);
            }

            return merged;
        }

        static void WriteUserCsv(string userCsvPath, Dictionary<string, SpeciesEcologyCsvRow> seed)
        {
            IReadOnlyList<SpeciesEcologyCatalog.Entry> catalog = SpeciesEcologyCatalog.All();
            var rows = new List<SpeciesEcologyCsvRow>(catalog.Count);
            for (int i = 0; i < catalog.Count; i++)
            {
                string species = catalog[i].Species;
                if (!seed.TryGetValue(species, out SpeciesEcologyCsvRow row)) continue;
                rows.Add(SpeciesEcologyCsvMerge.Clone(row));
            }

            SpeciesEcologyCsvWriter.WriteFile(userCsvPath, rows);
        }

        static Dictionary<string, SpeciesEcologyCsvRow> BuildCodeDefaults()
        {
            var dict = new Dictionary<string, SpeciesEcologyCsvRow>(StringComparer.Ordinal);
            IReadOnlyList<SpeciesEcologyCsvRow> rows = SpeciesEcologyExporter.ExportAll();
            for (int i = 0; i < rows.Count; i++)
            {
                SpeciesEcologyCsvRow row = SpeciesEcologyCsvMerge.Clone(rows[i]);
                dict[row.Species] = row;
            }

            return dict;
        }

        static void MergeCsvFile(
            string path,
            Dictionary<string, SpeciesEcologyCsvRow> target,
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

                if (!target.TryGetValue(entry.Key, out SpeciesEcologyCsvRow row))
                {
                    row = new SpeciesEcologyCsvRow { Species = entry.Key };
                    target[entry.Key] = row;
                }

                SpeciesEcologyCsvMerge.ApplyFields(row, entry.Value);
            }

            SpeciesCsvLoadWarnings.LogIssues(api, path, issues, userFile);
        }

        static void AppendMissingUserRows(string userCsvPath, Dictionary<string, SpeciesEcologyCsvRow> merged)
        {
            HashSet<string> present = SpeciesEcologyCsvReader.ReadSpeciesKeys(userCsvPath);
            IReadOnlyList<SpeciesEcologyCatalog.Entry> catalog = SpeciesEcologyCatalog.All();
            var missing = new List<SpeciesEcologyCsvRow>();

            for (int i = 0; i < catalog.Count; i++)
            {
                string species = catalog[i].Species;
                if (present.Contains(species)) continue;
                if (!merged.TryGetValue(species, out SpeciesEcologyCsvRow row)) continue;
                missing.Add(row);
            }

            if (missing.Count == 0) return;

            bool writeHeader = new FileInfo(userCsvPath).Length == 0;
            using (var writer = new StreamWriter(userCsvPath, append: true))
            {
                if (writeHeader)
                {
                    writer.WriteLine(string.Join(",", SpeciesEcologyCsvSchema.Columns));
                }

                for (int i = 0; i < missing.Count; i++)
                {
                    writer.WriteLine(SpeciesEcologyCsvWriter.FormatRowLine(missing[i]));
                }
            }
        }

        public static void ApplyTo(PlantRequirements req)
        {
            if (!IsLoaded || req == null || string.IsNullOrEmpty(req.Species)) return;
            if (!bySpecies.TryGetValue(req.Species, out SpeciesEcologyCsvRow row)) return;

            SpeciesEcologyApplier.Apply(req, row);
        }

        public static bool TryGetMatConnectivity(string species, out MatConnectivity connectivity)
        {
            connectivity = default;
            if (!IsLoaded || !TryGet(species, out SpeciesEcologyCsvRow row)) return false;
            if (string.IsNullOrEmpty(row.MatConnectivity)) return false;
            return Enum.TryParse(row.MatConnectivity, ignoreCase: true, out connectivity);
        }

        public static bool TryGetFlowerMaturation(string species, out double maturationHours, out double cooldownHours)
        {
            maturationHours = 0;
            cooldownHours = 0;
            if (!IsLoaded || !TryGet(species, out SpeciesEcologyCsvRow row)) return false;
            if (row.FlowerMaturationHours <= 0 && row.FlowerCooldownHours <= 0) return false;
            maturationHours = row.FlowerMaturationHours;
            cooldownHours = row.FlowerCooldownHours;
            return true;
        }

        public static bool TryGetFlowerPhenologyLifeCycles(string species, out int lifeCycles)
        {
            lifeCycles = 0;
            if (!IsLoaded || !TryGet(species, out SpeciesEcologyCsvRow row)) return false;
            if (row.FlowerPhenologyLifeCycles <= 0) return false;
            lifeCycles = row.FlowerPhenologyLifeCycles;
            return true;
        }

        public static bool TryGetFernMaturation(string species, out double maturationHours, out double cooldownHours)
        {
            maturationHours = 0;
            cooldownHours = 0;
            if (!IsLoaded || !TryGet(species, out SpeciesEcologyCsvRow row)) return false;
            if (row.FernMaturationHours <= 0 && row.FernCooldownHours <= 0) return false;
            maturationHours = row.FernMaturationHours;
            cooldownHours = row.FernCooldownHours;
            return true;
        }

        public static bool TryGetBerryMaturationHours(string species, out double maturationHours)
        {
            maturationHours = 0;
            if (!IsLoaded || !TryGet(species, out SpeciesEcologyCsvRow row)) return false;
            if (row.BerryMaturationHours <= 0) return false;
            maturationHours = row.BerryMaturationHours;
            return true;
        }
    }
}
