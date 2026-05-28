using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class RhizomeSpreadTests
    {
        [Theory]
        [InlineData(1, 0, true)]
        [InlineData(-1, 0, true)]
        [InlineData(0, 1, true)]
        [InlineData(0, -1, true)]
        [InlineData(1, 1, false)]
        [InlineData(2, 0, false)]
        [InlineData(0, 0, false)]
        public void IsOrthogonalStep_MatchesManhattanDistanceOne(int dx, int dz, bool expected)
        {
            Assert.Equal(expected, RhizomeSpread.IsOrthogonalStep(dx, dz));
        }

        [Fact]
        public void ApplyTo_ReedHabitat_SetsRhizomeModeAndRadius()
        {
            EcosystemConfig.Loaded = new EcosystemConfig { UseRhizomeSpreadForReeds = true };

            var req = new PlantRequirements
            {
                Habitat = EcologyHabitat.ReedNearWater,
                Species = "coopersreed",
            };

            RhizomeSpread.ApplyTo(req);

            Assert.True(req.UsesRhizomeSpread);
            Assert.Equal(1, req.SpreadRadius);
        }

        [Fact]
        public void ApplyTo_ConfigOff_LeavesIndependentMode()
        {
            EcosystemConfig.Loaded = new EcosystemConfig { UseRhizomeSpreadForReeds = false };

            var req = new PlantRequirements
            {
                Habitat = EcologyHabitat.ReedNearWater,
                Species = "coopersreed",
            };

            RhizomeSpread.ApplyTo(req);

            Assert.False(req.UsesRhizomeSpread);
            Assert.Equal(0, req.SpreadRadius);
        }

        [Fact]
        public void WildAquaticEcology_ReedSpreadRates_AreBelowTwo()
        {
            Assert.True(WildAquaticEcology.TryGet("coopersreed", out WildAquaticEcology.Profile reed));
            Assert.True(WildAquaticEcology.TryGet("tule", out WildAquaticEcology.Profile tule));
            Assert.True(WildAquaticEcology.TryGet("papyrus", out WildAquaticEcology.Profile papyrus));

            Assert.Equal(1.0f, reed.SpreadRate);
            Assert.Equal(0.85f, tule.SpreadRate);
            Assert.Equal(0.75f, papyrus.SpreadRate);
        }

        [Fact]
        public void WildAquaticEcology_ReedsHaveSeedDispersalProfiles()
        {
            Assert.True(WildAquaticEcology.TryGet("coopersreed", out WildAquaticEcology.Profile reed));
            Assert.True(WildAquaticEcology.TryGet("tule", out WildAquaticEcology.Profile tule));
            Assert.True(WildAquaticEcology.TryGet("papyrus", out WildAquaticEcology.Profile papyrus));

            Assert.Equal(0.08f, reed.SeedDispersalChance);
            Assert.Equal(6, reed.SeedDispersalRadius);
            Assert.Equal(0.06f, tule.SeedDispersalChance);
            Assert.Equal(5, tule.SeedDispersalRadius);
            Assert.Equal(0.10f, papyrus.SeedDispersalChance);
            Assert.Equal(5, papyrus.SeedDispersalRadius);
        }

        [Fact]
        public void ResolveSearchRadius_SeedMode_UsesSpeciesRadius()
        {
            var req = new PlantRequirements { Species = "coopersreed", SpreadRadius = 1, SeedDispersalRadius = 6 };
            int radius = RhizomeSpread.ResolveSearchRadius(req, MatSpreadCollectMode.SeedDispersal, 4);
            Assert.Equal(6, radius);
        }

        [Fact]
        public void ResolveSearchRadius_RhizomeMode_UsesOneBlock()
        {
            var req = new PlantRequirements { SpreadRadius = 1 };
            int radius = RhizomeSpread.ResolveSearchRadius(req, MatSpreadCollectMode.MatEdge, 4);
            Assert.Equal(1, radius);
        }

        [Fact]
        public void EffectiveSeedDispersalChance_ScalesWithConfig()
        {
            EcosystemConfig.Loaded = new EcosystemConfig { RhizomeSeedDispersalChanceScale = 0.5f };
            var req = new PlantRequirements { Species = "coopersreed", SeedDispersalChance = 0.08f };
            Assert.Equal(0.04f, RhizomeSpread.EffectiveSeedDispersalChance(req));
        }
    }
}
