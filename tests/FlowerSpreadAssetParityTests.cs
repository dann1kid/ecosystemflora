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
            string path = Path.Combine(PlantAssetDir, "juvenile-flower-catmint.json");
            string json = File.ReadAllText(path);
            Assert.Contains("petal/catmint", json);
            Assert.DoesNotMatch("petal/catmint[0-9]", json);
            Assert.Contains("\"frostable\": true", json);
            Assert.Contains("\"cover\"", json);
            Assert.Contains("\"snow\"", json);
        }

        [Fact]
        public void Redtopgrass_Juvenile_UsesWildcardCrossTextures()
        {
            string path = Path.Combine(PlantAssetDir, "juvenile-flower-redtopgrass.json");
            string json = File.ReadAllText(path);
            Assert.Contains("petal/redtopgrass*", json);
            Assert.DoesNotContain("petal/redtopgrass1", json);
        }

        [Fact]
        public void AllEcologyFlowers_HaveJuvenileSpreadBlock()
        {
            foreach (string species in EcologyFlowerSpecies.All)
            {
                string path = Path.Combine(PlantAssetDir, $"juvenile-flower-{species}.json");
                Assert.True(File.Exists(path), $"missing juvenile for {species}");
                string json = File.ReadAllText(path);
                Assert.Contains("\"frostable\": true", json);
            }
        }

        [Fact]
        public void AllEcologyFlowers_JuvenileSpreadBlock_HasSnowCoverVariant()
        {
            foreach (string species in EcologyFlowerSpecies.All)
            {
                string path = Path.Combine(PlantAssetDir, $"juvenile-flower-{species}.json");
                string json = File.ReadAllText(path);
                Assert.Contains("crossandsnowlayer", json);
                Assert.Contains("*-snow", json);
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

            string phasePath = Path.Combine(PlantAssetDir, "flowerphase-catmint-dormant.json");
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
            string flowerPath = Path.Combine(PlantAssetDir, $"juvenile-flower-{species}.json");
            Assert.False(File.Exists(flowerPath));

            string fernPath = Path.Combine(PlantAssetDir, $"juvenile-fern-{species}.json");
            Assert.True(File.Exists(fernPath), fernPath);
            Assert.Contains("crossandsnowlayer", File.ReadAllText(fernPath));

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
                string path = Path.Combine(PlantAssetDir, $"juvenile-fern-{species}.json");
                string json = File.ReadAllText(path);
                Assert.Contains($"game:block/plant/fern/{species}", json);
                Assert.Contains("crossandsnowlayer", json);
            }
        }

        [Fact]
        public void JuvenileFlowerSpreadBlocks_UseGameDomainPlantSounds()
        {
            foreach (string species in EcologyFlowerSpecies.All)
            {
                string path = Path.Combine(PlantAssetDir, $"juvenile-flower-{species}.json");
                string json = File.ReadAllText(path);
                Assert.Contains("\"break\": \"game:block/plant\"", json);
                Assert.Contains("\"hit\": \"game:block/plant\"", json);
                Assert.DoesNotMatch("\"break\": \"block/plant\"", json);
            }
        }

        [Fact]
        public void AllEcologyFlowers_PhenologyPhaseBlocks_HaveSnowCoverVariant()
        {
            foreach (string species in EcologyFlowerSpecies.All)
            {
                foreach (string phase in new[] { "vegetative", "dormant", "dieback" })
                {
                    string path = Path.Combine(PlantAssetDir, $"flowerphase-{species}-{phase}.json");
                    Assert.True(File.Exists(path), $"missing {species} {phase}");
                    string json = File.ReadAllText(path);
                    Assert.Contains("\"frostable\": true", json);
                    Assert.Contains("crossandsnowlayer", json);
                }
            }
        }

        [Fact]
        public void AllFernPhases_HaveSnowCoverVariant()
        {
            foreach (string species in new[] { "eaglefern", "cinnamonfern", "deerfern", "hartstongue", "tallfern" })
            {
                foreach (string phase in new[] { "dormant", "dieback" })
                {
                    string basePath = Path.Combine(PlantAssetDir, $"fernphase-{species}-{phase}.json");
                    Assert.True(File.Exists(basePath), basePath);
                    string baseJson = File.ReadAllText(basePath);
                    Assert.Contains("\"frostable\": true", baseJson);
                    Assert.DoesNotContain("variantgroups", baseJson);

                    string snowPath = Path.Combine(PlantAssetDir, $"fernphase-{species}-{phase}-snow.json");
                    Assert.True(File.Exists(snowPath), snowPath);
                    string snowJson = File.ReadAllText(snowPath);
                    Assert.Contains("crossandsnowlayer", snowJson);
                }
            }
        }

        [Fact]
        public void TallgrassPhases_HaveSnowCoverVariant()
        {
            foreach (string phase in new[] { "dormant", "dieback" })
            {
                string path = Path.Combine(PlantAssetDir, $"tallgrassphase-{phase}.json");
                Assert.True(File.Exists(path), path);
                string json = File.ReadAllText(path);
                Assert.Contains("\"frostable\": true", json);
                Assert.Contains("crossandsnowlayer", json);
            }
        }

        [Fact]
        public void TallgrassPhaseBlocks_UseVanillaCrossAndTallgrassTextures()
        {
            foreach (string phase in new[] { "dormant", "dieback" })
            {
                string path = Path.Combine(PlantAssetDir, $"tallgrassphase-{phase}.json");
                string json = File.ReadAllText(path);
                Assert.Contains("\"drawtype\": \"JSON\"", json);
                Assert.Contains("game:block/basic/cross", json);
                Assert.Contains("game:block/plant/tallgrass/free/veryshort-north", json);
                Assert.Contains("game:block/plant/tallgrass/free/veryshort-south", json);
                Assert.Contains("game:block/plant/tallgrass/snow/veryshort-north", json);
                Assert.Contains("game:block/plant/tallgrass/snow/veryshort-south", json);
                Assert.Contains("\"drawnHeight\": 8", json);
                Assert.DoesNotContain("plant/grass/tall/veryshort", json);
                Assert.Contains("crossandsnowlayer", json);
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
