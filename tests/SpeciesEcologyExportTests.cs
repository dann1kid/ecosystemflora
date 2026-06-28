using System;
using System.IO;
using System.Linq;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.SpeciesEcology;
using Xunit;

namespace WildFarming.Tests
{
    public class SpeciesEcologyExportTests
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
            var rows = SpeciesEcologyExporter.ExportAll();
            Assert.Equal(SpeciesEcologyCatalog.All().Count, rows.Count);
            Assert.Equal(rows.Count, rows.Select(r => r.Species).Distinct().Count());
        }

        [Fact]
        public void Export_horsetail_matches_flower_climate_table()
        {
            SpeciesEcologyCsvRow row = SpeciesEcologyExporter.Export("horsetail", "flower");
            Assert.Equal(2.8f, row.SpreadRate, 3);
            Assert.Equal(1f, row.MinTemp, 3);
            Assert.Equal(15f, row.MaxTemp, 3);
            Assert.Equal("Independent", row.SpreadMode);
            Assert.True(row.SeasonExplicit);
            Assert.True(row.FlowerMaturationHours > 0);
        }

        [Fact]
        public void Export_brownsedge_matches_shore_sedge_table()
        {
            SpeciesEcologyCsvRow row = SpeciesEcologyExporter.Export(EcologyShoreSedgeSpecies.Brownsedge, "shore_sedge");
            Assert.Equal(0.35f, row.SpreadRate, 3);
            Assert.Equal("ShoreSedgeMat", row.SpreadMode);
            Assert.Equal(0f, row.SeedDispersalChance, 3);
        }

        [Fact]
        public void Export_bluebell_includes_spacing_from_species()
        {
            SpeciesEcologyCsvRow row = SpeciesEcologyExporter.Export("bluebell", "flower");
            Assert.Contains("wilddaisy=3", row.SpacingFromSpecies);
            Assert.Contains("catmint=3", row.SpacingFromSpecies);
        }

        [Fact]
        public void Export_writes_repo_csv_when_requested()
        {
            if (Environment.GetEnvironmentVariable("ECOSYSTEMFLORA_EXPORT_SPECIES_CSV") != "1")
            {
                return;
            }

            string path = Path.Combine(RepoRoot, SpeciesEcologyCsvWriter.DefaultRelativePath);
            SpeciesEcologyCsvWriter.WriteFile(path, SpeciesEcologyExporter.ExportAll());
            Assert.True(File.Exists(path));
        }
    }
}
