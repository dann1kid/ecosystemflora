using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace WildFarming.Ecosystem.SpeciesEcology
{
    internal static class SpeciesSeasonCsvWriter
    {
        public const string DefaultRelativePath = "assets/ecosystemflora/species/season.csv";

        public static string FormatRowLine(SpeciesSeasonCsvRow row) => FormatRow(row);

        public static void WriteFile(string path, IReadOnlyList<SpeciesSeasonCsvRow> rows)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using (var writer = new StreamWriter(path, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                Write(writer, rows);
            }
        }

        public static void Write(TextWriter writer, IReadOnlyList<SpeciesSeasonCsvRow> rows)
        {
            writer.WriteLine(string.Join(",", SpeciesSeasonCsvSchema.Columns));
            for (int i = 0; i < rows.Count; i++)
            {
                writer.WriteLine(FormatRow(rows[i]));
            }
        }

        static string FormatRow(SpeciesSeasonCsvRow row)
        {
            var fields = new string[25];
            fields[0] = row.Species;
            for (int month = 0; month < 12; month++)
            {
                fields[1 + month] = F(row.Spread[month]);
                fields[13 + month] = F(row.Stress[month]);
            }

            var sb = new StringBuilder();
            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(fields[i]);
            }

            return sb.ToString();
        }

        static string F(float value) =>
            value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
