using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class TreeSpacingDefaultsTests
    {
        [Theory]
        [InlineData("birch", 5, 4)]
        [InlineData("oak", 6, 4)]
        [InlineData("pine", 3, 3)]
        [InlineData("redwood", 8, 5)]
        [InlineData("acacia", 7, 5)]
        public void Resolve_UsesHandTunedWildTreeEcology(string wood, int same, int other)
        {
            TreeSpacingDefaults.Resolve(wood, out int gotSame, out int gotOther);
            Assert.Equal(same, gotSame);
            Assert.Equal(other, gotOther);
        }

        [Fact]
        public void Resolve_UnknownWood_UsesCrownPlusSeralBias()
        {
            // No WildTreeEcology row → crown from growth profile default (12/4) + Mid bias 1 → 5/4
            TreeSpacingDefaults.Resolve("notarealwoodzzz", out int same, out int other);
            Assert.Equal(5, same);
            Assert.Equal(4, other);
        }

        [Fact]
        public void GetRequiredSpacingTo_TreeZeroSame_DoesNotReturnZero()
        {
            var req = new PlantRequirements
            {
                Species = "oak",
                Habitat = EcologyHabitat.TerrestrialTree,
                SameSpeciesSpacing = 0,
                OtherSpeciesSpacing = 0,
            };

            Assert.True(req.GetRequiredSpacingTo("oak", new EcosystemConfig()) >= 3);
            Assert.True(req.GetRequiredSpacingTo("birch", new EcosystemConfig()) >= 2);
        }

        [Fact]
        public void GetSpacingSearchRadius_TreeZeroSame_StillSearches()
        {
            var req = new PlantRequirements
            {
                Species = "pine",
                Habitat = EcologyHabitat.TerrestrialTree,
                SameSpeciesSpacing = 0,
                OtherSpeciesSpacing = 0,
            };
            var cfg = new EcosystemConfig { PlantSpacingEnabled = true, DefaultSameSpeciesSpacing = 0, DefaultOtherSpeciesSpacing = 0 };
            Assert.True(req.GetSpacingSearchRadius(cfg) >= 3);
        }

        [Fact]
        public void EnsureOn_FillsMissingTreeSpacing()
        {
            var req = new PlantRequirements
            {
                Species = "maple",
                Habitat = EcologyHabitat.TerrestrialTree,
                SameSpeciesSpacing = 0,
                OtherSpeciesSpacing = 0,
            };
            TreeSpacingDefaults.EnsureOn(req);
            Assert.Equal(6, req.SameSpeciesSpacing);
            Assert.Equal(4, req.OtherSpeciesSpacing);
        }

        [Fact]
        public void FlowerZeroSame_StillMeansPatchForming()
        {
            var req = new PlantRequirements
            {
                Species = "wilddaisy",
                Habitat = EcologyHabitat.Terrestrial,
                SameSpeciesSpacing = 0,
                OtherSpeciesSpacing = 1,
            };
            Assert.Equal(0, req.GetRequiredSpacingTo("wilddaisy", new EcosystemConfig()));
        }

        [Fact]
        public void TimelapsePreset_DoesNotSetGlobalSpacingToZero()
        {
            var cfg = new EcosystemConfig { BalancePreset = EcosystemBalancePresets.Timelapse };
            EcosystemBalancePresets.Apply(cfg, EcosystemBalancePresets.Timelapse);
            Assert.False(cfg.PlantSpacingEnabled);
            Assert.True(cfg.DefaultSameSpeciesSpacing >= 1);
            Assert.True(cfg.DefaultOtherSpeciesSpacing >= 1);
        }

        [Fact]
        public void RedwoodCrown_MatchesWideSpacing()
        {
            Assert.Equal(8, WildTreeGrowthProfiles.Resolve("redwood").ReferenceCrownRadius);
            TreeSpacingDefaults.Resolve("redwood", out int same, out _);
            Assert.Equal(8, same);
        }
    }
}
