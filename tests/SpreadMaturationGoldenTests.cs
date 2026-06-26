using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    /// <summary>
    /// Golden characterization of flower/fern maturation + cooldown math, pinned before the
    /// SpreadMaturationPolicy unification so the merge is provably behavior-preserving.
    /// Seasonal scaling is disabled (UseSeasonalEcology = false) so api/pos can be null and only
    /// the profile tables, multipliers, floors, and clamps are exercised.
    /// </summary>
    public class SpreadMaturationGoldenTests
    {
        static EcosystemConfig Cfg(float growth = 1f, float flowerCd = 1f, float fernCd = 1f)
        {
            return new EcosystemConfig
            {
                UseSeasonalEcology = false,
                GrowthHoursMultiplier = growth,
                FlowerSpreadCooldownHoursMultiplier = flowerCd,
                FernSpreadCooldownHoursMultiplier = fernCd,
                EnableFlowerSpreadAttemptCooldown = true,
                EnableFernSpreadAttemptCooldown = true,
            };
        }

        static PlantRequirements Req(string species, EcologyHabitat habitat = EcologyHabitat.Terrestrial)
        {
            return new PlantRequirements { Species = species, Habitat = habitat };
        }

        // ---- Flower maturation hours ----
        [Theory]
        [InlineData("cowparsley", 42)]
        [InlineData("catmint", 48)]      // DefaultSteady
        [InlineData("daffodil", 72)]     // DefaultSlow
        [InlineData("bluebell", 64)]     // DefaultForest
        [InlineData("croton", 84)]       // DefaultRare
        public void Flower_MaturationHours_MatchesProfileTable(string species, double expected)
        {
            Assert.Equal(expected, WildFlowerMaturation.MaturationHours(null, null, species, Cfg()));
        }

        [Fact]
        public void Flower_MaturationHours_ScalesWithGrowthMultiplier()
        {
            Assert.Equal(21, WildFlowerMaturation.MaturationHours(null, null, "cowparsley", Cfg(growth: 2f)));
        }

        [Fact]
        public void Flower_MaturationHours_FloorIsSix()
        {
            Assert.Equal(6, WildFlowerMaturation.MaturationHours(null, null, "cowparsley", Cfg(growth: 20f)));
        }

        // ---- Fern maturation hours ----
        [Theory]
        [InlineData("eaglefern", 58)]    // BorealForest
        [InlineData("cinnamonfern", 62)] // ColdWetForest
        [InlineData("deerfern", 50)]     // TemperateWetForest
        [InlineData("tallfern", 46)]     // DefaultEdge
        [InlineData("hartstongue", 44)]  // Hartstongue
        public void Fern_MaturationHours_MatchesProfileTable(string species, double expected)
        {
            Assert.Equal(expected, WildFernSpread.MaturationHours(null, null, species, Cfg()));
        }

        [Fact]
        public void Fern_MaturationHours_FloorIsEight()
        {
            Assert.Equal(8, WildFernSpread.MaturationHours(null, null, "eaglefern", Cfg(growth: 20f)));
        }

        // ---- Post-spread cooldown ----
        [Theory]
        [InlineData("cowparsley", 18)]
        [InlineData("catmint", 24)]
        public void Flower_PostSpreadCooldown_MatchesProfileTable(string species, double expected)
        {
            Assert.Equal(expected, WildFlowerMaturation.PostSpreadAttemptCooldownHours(null, null, Req(species), Cfg()));
        }

        [Fact]
        public void Flower_PostSpreadCooldown_ScalesWithMultiplier()
        {
            Assert.Equal(9, WildFlowerMaturation.PostSpreadAttemptCooldownHours(null, null, Req("cowparsley"), Cfg(flowerCd: 2f)));
        }

        [Theory]
        [InlineData("eaglefern", 34)]
        [InlineData("hartstongue", 24)]
        public void Fern_PostSpreadCooldown_MatchesProfileTable(string species, double expected)
        {
            Assert.Equal(expected, WildFernSpread.PostSpreadAttemptCooldownHours(null, null, Req(species), Cfg()));
        }

        // ---- Failed-chance-roll cooldown (different base + clamp per kind) ----
        [Fact]
        public void Flower_FailedCooldown_BaseIsThree()
        {
            Assert.Equal(3, WildFlowerMaturation.FailedSpreadAttemptCooldownHours(null, null, Req("cowparsley"), Cfg()));
        }

        [Fact]
        public void Flower_FailedCooldown_CapIsFour()
        {
            Assert.Equal(4, WildFlowerMaturation.FailedSpreadAttemptCooldownHours(null, null, Req("cowparsley"), Cfg(flowerCd: 0.5f)));
        }

        [Fact]
        public void Flower_FailedCooldown_FloorIsOne()
        {
            Assert.Equal(1, WildFlowerMaturation.FailedSpreadAttemptCooldownHours(null, null, Req("cowparsley"), Cfg(flowerCd: 4f)));
        }

        [Fact]
        public void Fern_FailedCooldown_BaseIsFour()
        {
            Assert.Equal(4, WildFernSpread.FailedSpreadAttemptCooldownHours(null, null, Req("eaglefern"), Cfg()));
        }

        [Fact]
        public void Fern_FailedCooldown_CapIsSix()
        {
            Assert.Equal(6, WildFernSpread.FailedSpreadAttemptCooldownHours(null, null, Req("eaglefern"), Cfg(fernCd: 0.5f)));
        }

        [Fact]
        public void Fern_FailedCooldown_FloorIsTwo()
        {
            Assert.Equal(2, WildFernSpread.FailedSpreadAttemptCooldownHours(null, null, Req("eaglefern"), Cfg(fernCd: 20f)));
        }

        // ---- Cooldown application: flower has a terrestrial guard, fern does not ----
        [Fact]
        public void Flower_TryApplyCooldown_AppliesOnTerrestrial()
        {
            var entry = new ReproducerEntry(null, null, null, Req("cowparsley"), 0);
            Assert.True(WildFlowerMaturation.TryApplySpreadAttemptCooldown(
                entry, nowHours: 10, api: null, pos: null, entry.Requirements, Cfg(), failedChanceRoll: false));
            Assert.Equal(28, entry.NextSpawnAllowedAtHours);
        }

        [Fact]
        public void Flower_TryApplyCooldown_SkippedWhenNonTerrestrial()
        {
            var entry = new ReproducerEntry(null, null, null, Req("cowparsley", EcologyHabitat.WaterSurface), 0);
            Assert.False(WildFlowerMaturation.TryApplySpreadAttemptCooldown(
                entry, nowHours: 10, api: null, pos: null, entry.Requirements, Cfg(), failedChanceRoll: false));
            Assert.Equal(0, entry.NextSpawnAllowedAtHours);
        }

        [Fact]
        public void Fern_TryApplyCooldown_HasNoHabitatGuard()
        {
            var entry = new ReproducerEntry(null, null, null, Req("eaglefern", EcologyHabitat.WaterSurface), 0);
            Assert.True(WildFernSpread.TryApplySpreadAttemptCooldown(
                entry, nowHours: 10, api: null, pos: null, entry.Requirements, Cfg(), failedChanceRoll: false));
            Assert.Equal(44, entry.NextSpawnAllowedAtHours);
        }

        // ---- Species membership / profile fallback ----
        [Fact]
        public void Flower_TryGetProfile_FallsBackForKnownFlowerWithoutExplicitEntry()
        {
            Assert.True(WildFlowerMaturation.TryGetProfile("daffodil", out WildFlowerMaturation.Profile p));
            Assert.Equal(72, p.MaturationHours);
        }

        [Fact]
        public void Fern_TryGetProfile_FallsBackToDefaultForestForUnlistedFern()
        {
            Assert.True(WildFernSpread.TryGetProfile("eaglefern", out WildFernSpread.Profile p));
            Assert.Equal(58, p.MaturationHours);
        }
    }
}
