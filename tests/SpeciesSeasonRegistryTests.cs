using System;
using System.IO;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.SpeciesEcology;
using Xunit;

namespace WildFarming.Tests
{
    public class SpeciesSeasonRegistryTests : IDisposable
    {
        readonly string tempDir;

        public SpeciesSeasonRegistryTests()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "wf-species-season-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
        }

        public void Dispose()
        {
            SpeciesSeasonRegistry.ResetForTests();
            SpeciesEcologyRegistry.ResetForTests();
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
        public void LoadFromPaths_includes_all_contract_species()
        {
            SpeciesSeasonRegistry.LoadFromPaths(RepoRoot, userCsvPath: null, appendMissingUserRows: false);
            Assert.True(SpeciesSeasonRegistry.TryGet("horsetail", out _));
        }

        [Fact]
        public void User_csv_overrides_monthly_spread_multiplier()
        {
            string userCsv = Path.Combine(tempDir, "ecosystemflora.species.season.csv");
            File.WriteAllText(userCsv,
                "species,spread_jun\n" +
                "horsetail,0.15\n");

            SpeciesSeasonRegistry.LoadFromPaths(RepoRoot, userCsv, appendMissingUserRows: false);
            Assert.True(SpeciesSeasonRegistry.TryGet("horsetail", out WildSpeciesSeason.Profile profile));
            Assert.Equal(0.15f, profile.SpreadMultiplier(5), 3);
            Assert.True(profile.SpreadMultiplier(4) > 0.15f);
        }

        [Fact]
        public void Resolve_uses_registry_when_loaded()
        {
            string userCsv = Path.Combine(tempDir, "ecosystemflora.species.season.csv");
            File.WriteAllText(userCsv,
                "species,stress_dec\n" +
                "horsetail,0.99\n");

            SpeciesSeasonRegistry.LoadFromPaths(RepoRoot, userCsv, appendMissingUserRows: false);
            WildSpeciesSeason.Profile profile = WildSpeciesSeason.Resolve("horsetail");
            Assert.Equal(0.99f, profile.StressChance(11), 3);
        }

        [Fact]
        public void Ecology_user_csv_overrides_flower_maturation()
        {
            string userCsv = Path.Combine(tempDir, "ecosystemflora.species.csv");
            File.WriteAllText(userCsv,
                "species,flower_maturation_h\n" +
                "horsetail,12\n");

            SpeciesEcologyRegistry.LoadFromPaths(RepoRoot, userCsv, appendMissingUserRows: false);
            Assert.True(WildFlowerMaturation.TryGetProfile("horsetail", out WildFlowerMaturation.Profile profile));
            Assert.Equal(12, profile.MaturationHours);
        }

        [Fact]
        public void Ecology_user_csv_overrides_berry_mat_connectivity()
        {
            string userCsv = Path.Combine(tempDir, "ecosystemflora.species.csv");
            File.WriteAllText(userCsv,
                "species,mat_connectivity\n" +
                "blackberry,Orthogonal4\n");

            SpeciesEcologyRegistry.LoadFromPaths(RepoRoot, userCsv, appendMissingUserRows: false);
            Assert.True(SpeciesEcologyRegistry.TryGetMatConnectivity("blackberry", out MatConnectivity connectivity));
            Assert.Equal(MatConnectivity.Orthogonal4, connectivity);
        }
    }
}
