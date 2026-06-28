using System;
using System.Collections.Generic;
using System.Globalization;

namespace WildFarming.Ecosystem.SpeciesEcology
{
    internal static class SpeciesSeasonCsvMerge
    {
        public static void ApplyFields(SpeciesSeasonCsvRow row, Dictionary<string, string> fields)
        {
            if (row == null || fields == null || fields.Count == 0) return;

            for (int month = 0; month < 12; month++)
            {
                string spreadKey = "spread_" + SpeciesSeasonCsvSchema.MonthSuffixes[month];
                if (fields.TryGetValue(spreadKey, out string spreadText)
                    && float.TryParse(spreadText, NumberStyles.Float, CultureInfo.InvariantCulture, out float spread))
                {
                    row.Spread[month] = spread;
                }

                string stressKey = "stress_" + SpeciesSeasonCsvSchema.MonthSuffixes[month];
                if (fields.TryGetValue(stressKey, out string stressText)
                    && float.TryParse(stressText, NumberStyles.Float, CultureInfo.InvariantCulture, out float stress))
                {
                    row.Stress[month] = stress;
                }
            }
        }

        public static SpeciesSeasonCsvRow Clone(SpeciesSeasonCsvRow source)
        {
            if (source == null) return new SpeciesSeasonCsvRow();

            var row = new SpeciesSeasonCsvRow { Species = source.Species };
            Array.Copy(source.Spread, row.Spread, 12);
            Array.Copy(source.Stress, row.Stress, 12);
            return row;
        }

        public static WildSpeciesSeason.Profile ToProfile(SpeciesSeasonCsvRow row)
        {
            if (row == null) return default;

            var spread = new float[12];
            var stress = new float[12];
            Array.Copy(row.Spread, spread, 12);
            Array.Copy(row.Stress, stress, 12);
            return new WildSpeciesSeason.Profile(spread, stress);
        }

        public static bool RowEquals(SpeciesSeasonCsvRow a, SpeciesSeasonCsvRow b, float tolerance = 0.001f)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            if (a.Species != b.Species) return false;

            for (int i = 0; i < 12; i++)
            {
                if (Math.Abs(a.Spread[i] - b.Spread[i]) > tolerance) return false;
                if (Math.Abs(a.Stress[i] - b.Stress[i]) > tolerance) return false;
            }

            return true;
        }
    }
}
