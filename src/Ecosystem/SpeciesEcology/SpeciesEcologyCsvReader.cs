using System;
using System.Collections.Generic;
using System.IO;

namespace WildFarming.Ecosystem.SpeciesEcology
{
    internal static class SpeciesEcologyCsvReader
    {
        public static HashSet<string> ReadSpeciesKeys(string path)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return keys;

            foreach (KeyValuePair<string, Dictionary<string, string>> row in ReadRows(path))
            {
                if (!string.IsNullOrEmpty(row.Key))
                {
                    keys.Add(row.Key);
                }
            }

            return keys;
        }

        public static IEnumerable<KeyValuePair<string, Dictionary<string, string>>> ReadRows(
            string path,
            IList<CsvRowIssue> issues = null,
            bool validateContractSpecies = false)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) yield break;

            string[] lines = File.ReadAllLines(path);
            if (lines.Length == 0) yield break;

            string[] header = ParseLine(lines[0]);
            var columnIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < header.Length; i++)
            {
                string name = header[i]?.Trim();
                if (string.IsNullOrEmpty(name)) continue;
                columnIndex[name] = i;
            }

            if (!columnIndex.ContainsKey(SpeciesEcologyCsvSchema.SpeciesColumn)) yield break;

            var seenSpecies = issues != null
                ? new HashSet<string>(StringComparer.Ordinal)
                : null;

            for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.TrimStart().StartsWith("#", StringComparison.Ordinal)) continue;

                string[] cells = ParseLine(line);
                int speciesIndex = columnIndex[SpeciesEcologyCsvSchema.SpeciesColumn];
                if (speciesIndex >= cells.Length) continue;

                string species = cells[speciesIndex]?.Trim();
                if (string.IsNullOrEmpty(species)) continue;

                if (seenSpecies != null && !seenSpecies.Add(species))
                {
                    issues.Add(new CsvRowIssue(CsvRowIssueKind.DuplicateSpecies, lineIndex + 1, species));
                }

                if (validateContractSpecies
                    && issues != null
                    && !SpeciesEcologyCatalogIndex.IsContractSpecies(species))
                {
                    issues.Add(new CsvRowIssue(CsvRowIssueKind.UnknownSpecies, lineIndex + 1, species));
                }

                var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, int> column in columnIndex)
                {
                    if (column.Key == SpeciesEcologyCsvSchema.SpeciesColumn) continue;
                    if (column.Value >= cells.Length) continue;

                    string value = cells[column.Value]?.Trim();
                    if (string.IsNullOrEmpty(value)) continue;
                    fields[column.Key] = value;
                }

                yield return new KeyValuePair<string, Dictionary<string, string>>(species, fields);
            }
        }

        internal static string[] ParseLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return Array.Empty<string>();

            var cells = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }

                    continue;
                }

                if (c == '"')
                {
                    inQuotes = true;
                    continue;
                }

                if (c == ',')
                {
                    cells.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(c);
            }

            cells.Add(current.ToString());
            return cells.ToArray();
        }
    }
}
