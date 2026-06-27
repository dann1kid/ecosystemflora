using WildFarming.Ecosystem;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Xunit;

namespace WildFarming.Tests
{
    public class TreeSenescenceTests
    {
        static readonly EcosystemConfig EnabledCfg = new EcosystemConfig
        {
            EnableTreeAging = true,
            EnableTreeSenescence = true,
        };

        static readonly EcosystemConfig DisabledCfg = new EcosystemConfig
        {
            EnableTreeAging = true,
            EnableTreeSenescence = false,
        };

        static ReproducerEntry MakeOakEntry(int ageYears, TreeSenescencePhase phase = TreeSenescencePhase.None)
        {
            return new ReproducerEntry(
                new BlockPos(0, 64, 0, 0),
                new AssetLocation("game:sapling-oak-free"),
                new AssetLocation("game:log-grown-oak-ud"),
                new PlantRequirements { Species = "oak", Habitat = EcologyHabitat.TerrestrialTree },
                0)
            {
                TreeAgeYears = ageYears,
                TreeSenescencePhase = phase,
            };
        }

        [Theory]
        [InlineData(119, false)]
        [InlineData(120, true)]
        [InlineData(150, true)]
        public void Oak_IsPastHorizon_AtLifespan(int ageYears, bool expected)
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");
            bool past = TreeSenescence.IsPastHorizon(ageYears, profile, EnabledCfg);

            Assert.Equal(expected, past);
        }

        [Fact]
        public void Senescence_Off_NeverPastHorizon()
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");

            Assert.False(TreeSenescence.IsPastHorizon(200, profile, DisabledCfg));
        }

        [Fact]
        public void Senescence_RequiresTreeAging()
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");
            var cfg = new EcosystemConfig { EnableTreeAging = false, EnableTreeSenescence = true };

            Assert.False(TreeSenescence.IsPastHorizon(200, profile, cfg));
        }

        [Theory]
        [InlineData("pine", 110)]
        [InlineData("birch", 75)]
        [InlineData("redwood", 140)]
        public void Species_Horizon_MatchesProfile(string wood, int horizon)
        {
            var profile = WildTreeGrowthProfiles.Resolve(wood);

            Assert.Equal(horizon, profile.SenescenceAgeYears);
            Assert.False(TreeSenescence.IsPastHorizon(horizon - 1, profile, EnabledCfg));
            Assert.True(TreeSenescence.IsPastHorizon(horizon, profile, EnabledCfg));
        }

        [Fact]
        public void SuppressesSpread_WhenDeclining()
        {
            ReproducerEntry entry = MakeOakEntry(125, TreeSenescencePhase.Declining);

            Assert.True(TreeSenescence.SuppressesSpread(entry, EnabledCfg));
        }

        [Fact]
        public void SuppressesSpread_WhenPastHorizon_BeforeFirstPhaseTick()
        {
            ReproducerEntry entry = MakeOakEntry(120, TreeSenescencePhase.None);

            Assert.True(TreeSenescence.SuppressesSpread(entry, EnabledCfg));
        }

        [Fact]
        public void SuppressesSpread_False_WhenYoung()
        {
            ReproducerEntry entry = MakeOakEntry(50, TreeSenescencePhase.None);

            Assert.False(TreeSenescence.SuppressesSpread(entry, EnabledCfg));
        }
    }
}
