using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class ShoreSedgeMatSpreadTests
    {
        [Fact]
        public void ApplyTo_WhenEnabled_SetsShoreSedgeMatMode()
        {
            EcosystemConfig.Loaded = new EcosystemConfig { EnableShoreSedgeMatSpread = true };

            var req = new PlantRequirements { Species = EcologyShoreSedgeSpecies.Brownsedge, Habitat = EcologyHabitat.Terrestrial };
            ShoreSedgeMatSpread.ApplyTo(req);

            Assert.True(req.UsesShoreSedgeMatSpread);
            Assert.Equal(1, req.SpreadRadius);
            Assert.Equal(0f, req.SeedDispersalChance);
            Assert.Equal(0, req.SeedDispersalRadius);
        }

        [Fact]
        public void ApplyTo_WhenDisabled_LeavesIndependentSpread()
        {
            EcosystemConfig.Loaded = new EcosystemConfig { EnableShoreSedgeMatSpread = false };

            var req = new PlantRequirements { Species = EcologyShoreSedgeSpecies.Brownsedge, Habitat = EcologyHabitat.Terrestrial };
            ShoreSedgeMatSpread.ApplyTo(req);

            Assert.False(req.UsesShoreSedgeMatSpread);
        }

        [Theory]
        [InlineData(1, 0, true)]
        [InlineData(1, 1, false)]
        [InlineData(2, 0, false)]
        public void IsOrthogonalStep_MatEdgeOnly(int dx, int dz, bool expected)
        {
            Assert.Equal(expected, ShoreSedgeMatSpread.IsOrthogonalStep(dx, dz));
        }

        [Fact]
        public void ResolveCollectMode_WithZeroSeedChance_AlwaysUsesMatEdge()
        {
            EcosystemConfig.Loaded = new EcosystemConfig
            {
                EnableShoreSedgeMatSpread = true,
                RhizomeSeedDispersalEnabled = true,
                RhizomeSeedDispersalChanceScale = 1f,
            };
            var req = new PlantRequirements { Species = EcologyShoreSedgeSpecies.Brownsedge, Habitat = EcologyHabitat.Terrestrial };
            ShoreSedgeMatSpread.ApplyTo(req);

            Assert.Equal(MatSpreadCollectMode.MatEdge, ShoreSedgeMatSpread.ResolveCollectMode(req, new System.Random(0)));
        }

        [Fact]
        public void ResolveCollectMode_SeedDispersalUsesRhizomeSeedConfig()
        {
            EcosystemConfig.Loaded = new EcosystemConfig
            {
                EnableShoreSedgeMatSpread = true,
                RhizomeSeedDispersalEnabled = true,
                RhizomeSeedDispersalChanceScale = 1f,
            };
            var req = new PlantRequirements { Species = EcologyShoreSedgeSpecies.Brownsedge, Habitat = EcologyHabitat.Terrestrial };
            ShoreSedgeMatSpread.ApplyTo(req);
            req.SeedDispersalChance = 1f;

            Assert.Equal(MatSpreadCollectMode.SeedDispersal, ShoreSedgeMatSpread.ResolveCollectMode(req, new System.Random(0)));
        }

        [Fact]
        public void JuvenileAsset_UsesVanillaSedgeShapeAndTexture()
        {
            string dir = System.IO.Directory.GetCurrentDirectory();
            while (!string.IsNullOrEmpty(dir) && !System.IO.File.Exists(System.IO.Path.Combine(dir, "wildfarming.sln")))
            {
                dir = System.IO.Directory.GetParent(dir)?.FullName;
            }

            string path = System.IO.Path.Combine(
                ResolvePlantAssetDir(),
                "juvenile-sedge-brownsedge.json");
            Assert.True(System.IO.File.Exists(path));
            string json = System.IO.File.ReadAllText(path);
            Assert.Contains("reedpapyrus/sedge", json);
            Assert.Contains("reeds/brownsedge", json);
            Assert.Contains("crossandsnowlayer", json);
        }

        [Fact]
        public void SedgePhaseBlocks_FreeVariant_HasNoDrawnHeightClip()
        {
            foreach (string phase in new[] { "dormant", "dieback" })
            {
                string path = System.IO.Path.Combine(
                    ResolvePlantAssetDir(),
                    $"sedgephase-{phase}.json");
                string json = System.IO.File.ReadAllText(path);
                Assert.Contains("\"scale\": 1.0", json);
                Assert.DoesNotMatch(@"\""\*-free\""\s*:\s*\{[^\}]*\""drawnHeight\""", json);
            }
        }

        [Fact]
        public void UsesJuvenileMaturation_WhenFlowerMaturationEnabled()
        {
            var cfg = new EcosystemConfig { EnableFlowerSpreadMaturation = true };
            Assert.True(WildFlowerMaturation.UsesMaturation(cfg, EcologyShoreSedgeSpecies.Brownsedge));
        }

        static string ResolvePlantAssetDir()
        {
            string dir = System.IO.Directory.GetCurrentDirectory();
            while (!string.IsNullOrEmpty(dir) && !System.IO.File.Exists(System.IO.Path.Combine(dir, "wildfarming.sln")))
            {
                dir = System.IO.Directory.GetParent(dir)?.FullName;
            }

            return System.IO.Path.Combine(
                dir ?? System.IO.Directory.GetCurrentDirectory(),
                "assets",
                "ecosystemflora",
                "blocktypes",
                "plant");
        }
    }
}
