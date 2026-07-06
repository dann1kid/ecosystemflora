using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.SpeciesEcology;
using WildFarming.Ecosystem.Testing;
using Xunit;

namespace WildFarming.Tests
{
    public class BerryColonySpreadTests
    {
        public BerryColonySpreadTests()
        {
            SpeciesEcologyRegistry.ResetForTests();
        }

        [Fact]
        public void ApplyTo_NullSpecies_DoesNotThrow()
        {
            EcosystemConfig.Loaded = new EcosystemConfig { EnableBerryColonySpread = true };

            var req = new PlantRequirements { Species = null, Habitat = EcologyHabitat.Terrestrial };
            BerryColonySpread.ApplyTo(req);

            Assert.False(req.UsesBerryColonySpread);
        }

        [Theory]
        [InlineData("blueberry")]
        [InlineData("blackcurrant")]
        [InlineData("blackberry")]
        public void ApplyTo_WhenEnabled_SetsBerryColonyMat(string species)
        {
            EcosystemConfig.Loaded = new EcosystemConfig { EnableBerryColonySpread = true };

            var req = new PlantRequirements { Species = species, Habitat = EcologyHabitat.Terrestrial };
            BerryColonySpread.ApplyTo(req);

            Assert.True(req.UsesBerryColonySpread);
            Assert.Equal(1, req.SpreadRadius);
        }

        [Fact]
        public void ApplyTo_Beautyberry_StaysIndependent()
        {
            EcosystemConfig.Loaded = new EcosystemConfig { EnableBerryColonySpread = true };

            var req = new PlantRequirements { Species = "beautyberry", Habitat = EcologyHabitat.Terrestrial };
            BerryColonySpread.ApplyTo(req);

            Assert.False(req.UsesBerryColonySpread);
            Assert.Equal(5, req.SpreadRadius);
        }

        [Fact]
        public void ApplyTo_WhenDisabled_FallsBackToIndependentRadius()
        {
            EcosystemConfig.Loaded = new EcosystemConfig { EnableBerryColonySpread = false };

            var req = new PlantRequirements { Species = "blueberry", Habitat = EcologyHabitat.Terrestrial };
            BerryColonySpread.ApplyTo(req);

            Assert.False(req.UsesBerryColonySpread);
        }

        [Theory]
        [InlineData("blueberry", 1, 0, true)]
        [InlineData("blueberry", 1, 1, false)]
        [InlineData("blackberry", 1, 1, true)]
        [InlineData("blackberry", 2, 0, false)]
        public void IsStep_RespectsConnectivity(string species, int dx, int dz, bool expected)
        {
            Assert.Equal(expected, BerryColonySpread.IsStep(dx, dz, species));
        }

        [Fact]
        public void IsFrontier_ThirdPartyBerry_AirNeighbor_IsFrontier()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                EcosystemConfig.Loaded = new EcosystemConfig
                {
                    EnableThirdPartyParticipants = true,
                    EnableBerryColonySpread = true,
                };

                Block air = new Block { BlockId = 0, Code = new AssetLocation("game:air") };
                Block bdBlueberry = ThirdPartyBerryBlock("bdshrub", "blueberry", 1);
                var acc = new EcologyTestBlockAccessor(new[] { air, bdBlueberry });
                var pos = new BlockPos(10, 64, 10);
                acc.SetBlock(1, pos);

                Assert.True(BerryColonySpread.IsFrontier(acc, pos, "blueberry"));
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }

        [Fact]
        public void IsFrontier_ThirdPartyBerry_SurroundedBySameSpecies_NotFrontier()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                EcosystemConfig.Loaded = new EcosystemConfig
                {
                    EnableThirdPartyParticipants = true,
                    EnableBerryColonySpread = true,
                };

                Block air = new Block { BlockId = 0, Code = new AssetLocation("game:air") };
                Block bdBlueberry = ThirdPartyBerryBlock("bdshrub", "blueberry", 1);
                var acc = new EcologyTestBlockAccessor(new[] { air, bdBlueberry });
                var center = new BlockPos(10, 64, 10);
                acc.SetBlock(1, center);
                acc.SetBlock(1, new BlockPos(11, 64, 10));
                acc.SetBlock(1, new BlockPos(9, 64, 10));
                acc.SetBlock(1, new BlockPos(10, 64, 11));
                acc.SetBlock(1, new BlockPos(10, 64, 9));

                Assert.False(BerryColonySpread.IsFrontier(acc, center, "blueberry"));
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }

        static Block ThirdPartyBerryBlock(string domain, string species, int id)
        {
            JsonObject attrs = JsonObject.FromJson($@"{{
                ""ecologyParticipant"": true,
                ""ecologySpecies"": ""{species}"",
                ""ecologySpreadBlock"": ""fruitingbush-{species}-empty""
            }}");
            return new Block
            {
                BlockId = id,
                Code = new AssetLocation(domain, $"fruitingbush-{species}-ripe"),
                Attributes = attrs,
            };
        }

        [Fact]
        public void IsThirdPartyEcologyBlock_RecognizesPatchLikeAttrs()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                EcosystemConfig.Loaded = new EcosystemConfig { EnableThirdPartyParticipants = true };
                Block block = ThirdPartyBerryBlock("bdshrub", "blueberry", 1);
                Assert.True(PlantCodeHelper.IsThirdPartyEcologyBlock(block));
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }

        [Fact]
        public void WildBerryEcology_AllTypesHaveProfiles()
        {
            foreach (string type in WildBerryEcology.AllTypes)
            {
                Assert.True(WildBerryEcology.TryGet(type, out WildBerryEcology.Profile profile), type);
                Assert.True(profile.SpreadRate > 0f, type);
            }
        }

        [Theory]
        [InlineData("blueberry", FloraContextAffinity.Forest)]
        [InlineData("blackcurrant", FloraContextAffinity.Edge)]
        [InlineData("cranberry", FloraContextAffinity.Open)]
        [InlineData("beautyberry", FloraContextAffinity.Edge)]
        public void WildSpeciesModifiers_BerriesMatchHabitat(string species, FloraContextAffinity expected)
        {
            Assert.True(WildSpeciesModifiers.TryGet(species, out WildSpeciesModifiers.Profile profile));
            Assert.Equal(expected, profile.ContextAffinity);
        }
    }
}
