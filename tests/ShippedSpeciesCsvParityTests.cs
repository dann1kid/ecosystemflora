using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.SpeciesEcology;
using Xunit;

namespace WildFarming.Tests
{
    public class ShippedSpeciesCsvParityTests
    {
        static string RepoRoot
        {
            get
            {
                string dir = Directory.GetCurrentDirectory();
                while (!string.IsNullOrEmpty(dir))
                {
                    if (File.Exists(Path.Combine(dir, "wildfarming.sln")))
                    {
                        return dir;
                    }

                    dir = Directory.GetParent(dir)?.FullName;
                }

                return Directory.GetCurrentDirectory();
            }
        }

        [Fact]
        public void Shipped_ecology_csv_matches_exporter_defaults()
        {
            SpeciesEcologyRegistry.ResetForTests();
            SpeciesEcologyRegistry.LoadFromPaths(RepoRoot, userCsvPath: null, appendMissingUserRows: false);

            IReadOnlyList<SpeciesEcologyCsvRow> exported = SpeciesEcologyExporter.ExportAll();
            var exportedBySpecies = exported.ToDictionary(r => r.Species, StringComparer.Ordinal);

            IReadOnlyList<SpeciesEcologyCatalog.Entry> catalog = SpeciesEcologyCatalog.All();
            for (int i = 0; i < catalog.Count; i++)
            {
                string species = catalog[i].Species;
                Assert.True(
                    SpeciesEcologyRegistry.TryGet(species, out SpeciesEcologyCsvRow loaded),
                    $"Missing loaded ecology row for '{species}'.");
                Assert.True(
                    exportedBySpecies.TryGetValue(species, out SpeciesEcologyCsvRow expected),
                    $"Missing exported ecology row for '{species}'.");
                Assert.True(
                    SpeciesEcologyCsvMerge.RowEquals(expected, loaded),
                    $"Shipped ecology.csv drift for '{species}' — run tools/Export-SpeciesEcologyCsv.ps1.");
            }
        }

        [Fact]
        public void Shipped_season_csv_matches_exporter_defaults()
        {
            SpeciesSeasonRegistry.ResetForTests();
            SpeciesSeasonRegistry.LoadFromPaths(RepoRoot, userCsvPath: null, appendMissingUserRows: false);

            IReadOnlyList<SpeciesSeasonCsvRow> exported = SpeciesSeasonExporter.ExportAll();
            var exportedBySpecies = exported.ToDictionary(r => r.Species, StringComparer.Ordinal);

            IReadOnlyList<SpeciesEcologyCatalog.Entry> catalog = SpeciesEcologyCatalog.All();
            for (int i = 0; i < catalog.Count; i++)
            {
                string species = catalog[i].Species;
                Assert.True(
                    SpeciesSeasonRegistry.TryGet(species, out WildSpeciesSeason.Profile loaded),
                    $"Missing loaded season profile for '{species}'.");
                Assert.True(
                    exportedBySpecies.TryGetValue(species, out SpeciesSeasonCsvRow expectedRow),
                    $"Missing exported season row for '{species}'.");
                WildSpeciesSeason.Profile expected = SpeciesSeasonCsvMerge.ToProfile(expectedRow);
                Assert.True(
                    ProfileEquals(expected, loaded),
                    $"Shipped season.csv drift for '{species}' — run tools/Export-SpeciesSeasonCsv.ps1.");
            }
        }

        static bool ProfileEquals(WildSpeciesSeason.Profile a, WildSpeciesSeason.Profile b, float tolerance = 0.001f)
        {
            for (int month = 0; month < 12; month++)
            {
                if (Math.Abs(a.SpreadMultiplier(month) - b.SpreadMultiplier(month)) > tolerance) return false;
                if (Math.Abs(a.StressChance(month) - b.StressChance(month)) > tolerance) return false;
            }

            return true;
        }
    }
}
