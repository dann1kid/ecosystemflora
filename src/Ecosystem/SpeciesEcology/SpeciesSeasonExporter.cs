using System.Collections.Generic;

namespace WildFarming.Ecosystem.SpeciesEcology
{
    internal static class SpeciesSeasonExporter
    {
        public static IReadOnlyList<SpeciesSeasonCsvRow> ExportAll()
        {
            IReadOnlyList<SpeciesEcologyCatalog.Entry> catalog = SpeciesEcologyCatalog.All();
            var rows = new List<SpeciesSeasonCsvRow>(catalog.Count);
            for (int i = 0; i < catalog.Count; i++)
            {
                rows.Add(Export(catalog[i].Species));
            }

            return rows;
        }

        public static SpeciesSeasonCsvRow Export(string species)
        {
            WildSpeciesSeason.Profile profile = WildSpeciesSeason.ResolveFromCode(species);
            var row = new SpeciesSeasonCsvRow { Species = species };
            for (int month = 0; month < 12; month++)
            {
                row.Spread[month] = profile.SpreadMultiplier(month);
                row.Stress[month] = profile.StressChance(month);
            }

            return row;
        }
    }
}
