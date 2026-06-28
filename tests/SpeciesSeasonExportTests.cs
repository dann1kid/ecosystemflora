using System;
using System.IO;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.SpeciesEcology;
using Xunit;

namespace WildFarming.Tests
{
    public class SpeciesSeasonExportTests
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
        public void ExportAll_covers_contract_species()
        {
            var rows = SpeciesSeasonExporter.ExportAll();
            Assert.Equal(SpeciesEcologyCatalog.All().Count, rows.Count);
        }

        [Fact]
        public void Export_horsetail_has_colonizer_summer_peak()
        {
            SpeciesSeasonCsvRow row = SpeciesSeasonExporter.Export("horsetail");
            Assert.True(row.Spread[4] > row.Spread[0]);
            Assert.Equal(0f, row.Spread[0], 3);
        }

        [Fact]
        public void Export_writes_repo_csv_when_requested()
        {
            if (Environment.GetEnvironmentVariable("ECOSYSTEMFLORA_EXPORT_SPECIES_SEASON_CSV") != "1")
            {
                return;
            }

            string path = Path.Combine(RepoRoot, SpeciesSeasonCsvWriter.DefaultRelativePath);
            SpeciesSeasonCsvWriter.WriteFile(path, SpeciesSeasonExporter.ExportAll());
            Assert.True(File.Exists(path));
        }
    }
}
