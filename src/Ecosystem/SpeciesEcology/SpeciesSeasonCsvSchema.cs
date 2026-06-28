namespace WildFarming.Ecosystem.SpeciesEcology
{
    internal static class SpeciesSeasonCsvSchema
    {
        public const string SpeciesColumn = "species";

        public static readonly string[] MonthSuffixes =
        {
            "jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec",
        };

        public static readonly string[] Columns = BuildColumns();

        static string[] BuildColumns()
        {
            var cols = new string[1 + MonthSuffixes.Length * 2];
            cols[0] = SpeciesColumn;
            int index = 1;
            for (int i = 0; i < MonthSuffixes.Length; i++)
            {
                cols[index++] = "spread_" + MonthSuffixes[i];
            }

            for (int i = 0; i < MonthSuffixes.Length; i++)
            {
                cols[index++] = "stress_" + MonthSuffixes[i];
            }

            return cols;
        }
    }
}
