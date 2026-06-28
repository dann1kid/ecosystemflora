using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Moq;
using Vintagestory.API.Common;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.SpeciesEcology;
using Xunit;

namespace WildFarming.Tests
{
    public class SpeciesEcologyRegistryTests : IDisposable
    {
        readonly string tempDir;

        public SpeciesEcologyRegistryTests()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "wf-species-ecology-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
        }

        public void Dispose()
        {
            SpeciesEcologyRegistry.ResetForTests();
            SpeciesSeasonRegistry.ResetForTests();
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
            catch
            {
            }
        }

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
        public void EnsureUserCsvFile_creates_full_table_when_missing()
        {
            string userCsv = Path.Combine(tempDir, "species", "ecology.csv");
            SpeciesEcologyRegistry.EnsureUserCsvFile(RepoRoot, userCsv);

            Assert.True(File.Exists(userCsv));
            var present = SpeciesEcologyCsvReader.ReadSpeciesKeys(userCsv);
            Assert.Equal(SpeciesEcologyCatalog.All().Count, present.Count);
        }

        [Fact]
        public void MigrateLegacyFiles_moves_flat_ecology_csv_into_species_folder()
        {
            string modConfig = Path.Combine(tempDir, "ModConfig");
            Directory.CreateDirectory(modConfig);
            string legacy = Path.Combine(modConfig, SpeciesEcologyUserConfig.LegacyEcologyFileName);
            File.WriteAllText(legacy, "species,spread_rate\nhorsetail,1.2\n");

            var api = new Mock<ICoreAPI>();
            api.Setup(a => a.GetOrCreateDataPath("ModConfig")).Returns(modConfig);
            SpeciesEcologyUserConfig.MigrateLegacyFiles(api.Object);

            string migrated = SpeciesEcologyUserConfig.GetEcologyCsvPath(api.Object);
            Assert.True(File.Exists(migrated));
            Assert.False(File.Exists(legacy));
            Assert.Contains("horsetail", SpeciesEcologyCsvReader.ReadSpeciesKeys(migrated));
        }

        [Fact]
        public void LoadFromPaths_includes_all_contract_species()
        {
            SpeciesEcologyRegistry.LoadFromPaths(RepoRoot, userCsvPath: null, appendMissingUserRows: false);
            Assert.True(SpeciesEcologyRegistry.IsLoaded);
            Assert.Equal(SpeciesEcologyCatalog.All().Count, CountLoaded());
        }

        [Fact]
        public void Merge_partial_user_row_overrides_only_set_columns()
        {
            string userCsv = Path.Combine(tempDir, "ecosystemflora.species.csv");
            File.WriteAllText(userCsv,
                "species,spread_rate\n" +
                "horsetail,1.75\n");

            SpeciesEcologyRegistry.LoadFromPaths(RepoRoot, userCsv, appendMissingUserRows: false);
            Assert.True(SpeciesEcologyRegistry.TryGet("horsetail", out SpeciesEcologyCsvRow row));
            Assert.Equal(1.75f, row.SpreadRate, 3);
            Assert.Equal(1f, row.MinTemp, 3);
            Assert.Equal(15f, row.MaxTemp, 3);
        }

        [Fact]
        public void Registry_override_applies_to_PlantRequirements_FromBlock()
        {
            string userCsv = Path.Combine(tempDir, "ecosystemflora.species.csv");
            File.WriteAllText(userCsv,
                "species,spread_rate\n" +
                "horsetail,4.2\n");

            SpeciesEcologyRegistry.LoadFromPaths(RepoRoot, userCsv, appendMissingUserRows: false);

            PlantRequirements req = PlantRequirements.FromBlock(new Block
            {
                Code = new AssetLocation("game:flower-horsetail-free"),
            });

            Assert.Equal("horsetail", req.Species);
            Assert.Equal(4.2f, req.SpreadRate, 3);
        }

        [Fact]
        public void AppendMissingUserRows_adds_contract_species_not_in_file()
        {
            string userCsv = Path.Combine(tempDir, "ecosystemflora.species.csv");
            File.WriteAllText(userCsv,
                "species,spread_rate\n" +
                "horsetail,1.1\n");

            SpeciesEcologyRegistry.LoadFromPaths(RepoRoot, userCsv, appendMissingUserRows: true);

            var present = SpeciesEcologyCsvReader.ReadSpeciesKeys(userCsv);
            Assert.Contains("horsetail", present);
            Assert.Equal(SpeciesEcologyCatalog.All().Count, present.Count);
        }

        [Fact]
        public void CsvReader_parseLine_handles_quoted_commas()
        {
            string[] cells = SpeciesEcologyCsvReader.ParseLine("a,\"b,c\",d");
            Assert.Equal(new[] { "a", "b,c", "d" }, cells);
        }

        [Fact]
        public void FromBlock_uses_registry_as_primary_source_when_loaded()
        {
            SpeciesEcologyRegistry.LoadFromPaths(RepoRoot, userCsvPath: null, appendMissingUserRows: false);

            PlantRequirements req = PlantRequirements.FromBlock(new Block
            {
                Code = new AssetLocation("game:flower-horsetail-free"),
            });

            Assert.True(SpeciesEcologyRegistry.TryGet("horsetail", out SpeciesEcologyCsvRow row));
            Assert.Equal(row.SpreadRate, req.SpreadRate, 3);
            Assert.Equal(row.MinTemp, req.MinTemp, 3);
            Assert.Equal("Independent", req.SpreadMode.ToString());
            Assert.True(req.HasNicheProfile);
        }

        [Fact]
        public void Registry_primary_path_does_not_call_legacy_climate_tables()
        {
            string userCsv = Path.Combine(tempDir, "ecosystemflora.species.csv");
            File.WriteAllText(userCsv,
                "species,spread_rate,min_temp\n" +
                "horsetail,9.9,99\n");

            SpeciesEcologyRegistry.LoadFromPaths(RepoRoot, userCsv, appendMissingUserRows: false);

            PlantRequirements req = PlantRequirements.FromBlock(new Block
            {
                Code = new AssetLocation("game:flower-horsetail-free"),
            });

            Assert.Equal(9.9f, req.SpreadRate, 3);
            Assert.Equal(99f, req.MinTemp, 3);
        }

        [Fact]
        public void Registry_primary_path_applies_spacing_from_species()
        {
            SpeciesEcologyRegistry.LoadFromPaths(RepoRoot, userCsvPath: null, appendMissingUserRows: false);

            PlantRequirements req = PlantRequirements.FromBlock(new Block
            {
                Code = new AssetLocation("game:flower-bluebell-free"),
            });

            Assert.NotNull(req.SpacingFromSpecies);
            Assert.Equal(3, req.GetRequiredSpacingTo("wilddaisy", new EcosystemConfig()));
        }

        int CountLoaded()
        {
            int count = 0;
            IReadOnlyList<SpeciesEcologyCatalog.Entry> catalog = SpeciesEcologyCatalog.All();
            for (int i = 0; i < catalog.Count; i++)
            {
                if (SpeciesEcologyRegistry.TryGet(catalog[i].Species, out _))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
