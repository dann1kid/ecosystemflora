using WildFarming.Ecosystem;
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

        [Theory]
        [InlineData(119, false)]
        [InlineData(120, true)]
        [InlineData(150, true)]
        public void Oak_IsSenescent_AtHorizon(int ageYears, bool expected)
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");
            bool senescent = TreeSenescence.IsSenescent(ageYears, profile, EnabledCfg);

            Assert.Equal(expected, senescent);
        }

        [Fact]
        public void Senescence_Off_NeverTriggers()
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");

            Assert.False(TreeSenescence.IsSenescent(200, profile, DisabledCfg));
        }

        [Fact]
        public void Senescence_RequiresTreeAging()
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");
            var cfg = new EcosystemConfig { EnableTreeAging = false, EnableTreeSenescence = true };

            Assert.False(TreeSenescence.IsSenescent(200, profile, cfg));
        }

        [Theory]
        [InlineData("pine", 110)]
        [InlineData("birch", 90)]
        [InlineData("redwood", 140)]
        public void Species_Horizon_MatchesProfile(string wood, int horizon)
        {
            var profile = WildTreeGrowthProfiles.Resolve(wood);

            Assert.Equal(horizon, profile.SenescenceAgeYears);
            Assert.False(TreeSenescence.IsSenescent(horizon - 1, profile, EnabledCfg));
            Assert.True(TreeSenescence.IsSenescent(horizon, profile, EnabledCfg));
        }
    }
}
