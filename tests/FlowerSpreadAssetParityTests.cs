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
        public void Redtopgrass_Juvenile_UsesWildcardCrossTextures()
        {
            string path = Path.Combine(PlantAssetDir, "juvenile-flower-redtopgrass-free.json");
            string json = File.ReadAllText(path);
            Assert.Contains("petal/redtopgrass*", json);
            Assert.DoesNotContain("petal/redtopgrass1", json);
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

        [Fact]
        public void Eaglefern_PhaseBlock_UsesVanillaFernShape_NotCross()
        {
            string path = Path.Combine(PlantAssetDir, "fernphase-eaglefern-dieback.json");
            string json = File.ReadAllText(path);
            Assert.Contains("game:block/plant/fern/eaglefern/var*", json);
            Assert.DoesNotContain("fern/cross", json);
            Assert.Contains("\"break\": \"game:block/plant\"", json);
        }

        [Fact]
        public void Cinnamonfern_PhaseBlock_DefinesAllShapeTextureKeys()
        {
            string path = Path.Combine(PlantAssetDir, "fernphase-cinnamonfern-dieback.json");
            string json = File.ReadAllText(path);
            Assert.Contains("game:block/plant/fern/cinnamonfern/var*", json);
            Assert.Contains("\"center1\"", json);
            Assert.Contains("\"center2\"", json);
            Assert.Contains("\"short\"", json);
        }

        [Fact]
        public void JuvenileFernSpreadBlocks_ReferenceGameDomainShapes()
        {
            foreach (string species in new[] { "eaglefern", "cinnamonfern", "deerfern", "hartstongue", "tallfern" })
            {
                string path = Path.Combine(PlantAssetDir, $"juvenile-fern-{species}-free.json");
                string json = File.ReadAllText(path);
                Assert.Contains($"game:block/plant/fern/{species}", json);
            }
        }

        [Fact]
        public void JuvenileFlowerSpreadBlocks_UseGameDomainPlantSounds()
        {
            foreach (string species in EcologyFlowerSpecies.All)
            {
                string path = Path.Combine(PlantAssetDir, $"juvenile-flower-{species}-free.json");
                string json = File.ReadAllText(path);
                Assert.Contains("\"break\": \"game:block/plant\"", json);
                Assert.Contains("\"hit\": \"game:block/plant\"", json);
                Assert.DoesNotMatch("\"break\": \"block/plant\"", json);
            }
        }

        [Fact]
        public void TallgrassPhaseBlocks_UseVanillaCrossAndTallgrassTextures()
        {
            foreach (string phase in new[] { "dormant", "dieback" })
            {
                string freePath = Path.Combine(PlantAssetDir, $"tallgrassphase-{phase}-free.json");
                string freeJson = File.ReadAllText(freePath);
                Assert.Contains("\"drawtype\": \"JSON\"", freeJson);
                Assert.Contains("game:block/basic/cross", freeJson);
                Assert.Contains("game:block/plant/tallgrass/free/veryshort-north", freeJson);
                Assert.Contains("game:block/plant/tallgrass/free/veryshort-south", freeJson);
                Assert.Contains("\"drawnHeight\": 8", freeJson);
                Assert.DoesNotContain("plant/grass/tall/veryshort", freeJson);

                string snowPath = Path.Combine(PlantAssetDir, $"tallgrassphase-{phase}-snow.json");
                Assert.True(File.Exists(snowPath), snowPath);
                string snowJson = File.ReadAllText(snowPath);
                Assert.Contains("\"drawtype\": \"crossandsnowlayer\"", snowJson);
                Assert.Contains("game:block/plant/tallgrass/snow/veryshort-north", snowJson);
                Assert.Contains("game:block/plant/tallgrass/snow/veryshort-south", snowJson);
            }
        }

        [Fact]
        public void SeasonalPhaseBlocks_UseGameDomainTexturePaths()
        {
            string[] badPatterns =
            {
                "\"base\": \"block/",
                "plant/grass/tall/",
                "\"break\": \"block/plant\"",
            };

            foreach (string file in Directory.GetFiles(PlantAssetDir, "*phase*.json"))
            {
                string json = File.ReadAllText(file);
                foreach (string bad in badPatterns)
                {
                    Assert.DoesNotContain(bad, json);
                }

                if (json.Contains("\"textures\""))
                {
                    Assert.Matches(@"""base"":\s*""game:", json);
                }
            }
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
