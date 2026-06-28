using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace WildFarming.Ecosystem.SpeciesEcology
{
    internal static class SpeciesEcologyCsvWriter
    {
        public const string DefaultRelativePath = "assets/ecosystemflora/species/ecology.csv";

        public static string FormatRowLine(SpeciesEcologyCsvRow row) => FormatRow(row);

        static readonly string[] Header = SpeciesEcologyCsvSchema.Columns;

        public static void WriteFile(string path, IReadOnlyList<SpeciesEcologyCsvRow> rows)
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

        public static void Write(TextWriter writer, IReadOnlyList<SpeciesEcologyCsvRow> rows)
        {
            writer.WriteLine(string.Join(",", Header));
            for (int i = 0; i < rows.Count; i++)
            {
                writer.WriteLine(FormatRow(rows[i]));
            }
        }

        static string FormatRow(SpeciesEcologyCsvRow row)
        {
            var fields = new string[]
            {
                row.Species,
                row.Taxon,
                F(row.MinTemp), F(row.MaxTemp), F(row.MinRain), F(row.MaxRain),
                F(row.MinForest), F(row.MaxForest),
                F(row.SpreadRate),
                row.SpreadMode,
                row.MatConnectivity,
                F(row.SeedDispersalChance), I(row.SeedDispersalRadius),
                I(row.MatSpreadRadius), I(row.IndependentSpreadRadius), I(row.SpreadRadius),
                I(row.SameSpeciesSpacing), I(row.OtherSpeciesSpacing), Escape(row.SpacingFromSpecies), I(row.MinSunlight),
                row.Habitat,
                I(row.WaterMaxDepth), I(row.WaterMinDepth), I(row.WaterVerticalBlocks), I(row.WaterExactDepth),
                row.SoilKinds, I(row.SoilMinFertility), I(row.SoilMaxFertility),
                row.ContextAffinity, F(row.ContextBonus), F(row.ForestInteriorPenalty), F(row.HoldStrength),
                row.Moisture, row.Light, F(row.NicheBonus),
                row.SeasonExplicit ? "true" : "false",
                D(row.FlowerMaturationHours), D(row.FlowerCooldownHours),
                D(row.FernMaturationHours), D(row.FernCooldownHours),
                D(row.BerryMaturationHours),
                row.TreeSeralRole,
                row.SoilSuccessionRole,
            };

            var sb = new StringBuilder();
            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(Escape(fields[i]));
            }

            return sb.ToString();
        }

        static string F(float value) =>
            value.ToString("0.###", CultureInfo.InvariantCulture);

        static string I(int value) =>
            value.ToString(CultureInfo.InvariantCulture);

        static string D(double value) =>
            value <= 0 ? string.Empty : value.ToString("0.###", CultureInfo.InvariantCulture);

        static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            bool needsQuotes = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            if (!needsQuotes) return value;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
