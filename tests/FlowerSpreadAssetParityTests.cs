using System.IO;
using System.Text.RegularExpressions;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    /// <summary>
    /// Meadow flower spread assets share one pipeline — catch species that drift to wrong texture groups.
    /// </summary>
    public class FlowerSpreadAssetParityTests
    {
        static readonly string PlantAssetDir = Path.Combine(
            FindRepoRoot(),
            "assets",
            "ecosystemflora",
            "blocktypes",
            "plant");

        static string FindRepoRoot()
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

        [Fact]
        public void Catmint_Juvenile_UsesWildcardSingleTexture()
        {
            string path = Path.Combine(PlantAssetDir, "juvenile-flower-catmint-free.json");
            string json = File.ReadAllText(path);
            Assert.Contains("petal/catmint", json);
            Assert.DoesNotMatch("petal/catmint[0-9]", json);
        }

        [Fact]
        public void Redtopgrass_Juvenile_UsesNumberedCrossTextures()
        {
            string path = Path.Combine(PlantAssetDir, "juvenile-flower-redtopgrass-free.json");
            string json = File.ReadAllText(path);
            Assert.Contains("petal/redtopgrass1", json);
        }

        [Fact]
        public void AllEcologyFlowers_HaveJuvenileSpreadBlock()
        {
            foreach (string species in EcologyFlowerSpecies.All)
            {
                string path = Path.Combine(PlantAssetDir, $"juvenile-flower-{species}-free.json");
                Assert.True(File.Exists(path), $"missing juvenile for {species}");
            }
        }

        [Fact]
        public void AllEcologyFlowers_HaveDefinedSoilRole()
        {
            foreach (string species in EcologyFlowerSpecies.All)
            {
                Assert.True(WildSpeciesSoilSuccession.TryGetRole(species, out _), species);
            }
        }

        [Fact]
        public void OpenMeadowFlowers_NeverUseForestSoilRole()
        {
            string[] openMeadow =
            {
                "wilddaisy", "cornflower", "goldenpoppy", "forgetmenot", "cowparsley",
                "catmint", "daffodil", "lupine", "mugwort", "woad", "orangemallow",
                "edelweiss", "heather", "westerngorse", "horsetail",
            };

            foreach (string species in openMeadow)
            {
                Assert.True(WildSpeciesSoilSuccession.TryGetRole(species, out PlantSoilRole role), species);
                Assert.False(role.IsForestRole(), $"{species} → {role}");
            }
        }

        [Fact]
        public void AllEcologyFlowers_SpreadNeverSetsForestFloorFlag()
        {
            foreach (string species in EcologyFlowerSpecies.All)
            {
                Assert.True(WildSpeciesSoilSuccession.TryGetImpact(
                    species, SoilSuccessionEvent.Spread, out SoilImpact spread), species);
                Assert.False(spread.IsForestFloor, species + " spread");
            }
        }

        [Fact]
        public void AllEcologyFlowers_UseFlowerSpreadMaturationPipeline()
        {
            var cfg = new EcosystemConfig
            {
                EnableFlowerSpreadMaturation = true,
                EnableFlowerSpreadAttemptCooldown = true,
            };

            foreach (string species in EcologyFlowerSpecies.All)
            {
                Assert.True(WildFlowerMaturation.UsesMaturation(cfg, species), species);
                Assert.True(WildFlowerMaturation.TryGetProfile(species, out _), species);
                Assert.NotNull(FlowerJuvenileBlocks.CodeForSpecies(species));
            }
        }

        [Fact]
        public void Catmint_PhenologyMatchesOtherMeadowFlowers()
        {
            var cfg = new EcosystemConfig { EnableFlowerPhenology = true };
            var req = new PlantRequirements { Species = "catmint", Habitat = EcologyHabitat.Terrestrial };

            Assert.True(FlowerPhenology.UsesPhenology(cfg, req));
            Assert.NotNull(FlowerPhenologyBlocks.CodeForPhase("catmint", FlowerPhenologyPhase.Dormant));

            string phasePath = Path.Combine(PlantAssetDir, "flowerphase-catmint-dormant-free.json");
            Assert.True(File.Exists(phasePath));

            string json = File.ReadAllText(phasePath);
            Assert.Contains("petal/catmint\"", json);
            Assert.DoesNotMatch("petal/catmint[0-9]", json);
        }

        [Theory]
        [InlineData("eaglefern")]
        [InlineData("hartstongue")]
        public void Ferns_UseFernJuvenilePipeline_NotFlowerJuvenile(string species)
        {
            string flowerPath = Path.Combine(PlantAssetDir, $"juvenile-flower-{species}-free.json");
            Assert.False(File.Exists(flowerPath));

            string fernPath = Path.Combine(PlantAssetDir, $"juvenile-fern-{species}-free.json");
            Assert.True(File.Exists(fernPath), fernPath);

            var cfg = new EcosystemConfig
            {
                EnableFernSpreadMaturation = true,
                EnableFernRhizomeSpread = true,
                EnableFernSporulationGate = true,
            };

            Assert.False(WildFlowerMaturation.UsesMaturation(cfg, species));
            Assert.True(WildFernSpread.UsesMaturation(cfg, species));
            Assert.Equal(SpreadMode.FernRhizomeMat, BuildFernRequirements(species).SpreadMode);
        }

        static PlantRequirements BuildFernRequirements(string species)
        {
            return PlantRequirements.FromBlock(new Vintagestory.API.Common.Block
            {
                Code = FernJuvenileBlocks.MatureVanillaCode(species),
            });
        }
    }
}
